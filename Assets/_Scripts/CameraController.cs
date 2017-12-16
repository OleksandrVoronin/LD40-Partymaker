using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class CameraController : MonoBehaviour {

	[SerializeField] private float moveTime = 0.4f;
	private Tween moveTween;

	private Vector3 lastMovePosition;
	
	public void MoveCamera(Vector3 newLocation) {
		moveTween.Kill();
		moveTween = transform.DOMove(newLocation, moveTime);

		lastMovePosition = newLocation;
	}

	public void RestoreCameraPosition() {
		MoveCamera(lastMovePosition);
	}

}
