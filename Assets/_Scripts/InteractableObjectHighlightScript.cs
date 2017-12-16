using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class InteractableObjectHighlightScript : MonoBehaviour {

	[SerializeField] private Color starting;
	[SerializeField] private Color highlight;
	[SerializeField] private string uiDesc = "to drink!";

	[SerializeField] private float drunk = 0.1f;
	[SerializeField] private float hp = 0.1f;
	[SerializeField] private float combo = 0.1f;
	
	
	
	// Use this for initialization
	void Start () {
		GetComponent<MeshRenderer>().material.DOColor(highlight, "_EmissionColor", 1f).SetLoops(-1, LoopType.Yoyo);
		
	}

	private void OnTriggerEnter(Collider other) {
		if (other.CompareTag("Interactable")) {
			PlayerController.instance.interactableObject = gameObject;
			PlayerController.instance.interactableText.text = "[<size=130%><b>F</b><size=100%> " + uiDesc + "]";
		}
	}

	private void OnTriggerExit(Collider other) {
		if (other.CompareTag("Interactable")) {
			other.gameObject.GetComponentInParent<PlayerController>().interactableObject = null;
			other.gameObject.GetComponentInParent<PlayerController>().interactableText.text = "";
		}
	}

	public void Use(out bool hpChange, out bool drunkChange) {
		drunkChange = drunk != 0;
		hpChange = hp != 0;
		
		PlayerController.instance.drunk += drunk;
		PlayerController.instance.combo += combo;
		PlayerController.instance.hp += hp;
		
		PlayerController.instance.interactableObject = null;
		PlayerController.instance.interactableText.text = "";
		
		Destroy(gameObject);
	}

	// Update is called once per frame
	void Update () {
		
	}
}
