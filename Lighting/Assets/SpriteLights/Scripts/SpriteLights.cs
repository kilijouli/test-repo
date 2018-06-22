using UnityEngine;
using System.Collections;


public class SpriteLights : MonoBehaviour {

	//The 16 bit vertex limit for one mesh is 65534, which is 21844 triangles.
	//The 32 bit vertex limit for one mesh is 4294967295 (2^32), which is 1431655765 triangles
	private static int maxTriangls32 = 1431655765; 
	private static int maxTriangls16 = 21844;
	
	//This struct holds the data for each individual light.
	public struct LightData{

		public Vector3 position;
		public Quaternion rotation;
		public float size;
		public float brightness;
		public Color frontColor; //The alpha component of the front color is used as a brightness offset instead.
		public Color backColor;		
		public float strobeID;
		public float strobeGroupID;
	}

	//Get the screen size of an object in pixels, given its distance and diameter.
	float DistanceAndDiameterToPixelSize(float distance, float diameter){
		
		float pixelSize = (diameter * Mathf.Rad2Deg * Screen.height) / (distance * Camera.main.fieldOfView);
		return pixelSize;
	}

	//Get the distance of an object, given its screen size in pixels and diameter.
	float PixelSizeAndDiameterToDistance(float pixelSize, float diameter){

		float distance = (diameter * Mathf.Rad2Deg * Screen.height) / (pixelSize * .1f * Camera.main.fieldOfView);
		return distance;
	}

	//Get the diameter of an object, given its screen size in pixels and distance. 
	float PixelSizeAndDistanceToDiameter(float pixelSize, float distance){

		float diameter = (pixelSize*.1f * distance * Camera.main.fieldOfView) / (Mathf.Rad2Deg * Screen.height);
		return diameter;
	}

	//Get the linear factor which is used to scale the light so it stays the same
	//size in pixels as the distance increases.
	public static float GetScaleFactor(float FOV, float screenHeight){

		return Camera.main.fieldOfView / (Mathf.Rad2Deg * Screen.height);
	}

	//Call this function before displaying the lights.
	public static void Init(float strobeTimeStep, float globalBrightnessOffset, float FOV, float screenHeight){

		//Calculate scale factor for the shader.
		float scaleFactor = GetScaleFactor(FOV, screenHeight);

		//Set global shader variables.
		Shader.SetGlobalFloat("_StrobeTimeStep", strobeTimeStep);
		Shader.SetGlobalFloat("_ScaleFactor", scaleFactor*.1f);
		Shader.SetGlobalFloat("_GlobalBrightnessOffset", globalBrightnessOffset);
	}

	//Overload for city lights (simple shader)
	public static GameObject[] CreateLights(string name, Vector3[] positions, float size, Material material, UnityEngine.Rendering.IndexFormat meshIndexFormat) {

		GameObject[] lightObjects = CreateLightsAll(name, null, positions, size, material, meshIndexFormat);

		return lightObjects;
	}

	//Overload for all other lights.
	public static GameObject[] CreateLights(string name, LightData[] lightData, Material material, UnityEngine.Rendering.IndexFormat meshIndexFormat) {

		GameObject[] lightObjects = CreateLightsAll(name, lightData, null, 0, material, meshIndexFormat);

		return lightObjects;
	}


	//Create a light mesh object. Cram as much lights as you can into the LightData array. If there are too many lights for one GameObject, it will
	//automatically be split into multiple objects.
	//When creating city lights, use the function overload which takes a position array instead of a lightData array. 
	private static GameObject[] CreateLightsAll(string name, LightData[] lightData, Vector3[] positions, float simpleShaderLightSize, Material material, UnityEngine.Rendering.IndexFormat meshIndexFormat) {

		int transformAmount = 0;
		
		if(lightData != null) {

			transformAmount = lightData.Length;
		}

		else {

			transformAmount = positions.Length;
		}
		
		int lightsInThisBatch;
		int maxTriangles;

		if (meshIndexFormat == UnityEngine.Rendering.IndexFormat.UInt32) {

			maxTriangles = maxTriangls32;
		}

		else {

			maxTriangles = maxTriangls16;
		}

		//The triangle limit is 21844 (16 bit) or 1431655765 (32 bit) per mesh, so split the objects up if needed.
		int meshAmount = (int)Mathf.Ceil(transformAmount / (float)maxTriangles);

		GameObject[] lightObjects = new GameObject[meshAmount];

		//Get the remainder.
		int remainder = (int)(transformAmount % (float)maxTriangles);

		//Loop through the mesh batches.
		for (int l = 0; l < meshAmount; l++) {

			//Get the amount of lights in this mesh batch.
			if (meshAmount == 1) {

				lightsInThisBatch = transformAmount;
			}

			else {

				//Last mesh batch.
				if (l == (meshAmount - 1)) {

					if (remainder == 0) {

						lightsInThisBatch = maxTriangles;
					}

					else {

						lightsInThisBatch = remainder;
					}
				}

				else {

					lightsInThisBatch = maxTriangles;
				}
			}

			bool directional = false;
			bool omnidirectional = false;
			bool strobe = false;
			bool papi = false;
			bool simple = false;

			int vertexCount = lightsInThisBatch * 3;

			Vector4[] corners = new Vector4[vertexCount];
			Vector2[] uvs = new Vector2[vertexCount];
			Vector4[] triangleCorners = new Vector4[3];
			Vector2[] triangleUvs = new Vector2[3];
			Vector3[] normals = null;

			Vector3 normal = Vector3.one;
			Vector3 right = Vector3.one;
			Vector3 up = Vector3.one;

			//Create temporary arrays.
			Vector3[] centers = new Vector3[vertexCount];			
			int[] triangles = new int[vertexCount];
			int[] indices = new int[vertexCount];
						
			Vector2[] uv2Channel = null;
			Vector2[] uv3Channel = null;
			Vector2[] uv4Channel = null;
			Color[] colorChannel = null;

			Vector4 corner0 = Vector4.zero;
			Vector4 corner1 = Vector4.zero;
			Vector4 corner2 = Vector4.zero;

			//Generate a triangle which fits over a quad.
			//Taken out of the function for performance.
			//GenerateTriangle(out triangleCorners, out triangleUvs);
			
			//Set the points, counter clockwise as an upright triangle, with the left 
			//corner as point 0, right corner point 1, and top is point 2.	
			//X and Y are computed like this:
			//x = (1f / Mathf.Tan(60f * Mathf.Deg2Rad)) + 0.5f;
			//y = // (0.5f / Mathf.Tan(30f * Mathf.Deg2Rad)) + 0.5f;
			triangleCorners[0] = new Vector4(-1.07735f, -0.5f, 0.0f, 1.0f);
			triangleCorners[1] = new Vector4(1.07735f, -0.5f, 0.0f, 1.0f);
			triangleCorners[2] = new Vector4(0.0f, 1.366025f, 0.0f, 1.0f);

			//Computed like this: 
			//float u = Math3d.NormalizeComplex(localPoints[i].x, -0.5f, 0.5f);
			//float v = Math3d.NormalizeComplex(localPoints[i].y, -0.5f, 0.5f);
			triangleUvs[0].x = -0.57735f;
			triangleUvs[0].y = 0f;
			triangleUvs[1].x = 1.57735f;
			triangleUvs[1].y = 0f;
			triangleUvs[2].x = 0.5f;
			triangleUvs[2].y = 1.866025f;

			if (lightData != null) {

				normals = new Vector3[vertexCount];

				//What is stored in here, depends on the shader.
				uv2Channel = new Vector2[vertexCount];
				uv3Channel = new Vector2[vertexCount];				
				uv4Channel = new Vector2[vertexCount];
				colorChannel = new Color[vertexCount];

				if (material.shader.name.Contains("Directional")) {

					directional = true;
				}

				if (material.shader.name.Contains("Omnidirectional")) {

					omnidirectional = true;
				}

				if (material.shader.name.Contains("Strobe")) {

					strobe = true;
				}

				if (material.shader.name.Contains("PAPI")) {

					papi = true;
				}
			}

			else {

				simple = true;

				corner0 = new Vector4(triangleCorners[0].x, triangleCorners[0].y, triangleCorners[0].z, simpleShaderLightSize);
				corner1 = new Vector4(triangleCorners[1].x, triangleCorners[1].y, triangleCorners[1].z, simpleShaderLightSize);
				corner2 = new Vector4(triangleCorners[2].x, triangleCorners[2].y, triangleCorners[2].z, simpleShaderLightSize);
			}
			
			//Loop through all the lights and set them up.
			for (int i = 0; i < lightsInThisBatch; i++) {

				float size = 0;
				int e = i * 3;
				int e1 = e + 1;
				int e2 = e + 2;
				int index = (l * maxTriangles) + i;

				if (lightData != null) {

					size = lightData[index].size;

					centers[e] = lightData[index].position;
					centers[e1] = lightData[index].position;
					centers[e2] = lightData[index].position;

					corners[e] = new Vector4(triangleCorners[0].x, triangleCorners[0].y, triangleCorners[0].z, size);
					corners[e1] = new Vector4(triangleCorners[1].x, triangleCorners[1].y, triangleCorners[1].z, size);
					corners[e2] = new Vector4(triangleCorners[2].x, triangleCorners[2].y, triangleCorners[2].z, size);
				}

				else {

					size = simpleShaderLightSize;

					centers[e] = positions[index];
					centers[e1] = positions[index];
					centers[e2] = positions[index];
					
					corners[e] = corner0;
					corners[e1] = corner1;
					corners[e2] = corner2;
				}

				uvs[e] = triangleUvs[0];
				uvs[e1] = triangleUvs[1];
				uvs[e2] = triangleUvs[2];

				if (lightData != null) {

					if (papi || omnidirectional) {

						up = Math3d.GetUpVector(lightData[index].rotation);
					}

					if (!simple) {

						normal = -Math3d.GetForwardVector(lightData[index].rotation);
						normals[e] = normal;
						normals[e1] = normal;
						normals[e2] = normal;
					}

					if (papi) {

						right = Math3d.GetRightVector(lightData[index].rotation);
						uv2Channel[e] = right;
						uv2Channel[e1] = right;
						uv2Channel[e2] = right;

						uv3Channel[e] = up;
						uv3Channel[e1] = up;
						uv3Channel[e2] = up;

						//Compose the z vector.
						Vector2 zVector = new Vector2(right.z, up.z);
						uv4Channel[e] = zVector;
						uv4Channel[e1] = zVector;
						uv4Channel[e2] = zVector;
					}

					if (directional) {

						//Create the back color.
						Vector2 RG = new Vector2(lightData[index].backColor.r, lightData[index].backColor.g);
						Vector2 BA = new Vector2(lightData[index].backColor.b, lightData[index].backColor.a);

						uv2Channel[e] = RG;
						uv2Channel[e1] = RG;
						uv2Channel[e2] = RG;

						uv3Channel[e] = BA;
						uv3Channel[e1] = BA;
						uv3Channel[e2] = BA;
					}

					if (omnidirectional) {

						//Create the back color.
						Vector2 RG = new Vector2(lightData[index].backColor.r, lightData[index].backColor.g);

						uv2Channel[e] = RG;
						uv2Channel[e1] = RG;
						uv2Channel[e2] = RG;

						uv3Channel[e] = up;
						uv3Channel[e1] = up;
						uv3Channel[e2] = up;

						//Compose the z vector. The x component is used for the blue component of the back color.
						Vector2 zVector = new Vector2(lightData[index].backColor.b, up.z);
						uv4Channel[e] = zVector;
						uv4Channel[e1] = zVector;
						uv4Channel[e2] = zVector;
					}

					if (strobe) {

						//Store the light and group id in the color channel.
						Color id = new Color(lightData[index].strobeID, lightData[index].strobeGroupID, 0, 0);

						colorChannel[e] = id;
						colorChannel[e1] = id;
						colorChannel[e2] = id;
					}

					if (directional || omnidirectional) {

						//Create the front color. Note that the light brightness value is stored in the alpha channel.
						Color frontColor = new Color(lightData[index].frontColor.r, lightData[index].frontColor.g, lightData[index].frontColor.b, lightData[index].brightness);

						colorChannel[e] = frontColor;
						colorChannel[e1] = frontColor;
						colorChannel[e2] = frontColor;
					}
				}

				triangles[e] = e;
				triangles[e1] = e1;
				triangles[e2] = e2;

				indices[e] = e;
				indices[e1] = e1;
				indices[e2] = e2;
			}

			//Create a new gameObject.
			GameObject lightObject = new GameObject(name + " " + l);
			lightObjects[l] = lightObject;

			//Add the required components to the game object.
			MeshFilter meshFilter = lightObject.AddComponent<MeshFilter>();
			MeshRenderer meshRenderer = lightObject.AddComponent<MeshRenderer>();

			//Create a new mesh.
			meshFilter.sharedMesh = new Mesh();

			//Apply the mesh properties.   
			meshFilter.sharedMesh.indexFormat = meshIndexFormat;
			meshFilter.sharedMesh.vertices = centers;
			meshFilter.sharedMesh.tangents = corners; //The mesh corner is stored in the tangent channel. The scale is stored in the w component.		
			meshFilter.sharedMesh.uv = uvs;
			meshFilter.sharedMesh.triangles = triangles;

			if (!simple) {

				meshFilter.sharedMesh.normals = normals;
				meshFilter.sharedMesh.colors = colorChannel; //Front color for directional lights. Alpha is the brightness reduction. //ID for strobe lights. 
				meshFilter.sharedMesh.uv2 = uv2Channel; //RG(BA) back color for directional lights. //Rotation vector for omnidirectional lights and PAPIs. 
				meshFilter.sharedMesh.uv3 = uv3Channel; //(RG)BA back color for directional lights. Alpha is for invisibility. //Rotation vector for omnidirectional lights and PAPIS. 
				meshFilter.sharedMesh.uv4 = uv4Channel; //The UV channels only hold two floats, so use a third UV channel to store the Z components of the vectors.
			}

			//Set the indices.
			meshFilter.sharedMesh.SetIndices(indices, MeshTopology.Triangles, 0);

			//Set the gameObject properties;
			meshRenderer.sharedMaterial = material;
			meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			meshRenderer.receiveShadows = false;
			meshRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
			meshRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
			
		}

		return lightObjects;
	}

		//Convert vector type.
		private static Vector4[] Vector3ToVector4(Vector3[] input){

		Vector4[] output = new Vector4[input.Length]; 

		for(int i = 0; i <input.Length; i++){

			output[i] = new Vector4(input[i].x, input[i].y, input[i].z, 1);
		}

		return output;
	}

	//Calculates the triangle points (counter clockwise) of a triangle placed over a unit quad.
	//This way a triangle is created which fits over a texture.
	private static void GenerateTriangle(out Vector4[] points, out Vector2[] uvs, float size, Quaternion rotation){

		uvs = new Vector2[3];

		//Calculate the horizontal displacement.
		float x = 1.07735f; // (1f / Mathf.Tan(60f * Mathf.Deg2Rad)) + 0.5f;

		//Calculate the vertical displacement.
		float y = 1.366025f; // (0.5f / Mathf.Tan(30f * Mathf.Deg2Rad)) + 0.5f;

		//Set the points, counter clockwise as an upright triangle, with the left 
		//corner as point 0, right corner point 1, and top is point 2.	
		Vector3[] localPoints = new Vector3[3]; 
		localPoints[0] = new Vector3(-x, -0.5f) * size;
		localPoints[1] = new Vector3(x, -0.5f) * size;
		localPoints[2] = new Vector3(0, y) * size;

		//Set the uv's
		for(int i = 0; i < 3; i++){

			float u = Math3d.NormalizeComplex(localPoints[i].x / size, -0.5f, 0.5f);
			float v = Math3d.NormalizeComplex(localPoints[i].y / size, -0.5f, 0.5f);
			uvs[i] = new Vector2(u, v);
		}

		//Rotate the points
		for(int i = 0; i < 3; i++){

			localPoints[i] = rotation * localPoints[i];
		}

		//Convert to vector4
		points = Vector3ToVector4(localPoints);
	}

	//Overload without size and rotation.
	private static void GenerateTriangle(out Vector4[] points, out Vector2[] uvs) {

		uvs = new Vector2[3];

		//Calculate the horizontal displacement.
		float x = 1.07735f; // (1f / Mathf.Tan(60f * Mathf.Deg2Rad)) + 0.5f;

		//Calculate the vertical displacement.
		float y = 1.366025f; // (0.5f / Mathf.Tan(30f * Mathf.Deg2Rad)) + 0.5f;

		//Set the points, counter clockwise as an upright triangle, with the left 
		//corner as point 0, right corner point 1, and top is point 2.	
		Vector3[] localPoints = new Vector3[3];
		localPoints[0] = new Vector3(-x, -0.5f);
		localPoints[1] = new Vector3(x, -0.5f);
		localPoints[2] = new Vector3(0, y);

		//Computed like this: 
		//float u = Math3d.NormalizeComplex(localPoints[i].x, -0.5f, 0.5f);
		//float v = Math3d.NormalizeComplex(localPoints[i].y, -0.5f, 0.5f);
		uvs[0].x = -0.57735f;
		uvs[0].y = 0f;
		uvs[1].x = 1.57735f;
		uvs[1].y = 0f;
		uvs[2].x = 0.5f;
		uvs[2].y = 1.866025f;

		//Convert to vector4
		points = Vector3ToVector4(localPoints);
	}

}
