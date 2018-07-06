/* 
	VertexDirt plug-in for Unity
	Copyright 2014-2017, Zoltan Farago, All rights reserved.
*/

// @script ExecuteInEditMode()
using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class VDColorHandler : VDColorHandlerBase 
{
	void Awake() 
	{
		if (Application.isPlaying) 
		{
			MeshFilter mf = GetComponent<MeshFilter>();
			SetMesh();
			mf.mesh.colors32 = colors;
		}
	}	
	void Update() 
	{
		if (Application.isEditor && !Application.isPlaying) 
		{
			SetMesh();
		}
	}
	
	public void SetColors()
	{
		if (coloredMesh) {
			MeshFilter mf = GetComponent<MeshFilter>();
			coloredMesh.colors32 = colors;
			mf.mesh = coloredMesh;
		}
	}
	public void SetMesh() {
		MeshFilter mf = GetComponent<MeshFilter>();
		if (!GetComponent<MeshFilter>().sharedMesh) 
		{
			GetComponent<MeshFilter>().sharedMesh = originalMesh;
		}
		if (!coloredMesh && mf.sharedMesh && Application.isEditor && !Application.isPlaying) 
		{
			originalMesh = mf.sharedMesh;
			coloredMesh = Mesh.Instantiate(mf.sharedMesh) as Mesh;  //make a deep copy
			coloredMesh.name = mf.sharedMesh.name;
			SetColors();
		}		
		if (colors != tempColors && Application.isEditor && !Application.isPlaying)
		{
			tempColors = colors;
			SetColors();
		}
	}

	void OnDisable() 
	{
		gameObject.GetComponent<MeshFilter>().mesh = originalMesh;
	}
	void OnEnable() 
	{
		MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
		if (!originalMesh) 
		{
			originalMesh = meshFilter.sharedMesh;
		}	
		if (coloredMesh) 
		{
			meshFilter.mesh = coloredMesh;
		}
	}
}