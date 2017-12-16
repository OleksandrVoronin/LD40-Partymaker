using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class SpeakerScript : MonoBehaviour {

	[SerializeField] private float bpm = 140;
	[SerializeField] private float punch = 0.05f;

	private AudioSource _audioSource;
	
	// Use this for initialization
	void Start () {
		transform.DOPunchScale(Vector3.one * punch, 60 / bpm, 0, 0).SetLoops(-1);
	}
}
