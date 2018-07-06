/*
	VertexDirt plug-in for Unity
	Copyright 2014-2017, Zoltan Farago, All rights reserved.
*/

using UnityEngine;

[ExecuteInEditMode]
public class VDSampler : MonoBehaviour {
		
	public Texture2D tex;
	public Color32[] lum;

	void Awake() {
		 tex = new Texture2D (VertexDirt.sampleWidth, VertexDirt.sampleHeight, TextureFormat.RGB24, true);
	}

	void OnPostRender () {
		tex.ReadPixels (new Rect(0, 0, VertexDirt.sampleWidth, VertexDirt.sampleHeight), 0, 0);
		lum = tex.GetPixels32(tex.mipmapCount-1);
		//
		//var bytes = tex.EncodeToPNG();
		//System.IO.File.WriteAllBytes(Application.dataPath + "/../saved/SavedScreen"+VertexDirt.vertexSample.index+".png", bytes);
		//
		VertexDirt.SetColorSample(lum[0]);
	}
}