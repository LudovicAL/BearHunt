using UnityEngine;
using System.Collections;

namespace TerrainGenerator {
	public class TreeSpawner : MonoBehaviour {

		public GameObject treesContainer;
		public GameObject bushesContainer;
		public GameObject rocksContainer;
		public GameObject player;
		public GameObject terrainChunksContainer;

		// Use this for initialization
		void Start () {
			
		}
		
		// Update is called once per frame
		void Update () {
			if (terrainChunksContainer.transform.childCount >= 4) {
				foreach (Transform t in treesContainer.transform) { 
					if (Vector3.Distance(t.position, player.transform.position) > 33.0f) {
						t.position = getRandomSpawnPoint(player.transform.position);
						t.Rotate (0.0f, Random.Range (0.0f, 360.0f), 0.0f);
					}
				}
				foreach (Transform t in bushesContainer.transform) { 
					if (Vector3.Distance(t.position, player.transform.position) > 33.0f) {
						t.position = getRandomSpawnPoint(player.transform.position);
						t.Rotate (0.0f, Random.Range (0.0f, 360.0f), 0.0f);
					}
				}
				foreach (Transform t in rocksContainer.transform) { 
					if (Vector3.Distance(t.position, player.transform.position) > 33.0f) {
						t.position = getRandomSpawnPoint(player.transform.position);
						t.Rotate (0.0f, Random.Range (0.0f, 360.0f), 0.0f);
					}
				}
			}
		}

		private Vector3 getRandomSpawnPoint(Vector3 position) {
			float angle = Random.Range (0.0f, Mathf.PI * 2); //Generates a random angle
			Vector3 point = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle));	//Creates a vector of length 1.0
			point *= 30;	//Scales the vector to the desired range
			point.x += position.x;
			point.z += position.z;
			point.y = gameObject.GetComponent<TerrainChunkGenerator> ().GetTerrainHeight (point);	//Gets the terrain height at that position
			return point;
		}
	}
}