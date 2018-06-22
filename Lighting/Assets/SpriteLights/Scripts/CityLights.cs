using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;


#if UNITY_EDITOR
using UnityEditor;
#endif

public class CityLights : MonoBehaviour {

	public UnityEngine.Rendering.IndexFormat meshIndexFormat;
	public Material cityLightsMatAmber;
	public Material cityLightsMatWhite;
	[ColorUsageAttribute(true,true,0f,8f,0.125f,3f)]
	public Color amberColor;
	[ColorUsageAttribute(true,true,0f,8f,0.125f,3f)]
	public Color whiteColor;
	public float spacing = 30f;


	//Enable this once Map-ity is installed.
	/*
	void OnEnable(){

		// Register Map-ity's Loaded Event
		Mapity.MapityLoaded += OnMapityLoaded;
	}

	void OnDisable(){

		// Un-Register Map-ity's Loaded Event
		Mapity.MapityLoaded -= OnMapityLoaded;
	}

	void OnMapityLoaded(){	

		List<Vector3> amberLightData = new List<Vector3>();
		List<Vector3> whiteLightData = new List<Vector3>();

		//Loop through all highways and roads.
		foreach (Mapity.Highway highway in Mapity.Singleton.highways.Values){
	
			for(int i = 0; i < highway.wayMapNodes.Count - 1; i++){

				//Get the from-to nodes.
				Mapity.MapNode fromNode = (Mapity.MapNode)highway.wayMapNodes[i];
				Mapity.MapNode toNode = (Mapity.MapNode)highway.wayMapNodes[i+1];

				//Get the road segment start and end point.
				Vector3 from = fromNode.position.world.ToVector();
				Vector3 to = toNode.position.world.ToVector();
				Vector3 fromToVec = to - from;
				float length = fromToVec.magnitude;
				int lightAmount = (int)Mathf.Ceil(length / spacing);
				Vector3 currentPosition = from;

				//Get a translation vector.
				Vector3 offsetVec = Math3d.SetVectorLength(fromToVec, spacing);

				//Give the small roads white lights.
				if ((highway.classification == Mapity.HighwayClassification.Residential) || (highway.classification == Mapity.HighwayClassification.Road) || (highway.classification == Mapity.HighwayClassification.Pedestrian) || (highway.classification == Mapity.HighwayClassification.Residential)){

					//Place light at a certain interval between the road nodes.
					for (int e = 0; e < lightAmount; e++) {

						whiteLightData.Add(currentPosition);
						currentPosition += offsetVec;
					}
				}

				//Give the big roads amber lights.
				else{

					//Place light at a certain interval between the road nodes.
					for (int e = 0; e < lightAmount; e++) {

						amberLightData.Add(currentPosition);
						currentPosition += offsetVec;
					}
				}
			}	
		}
		
		Vector3[] amberLightArray = amberLightData.ToArray();
		Vector3[] whiteLightArray = whiteLightData.ToArray();

		GameObject parentObject = new GameObject("CityLights");

		//Create the amber lights
		GameObject[] amberLightObjects = SpriteLights.CreateLights("City lights amber", amberLightArray, 1f, cityLightsMatAmber, meshIndexFormat);

		for (int i = 0; i < amberLightObjects.Length; i++) {

			//Parenting
			amberLightObjects[i].transform.parent = parentObject.transform;
		}

		//Create the mesh and prefab assets.
		CreateAssets(amberLightObjects, "Amber");

		//Create the white lights
		GameObject[] whiteLightObjects = SpriteLights.CreateLights("City lights white", whiteLightArray, 1f, cityLightsMatWhite, meshIndexFormat);

		for (int i = 0; i < whiteLightObjects.Length; i++) {

			//Parenting
			whiteLightObjects[i].transform.parent = parentObject.transform;
		}

		//Create the mesh and prefab assets.
		CreateAssets(whiteLightObjects, "White");
	}
	*/
	//Enable this once Map-ity is installed.




	void CreateAssets(GameObject[] lightObjects, string name) {

		for (int i = 0; i < lightObjects.Length; i++) {

			string meshName = "CityLights" + name + i.ToString();

			//Does the file already exist?
			string[] guids = AssetDatabase.FindAssets(meshName + " t:mesh");

			if (guids.Length != 0) {

				bool found = true;
				int index = 0;

				while (found) {

					//Try another file name.
					meshName = meshName + index.ToString();

					guids = AssetDatabase.FindAssets(meshName + " t:mesh");

					//Does this name exist?
					if (guids.Length == 0) {

						found = false;
					}

					index++;
				}
			}

#if UNITY_EDITOR
			//Save the mesh to disc.
			AssetDatabase.CreateAsset(lightObjects[i].GetComponent<MeshFilter>().sharedMesh, "Assets/" + meshName + ".asset");
			AssetDatabase.SaveAssets();

			//Save the game object to disc.
			PrefabUtility.CreatePrefab("Assets/" + meshName + ".prefab", lightObjects[i]);
#endif
		}
	}



}
