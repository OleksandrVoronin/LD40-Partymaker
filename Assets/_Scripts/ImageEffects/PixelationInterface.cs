using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class PixelationInterface : MonoBehaviour {

	public int chunks = 64;
	private Material effectsMaterial;
	
	// Use this for initialization
	void Start () {
		effectsMaterial = new Material(Shader.Find("Hidden/Pixelation"));	
	}

	private void OnRenderImage(RenderTexture src, RenderTexture dest) {
		float aspectRatio = Camera.main.aspect;
		
		Vector2 chunkCount = new Vector2(chunks, chunks/aspectRatio);
		Vector2 chunkSize = new Vector2(1f/chunkCount.x, 1f/chunkCount.y);
		
		effectsMaterial.SetVector("chunkCount", chunkCount);
		effectsMaterial.SetVector("chunkSize", chunkSize);
		
		Graphics.Blit(src, dest, effectsMaterial);
	}
}
