using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[AddComponentMenu("Image Effects/Custom/ExplosionEffect")]
public class ExplosionShaderInterface : MonoBehaviour {

    public Material EffectsMaterial;

    public float displacementRatio = 0.2f;

    public float waves;

    private float ratio;
    public bool work = true;
    public bool negativeColors = true;
    public float timeMultiplier = 1f; 

    void Start()
    {
        ratio = Screen.height / Screen.width;
    }

    void OnRenderImage(RenderTexture src, RenderTexture dst) {
        EffectsMaterial.SetFloat("_DisplacementPower", displacementRatio);
        EffectsMaterial.SetFloat("_Waves", waves);
        EffectsMaterial.SetFloat("_Ratio", ratio);
        EffectsMaterial.SetInt("_Work", work ? 1 : -1);
        EffectsMaterial.SetInt("_NegativeColors", negativeColors ? 1 : -1);
        EffectsMaterial.SetFloat("_TimeMultiplier", timeMultiplier);

        Graphics.Blit(src, dst, EffectsMaterial);
    }
}
