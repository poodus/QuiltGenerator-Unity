using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class QuiltScript : MonoBehaviour
{

	private bool camAvailable;
	// is the device available
	private WebCamTexture backCam;

	public RawImage colorSample;
	// sample element to accept camera color
	public RawImage background;
	public Text debugText;

	private Vector3 clickPoint;
	private Vector3 mappedClickPoint;

	private float ratioX;
	// what percentage the X pixel is of the image width
	private float ratioY;
	// what percentage the Y pixel is of the image height

	private MeshCollider meshCollider;
	private MeshFilter meshFilter;

	private int meshTriangleIndex = 0;
	private Color selectedColor;

	private int framesSinceTouch = 120;
	public LineRenderer triangleOutline;

	Quilt quilt;

	private bool blockView = true;

	/*
	 * Start()
	 * 
	 * Initialize back-facing camera.
	 * 
	 * */
	private void Start ()
	{
		quilt = new Quilt ();
		meshFilter = GetComponent<MeshFilter> ();
		meshCollider = GetComponent<MeshCollider> ();

		SetupDeviceCamera (false); // only look for backcam
		quilt.DrawQuiltBlock (300, -10, 200, -150, 1);
		LoadMesh ();
		//triangleOutline.positionCount = 3;
	}

	/*
	 * LoadMesh()
	 * 
	 * Grabs the latest mesh from a quilt object and links it to the
	 * canvas' mesh filter and collider.
	 * */
	private void LoadMesh ()
	{
		meshFilter.mesh = quilt.GetMesh ();
		meshCollider.sharedMesh = quilt.GetMesh ();
	}

	private void SetupDeviceCamera (bool front)
	{
		WebCamDevice[] devices = WebCamTexture.devices; // get list of devices

		if (devices.Length == 0) {
			Debug.Log ("No camera detected");
			camAvailable = false;
			return;
		}
			
		for (int i = 0; i < devices.Length; i++) {
			if (front) {
				if (devices [i].isFrontFacing) {
					backCam = new WebCamTexture (devices [i].name, Screen.height, Screen.width);
				}
			} else {
				if (!devices [i].isFrontFacing) {
					backCam = new WebCamTexture (devices [i].name, Screen.height, Screen.width);
				}
			}
		}

		// If back cam not found
		if (backCam == null) {
			Debug.Log ("Unable to find back camera");
			return;
			// TODO don't return if we didn't even want a back camera!
		}

		// At this point we know we have at least one back camera
		backCam.Play (); // camera is now being used by the software
		background.texture = backCam; // camera is now displayed on raw image
		background.rectTransform.localScale = new Vector3 (1f, -1f, 1f);
		camAvailable = true;
	}

	public void ToggleBetweenQuiltAndBlockView ()
	{
		// in blockview, going to quilt view
		if (blockView) {
			triangleOutline.enabled = false;
			quilt.DrawQuiltBlock (300, -10, 200, -150, 3);
			LoadMesh ();
			blockView = false;
		
		// in quilt view, going to block view
		} else {
			// TODO what if user has made edits? blocks should be saved individually...
			triangleOutline.enabled = false;
			quilt.DrawQuiltBlock (300, -10, 200, -150, 1);
			LoadMesh ();
			blockView = true;
		}
	}

	/*
	 * Update()
	 * 
	 * */
	private void Update ()
	{
		// TODO unhighlight the triangle if it's highighted for a while

		if (camAvailable) {

			// Debouncing to wait a few cycles before letting the mesh color change
			if (framesSinceTouch < 5) {
				framesSinceTouch++;
			} else {

				// If there are any touch events available
				if (Input.touches.Length > 0) {
					
					// reset loop counter
					framesSinceTouch = 0;
					// A UI Button wasn't clicked
					if (!CheckIfUIButtonClicked(Input.GetTouch(0).position)) {
						// And if the camera is available
						if (backCam != null) {

							RaycastHit hit;
							/* If mesh was clicked, update which triangle index we're modifying */
							if (Physics.Raycast (Camera.main.ScreenPointToRay (Input.GetTouch (0).position), out hit)) {
								Debug.Log ("mesh was clicked");

								meshCollider = hit.collider as MeshCollider;

								if (meshCollider != null && meshCollider.sharedMesh != null) {
									
									Debug.Log ("mesh triangle index: " + hit.triangleIndex);
									debugText.text = "hit index:" + hit.triangleIndex;

									meshTriangleIndex = hit.triangleIndex;

									// Draw outline around current triangle

									int[] triangles = quilt.GetTriangles ();
									Vector3[] vertices = quilt.GetVertices ();

									// Get vertices for given triangle's index
									Vector3 p0 = vertices [triangles [hit.triangleIndex * 3 + 0]];
									Vector3 p1 = vertices [triangles [hit.triangleIndex * 3 + 1]];
									Vector3 p2 = vertices [triangles [hit.triangleIndex * 3 + 2]];
									Debug.Log ("BEFORE: " + p0.ToString () + " " + p1.ToString () + " " + p2.ToString ());

									Transform hitTransform = hit.collider.transform;

									p0 = hitTransform.TransformPoint (p0);
									p1 = hitTransform.TransformPoint (p1);
									p2 = hitTransform.TransformPoint (p2);

									// Bring out slightly in Z so the outlines are in front of the quilt mesh
									p0.z -= .2f;
									p1.z -= .2f;
									p2.z -= .2f;

									triangleOutline.enabled = true;

									triangleOutline.SetPosition (0, p0);
									triangleOutline.SetPosition (1, p1);
									triangleOutline.SetPosition (2, p2);


								}
							} 

						/* Else, change the color of the current mesh triangle */
							else if (blockView){
								// Touch coordinates in terms of percentage of screen size
								ratioX = Input.GetTouch (0).position.x / Screen.width;
								ratioY = Input.GetTouch (0).position.y / Screen.height;

								selectedColor = backCam.GetPixel ((int)(backCam.width * ratioX), (int)(backCam.height * ratioY));

								// Set the triangle's 3 verticies to the new color
								quilt.SetColors (selectedColor, meshTriangleIndex);

								Debug.Log ("colors updated");
							}

						}

					}
				}

			}

		}

	}

	private bool CheckIfUIButtonClicked(Vector2 position) {
		UnityEngine.EventSystems.PointerEventData pointer = new UnityEngine.EventSystems.PointerEventData (UnityEngine.EventSystems.EventSystem.current);
		pointer.position = position;
		List<UnityEngine.EventSystems.RaycastResult> raycastResults = new List<UnityEngine.EventSystems.RaycastResult> ();
		UnityEngine.EventSystems.EventSystem.current.RaycastAll (pointer, raycastResults);
		if (raycastResults.Count > 0) {
			foreach (var result in raycastResults) {
				if(result.gameObject.name.Equals("Button")) {
					Debug.Log("Button clicked");
					return true;
				}
			}
		}
		return false;
	}

}









/**
 * 
 * Quilt
 * 
 * Defines mesh information, and abstracts methods for manipulating
 * and redrawing the quilt.
 * 
 * */

public class Quilt : MonoBehaviour
{

	Mesh mesh;
	MeshCollider meshCollider;
	MeshRenderer meshRenderer;
	Color[] meshColors;
	bool setColorsCalled = false;
	// if the colors have been set, don't overwrite them with random colors on redraw

	public Quilt ()
	{
		mesh = new Mesh ();
	}

	/*
	 * setColors()
	 * 
	 * Given an a colors and a mesh index, set that mesh triangle with
	 * that index to the input color
	 * 
	*/
	public void SetColors (Color selectedColor, int meshTriangleIndex)
	{
		meshColors [meshTriangleIndex * 3 + 0] = selectedColor;
		meshColors [meshTriangleIndex * 3 + 1] = selectedColor;
		meshColors [meshTriangleIndex * 3 + 2] = selectedColor;
		mesh.colors = meshColors;
		setColorsCalled = true;
	}


	public void OutlineTriangle ()
	{
		//TODO
	}

	/* Getters */
	public Mesh GetMesh ()
	{
		return mesh;
	}

	public int[] GetTriangles ()
	{
		return mesh.triangles;
	}

	public Vector3[] GetVertices ()
	{
		return mesh.vertices;
	}

	/**
	 * 
	 * drawQuiltBlock()
	 * 
	 * Draws some number of quilt blocks with a single mesh object, using
	 * a version of the traditional "Flying Geese" quilt pattern.
	 * 
	 * @params
	 * float triangleWidth - for each triangle in the pattern, when drawn as a single block
	 * float triangleHeight - ""
	 * float triangleDepth - ""
	 * float xOffset - position offset in X axis
	 * float yOffset - position offset in Y axis
	 * int numberOfBlocksAcross - assuming square quilt, so this value squared is the total block count
	 * */

	public void DrawQuiltBlock (float quiltWidth, float triangleDepth, float xOffset, float yOffset, int numberOfBlocksAcross)
	{

		mesh.Clear (false);
		Debug.Log ("Draw Mesh");
		mesh.name = "Quilt";

		float xOffsetInput = xOffset; // Keep the input value because we'll be modifying the offset variables

		int totalNumBlocks = numberOfBlocksAcross * numberOfBlocksAcross; // assuming square design
		int numberOfVertices = 24 * totalNumBlocks; // assuming square design, 24 vertices per block
		float triangleWidth = (int)(quiltWidth / numberOfBlocksAcross) / 2;

		Vector3[] newVertices = new Vector3[numberOfVertices];
		Vector2[] uv = new Vector2[numberOfVertices];

		// Iterate through all the blocks, initializing and calculating all verticies and UVs as we go
		for (int block = 0; block < totalNumBlocks; block++) {
			
			// Is this block on the next row down? Increment yOffset and reset xOffset to 0
			if (block % numberOfBlocksAcross == 0) {
				yOffset += triangleWidth * 2;
				xOffset = xOffsetInput;
			} 
		
			// This block is on the same row but the next column, so just increment xOffset
			else {
				xOffset += triangleWidth * 2;
			}

			// Just using this to make array indicies more legible in the vertex assignment block
			int blockIndexOffset = 24 * block;

			// TODO modify this block. it's a brute force approach and only defines this "flying geese" pattern

			// TOP ROW
			Debug.Log ("block#: " + block + " offset: " + blockIndexOffset);
			newVertices [0 + blockIndexOffset] = new Vector3 (xOffset, yOffset - triangleWidth, triangleDepth);
			newVertices [1 + blockIndexOffset] = new Vector3 (xOffset, yOffset, triangleDepth);
			newVertices [2 + blockIndexOffset] = new Vector3 (triangleWidth + xOffset, yOffset, triangleDepth);

			newVertices [3 + blockIndexOffset] = new Vector3 (xOffset, yOffset - triangleWidth, triangleDepth);
			newVertices [4 + blockIndexOffset] = new Vector3 (triangleWidth + xOffset, yOffset, triangleDepth);
			newVertices [5 + blockIndexOffset] = new Vector3 (triangleWidth + xOffset, yOffset - triangleWidth, triangleDepth);

			newVertices [6 + blockIndexOffset] = new Vector3 (triangleWidth + xOffset, yOffset - triangleWidth, triangleDepth);
			newVertices [7 + blockIndexOffset] = new Vector3 (triangleWidth + xOffset, yOffset, triangleDepth);
			newVertices [8 + blockIndexOffset] = new Vector3 ((triangleWidth * 2) + xOffset, yOffset-triangleWidth, triangleDepth);

			newVertices [9 + blockIndexOffset] = new Vector3 (triangleWidth + xOffset, yOffset, triangleDepth);
			newVertices [10 + blockIndexOffset] = new Vector3 ((triangleWidth * 2) + xOffset, yOffset, triangleDepth);
			newVertices [11 + blockIndexOffset] = new Vector3 ((triangleWidth * 2) + xOffset, yOffset - triangleWidth, triangleDepth);

			// BOTTOM ROW
			newVertices [12 + blockIndexOffset] = new Vector3 (xOffset, yOffset - (triangleWidth * 2), triangleDepth);
			newVertices [13 + blockIndexOffset] = new Vector3 (xOffset, yOffset - triangleWidth, triangleDepth);
			newVertices [14 + blockIndexOffset] = new Vector3 (triangleWidth + xOffset, yOffset - (triangleWidth * 2), triangleDepth);

			newVertices [15 + blockIndexOffset] = new Vector3 (xOffset, yOffset - triangleWidth, triangleDepth);
			newVertices [16 + blockIndexOffset] = new Vector3 (triangleWidth + xOffset, yOffset - triangleWidth, triangleDepth);
			newVertices [17 + blockIndexOffset] = new Vector3 (triangleWidth + xOffset, yOffset - (triangleWidth * 2), triangleDepth);

			newVertices [18 + blockIndexOffset] = new Vector3 (triangleWidth + xOffset, yOffset - (triangleWidth * 2), triangleDepth);
			newVertices [19 + blockIndexOffset] = new Vector3 (triangleWidth + xOffset, yOffset - triangleWidth, triangleDepth);
			newVertices [20 + blockIndexOffset] = new Vector3 ((triangleWidth * 2) + xOffset, yOffset - triangleWidth, triangleDepth);

			newVertices [21 + blockIndexOffset] = new Vector3 (triangleWidth + xOffset, yOffset - (triangleWidth * 2), triangleDepth);
			newVertices [22 + blockIndexOffset] = new Vector3 ((triangleWidth * 2) + xOffset, yOffset - triangleWidth, triangleDepth);
			newVertices [23 + blockIndexOffset] = new Vector3 ((triangleWidth * 2) + xOffset, yOffset - (triangleWidth * 2), triangleDepth);

			uv [0 + blockIndexOffset] = new Vector2 (0, .5f);
			uv [1 + blockIndexOffset] = new Vector2 (0, 1);
			uv [2 + blockIndexOffset] = new Vector2 (.5f, 1);

			uv [3 + blockIndexOffset] = new Vector2 (0, .5f);
			uv [4 + blockIndexOffset] = new Vector2 (.5f, 1);
			uv [5 + blockIndexOffset] = new Vector2 (.5f, .5f);

			uv [6 + blockIndexOffset] = new Vector2 (.5f, .5f);
			uv [7 + blockIndexOffset] = new Vector2 (.5f, 1);
			uv [8 + blockIndexOffset] = new Vector2 (1, .5f);

			uv [9 + blockIndexOffset] = new Vector2 (.5f, 1);
			uv [10 + blockIndexOffset] = new Vector2 (1, 1);
			uv [11 + blockIndexOffset] = new Vector2 (1, .5f);

			uv [12 + blockIndexOffset] = new Vector2 (0, 0); // 0
			uv [13 + blockIndexOffset] = new Vector2 (0, .5f); // 1
			uv [14 + blockIndexOffset] = new Vector2 (.5f, .5f); // 2

			uv [15 + blockIndexOffset] = new Vector2 (0, 0); // 3
			uv [16 + blockIndexOffset] = new Vector2 (.5f, .5f); // 4
			uv [17 + blockIndexOffset] = new Vector2 (.5f, 0); // 5

			uv [18 + blockIndexOffset] = new Vector2 (.5f, 0); // 6
			uv [19 + blockIndexOffset] = new Vector2 (.5f, .5f); // 7
			uv [20 + blockIndexOffset] = new Vector2 (1, 0); // 8

			uv [21 + blockIndexOffset] = new Vector2 (.5f, .5f); // 9
			uv [22 + blockIndexOffset] = new Vector2 (1, .5f); // 10
			uv [23 + blockIndexOffset] = new Vector2 (1, 0); // 11
		}

		// Initialize triangles and normals arrays
		int[] tri = new int[numberOfVertices];
		Vector3[] normals = new Vector3[numberOfVertices];

		for (int i = 0; i < numberOfVertices; i++) {
			tri [i] = i;
			normals [i] = -Vector3.forward;
		}

		Color[] oldMeshColors = meshColors;
		meshColors = new Color[numberOfVertices];

		// Is the mesh colors array growing or shrinking?

		if (setColorsCalled) {
			for (int meshTriangleIndex = 0; meshTriangleIndex < numberOfVertices; meshTriangleIndex += 3) {
				// New mesh has more verticies
				if (oldMeshColors.Length < numberOfVertices) {
					// fill up new array by resetting oldMeshColors index when it exceeds 24
					meshColors [meshTriangleIndex + 0] = oldMeshColors [meshTriangleIndex % 24];
					meshColors [meshTriangleIndex + 1] = oldMeshColors [meshTriangleIndex % 24];
					meshColors [meshTriangleIndex + 2] = oldMeshColors [meshTriangleIndex % 24];
				}
			 	// New mesh has less vertices
				else {
					meshColors [meshTriangleIndex + 0] = oldMeshColors [meshTriangleIndex];
					meshColors [meshTriangleIndex + 1] = oldMeshColors [meshTriangleIndex];
					meshColors [meshTriangleIndex + 2] = oldMeshColors [meshTriangleIndex];
				}
			}
		} else {

			// Initialize colors
			for (int meshTriangleIndex = 0; meshTriangleIndex < numberOfVertices; meshTriangleIndex += 3) {
				float randomFloat = Random.Range (0f, 1f);
				Color randomColor = new Color (randomFloat, randomFloat, randomFloat);
				meshColors [meshTriangleIndex + 0] = randomColor;
				meshColors [meshTriangleIndex + 1] = randomColor;
				meshColors [meshTriangleIndex + 2] = randomColor;
			}
		}

		mesh.vertices = newVertices;

		mesh.colors = meshColors;
		mesh.triangles = tri;
		mesh.normals = normals;
		mesh.uv = uv;
	}


}