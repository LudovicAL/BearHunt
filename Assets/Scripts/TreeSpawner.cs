using UnityEngine;
using System.Collections;

namespace TerrainGenerator {
	public class TreeSpawner : MonoBehaviour {

		public GameObject treesContainer;
		public GameObject bushesContainer;
		public GameObject meadowBushesContainer;
		public GameObject rocksContainer;
		public GameObject player;
		public GameObject terrainChunksContainer;
		public GameObject gameController;

		private readonly float CIRCLE_DIAMETER = 30.0f;
		private readonly float MAX_DISTANCE = 33.0f;

		private int numberOfTerrainTiles;

		// Use this for initialization
		void Start () {
			numberOfTerrainTiles = gameController.GetComponent<GameController> ().GetNumberOfTerrainTiles ();
		}
		
		// Update is called once per frame
		void Update () {
			if (terrainChunksContainer.transform.childCount >= numberOfTerrainTiles) {
				RelocateTransforms (treesContainer);
				RelocateTransforms (bushesContainer);
				RelocateTransforms (meadowBushesContainer);
				RelocateTransforms (rocksContainer);
			}
		}

		private void RelocateTransforms(GameObject container) {
			foreach (Transform t in container.transform) {
				if (Vector3.Distance(t.position, player.transform.position) > MAX_DISTANCE) {
					t.position = GetRandomSpawnPoint(player.transform.position);
					t.Rotate (0.0f, Random.Range (0.0f, 360.0f), 0.0f);
				}
			}
		}

		private Vector3 GetRandomSpawnPoint(Vector3 position) {
			float angle = Random.Range (0.0f, Mathf.PI * 2); //Generates a random angle
			Vector3 point = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle));	//Creates a vector of length 1.0
			point *= CIRCLE_DIAMETER;	//Scales the vector to the desired range
			point.x += position.x;
			point.z += position.z;
			point.y = gameObject.GetComponent<TerrainChunkGenerator> ().GetTerrainHeight (point);	//Gets the terrain height at that position
			return point;
		}
	}
}