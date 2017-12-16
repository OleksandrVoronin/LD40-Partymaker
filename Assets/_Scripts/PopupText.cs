using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class PopupText : MonoBehaviour {

	// Use this for initialization
	void Start () {
		GetComponent<TextMeshPro>().DOColor(Color.clear, 1.5f).SetEase(Ease.InCirc).OnComplete(() => Destroy(gameObject));
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
