using System.Threading;
using UnityEngine;

namespace TerrainGenerator {
    public class TerrainChunk {
        public Vector2i position { get; private set; }
        private Terrain terrain { get; set; }
        private TerrainData data { get; set; }
        private TerrainChunkSettings settings { get; set; }
        private NoiseProvider noiseProvider { get; set; }
        private TerrainChunkNeighborhood neighborhood { get; set; }
        private float[,] heightmap { get; set; }
        private object heightmapThreadLockObject { get; set; }

        public TerrainChunk(TerrainChunkSettings settings, NoiseProvider noiseProvider, int x, int z) {
			this.heightmapThreadLockObject = new object();
            this.settings = settings;
			this.noiseProvider = noiseProvider;
			this.neighborhood = new TerrainChunkNeighborhood();
			this.position = new Vector2i(x, z);
        }

        #region Heightmap stuff

        public void GenerateHeightmap() {
            var thread = new Thread(GenerateHeightmapThread);
            thread.Start();
        }

        private void GenerateHeightmapThread() {
            lock (heightmapThreadLockObject) {
                var newHeightmap = new float[settings.HeightmapResolution, settings.HeightmapResolution];
                for (var zRes = 0; zRes < settings.HeightmapResolution; zRes++) {
                    for (var xRes = 0; xRes < settings.HeightmapResolution; xRes++) {
                        var xCoordinate = position.X + (float)xRes / (settings.HeightmapResolution - 1);
                        var zCoordinate = position.Z + (float)zRes / (settings.HeightmapResolution - 1);
						newHeightmap[zRes, xRes] = noiseProvider.GetValue(xCoordinate, zCoordinate);
                    }
                }
				this.heightmap = newHeightmap;
            }
        }

        public bool IsHeightmapReady() {
            return terrain == null && heightmap != null;
        }

        public float GetTerrainHeight(Vector3 worldPosition) {
            return terrain.SampleHeight(worldPosition);
        }

        #endregion
        #region Main terrain generation

        public void CreateTerrain() {
            data = new TerrainData();
            data.heightmapResolution = settings.HeightmapResolution;
            data.alphamapResolution = settings.AlphamapResolution;
            data.SetHeights(0, 0, heightmap);
            ApplyTextures(data);
            data.size = new Vector3(settings.Length, settings.Height, settings.Length);
            var newTerrainGameObject = Terrain.CreateTerrainGameObject(data);
            newTerrainGameObject.transform.position = new Vector3(position.X * settings.Length, 0, position.Z * settings.Length);
			newTerrainGameObject.transform.parent = GameObject.FindGameObjectWithTag ("TerrainChunksContainer").transform;
			terrain = newTerrainGameObject.GetComponent<Terrain>();
			terrain.heightmapPixelError = 8;
			terrain.materialType = UnityEngine.Terrain.MaterialType.Custom;
			terrain.materialTemplate = settings.TerrainMaterial;
			terrain.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
			terrain.Flush();
        }


        private void ApplyTextures(TerrainData terrainData) {
            var flatSplat = new SplatPrototype();
            var steepSplat = new SplatPrototype();
            flatSplat.texture = settings.FlatTexture;
            steepSplat.texture = settings.SteepTexture;
            terrainData.splatPrototypes = new SplatPrototype[] {
                flatSplat,
                steepSplat
            };
            terrainData.RefreshPrototypes();
            var splatMap = new float[terrainData.alphamapResolution, terrainData.alphamapResolution, 2];
            for (var zRes = 0; zRes < terrainData.alphamapHeight; zRes++) {
                for (var xRes = 0; xRes < terrainData.alphamapWidth; xRes++) {
                    var normalizedX = (float)xRes / (terrainData.alphamapWidth - 1);
                    var normalizedZ = (float)zRes / (terrainData.alphamapHeight - 1);
                    var steepness = terrainData.GetSteepness(normalizedX, normalizedZ);
                    var steepnessNormalized = Mathf.Clamp(steepness / 1.5f, 0, 1f);
                    splatMap[zRes, xRes, 0] = 1f - steepnessNormalized;
                    splatMap[zRes, xRes, 1] = steepnessNormalized;
                }
            }
            terrainData.SetAlphamaps(0, 0, splatMap);
        }

        #endregion
        #region Distinction

        public override int GetHashCode() {
            return position.GetHashCode();
        }

        public override bool Equals(object obj) {
            var other = obj as TerrainChunk;
			if (other == null) {
                return false;
			}
            return this.position.Equals(other.position);
        }

        #endregion
        #region Chunk removal

        public void Remove() {
            heightmap = null;
            settings = null;
            if (neighborhood.XDown != null) {
                neighborhood.XDown.RemoveFromNeighborhood(this);
                neighborhood.XDown = null;
            }
            if (neighborhood.XUp != null) {
                neighborhood.XUp.RemoveFromNeighborhood(this);
                neighborhood.XUp = null;
            }
			if (neighborhood.ZDown != null) {
				neighborhood.ZDown.RemoveFromNeighborhood(this);
				neighborhood.ZDown = null;
            }
			if (neighborhood.ZUp != null) {
				neighborhood.ZUp.RemoveFromNeighborhood(this);
				neighborhood.ZUp = null;
            }
			if (terrain != null) {
				GameObject.Destroy(terrain.gameObject);
			}
        }

        public void RemoveFromNeighborhood(TerrainChunk chunk) {
			if (neighborhood.XDown == chunk)
				neighborhood.XDown = null;
			if (neighborhood.XUp == chunk)
				neighborhood.XUp = null;
			if (neighborhood.ZDown == chunk)
				neighborhood.ZDown = null;
			if (neighborhood.ZUp == chunk)
				neighborhood.ZUp = null;
        }

        #endregion
        #region Neighborhood

        public void SetNeighbors(TerrainChunk chunk, TerrainNeighbor direction) {
            if (chunk != null) {
                switch (direction) {
                    case TerrainNeighbor.XUp:
						neighborhood.XUp = chunk;
                        break;
                    case TerrainNeighbor.XDown:
						neighborhood.XDown = chunk;
                        break;
                    case TerrainNeighbor.ZUp:
						neighborhood.ZUp = chunk;
                        break;
                    case TerrainNeighbor.ZDown:
						neighborhood.ZDown = chunk;
                        break;
                }
            }
        }

        public void UpdateNeighbors() {
			if (terrain != null) {
				var xDown = neighborhood.XDown == null ? null : neighborhood.XDown.terrain;
				var xUp = neighborhood.XUp == null ? null : neighborhood.XUp.terrain;
				var zDown = neighborhood.ZDown == null ? null : neighborhood.ZDown.terrain;
				var zUp = neighborhood.ZUp == null ? null : neighborhood.ZUp.terrain;
				terrain.SetNeighbors(xDown, zUp, xUp, zDown);
				terrain.Flush();
            }
        }
        #endregion
    }
}