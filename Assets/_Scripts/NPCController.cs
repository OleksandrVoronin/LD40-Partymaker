using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class NPCController : MonoBehaviour {

	public string[] initialHitText = {"<size=70%>What did you just..!?", "<size=70%>Come here, you...", "Ouch!", ">_<"};
	public string[] someoneElseHitText = {"<size=70%>Don't touch my friend!", "I'll..", "You prick!", "Get him!"};
	
	public enum State {Dancing = 0, Idling = 1, WalkingAround = 2, Hostile = 3};
	[SerializeField] private State currentState = State.Idling;

	private Animator _animator; 
	private string[] danceAnimations = {"gangam", "salsa" };


	[Header("Customization/Randomization")] 
	[SerializeField] private Texture[] _textures;
	[SerializeField] private GameObject hat;
	
	[SerializeField] private Vector2 actionCooldownRange = new Vector2(9, 15);
	private bool boredOfCurrentTask = true;

	[SerializeField] private float hitRange;


	private bool hostileFollow = false;
	private float hp = 1f;
	private bool alive = true;

	private bool invul = false;

	[SerializeField] private SphereCollider[] attackSpheres;
	
	// Use this for initialization
	void Start () {
		_animator = GetComponent<Animator>();
		UpdateAnimations();

		_animator.SetInteger("idleState", Random.Range(0,5));
		
		SkinnedMeshRenderer[] renderers = GetComponentsInChildren<SkinnedMeshRenderer>();
		for (int i = 0; i < renderers.Length; i++) {
			renderers[i].material.SetTexture("_MainTex", _textures[Random.Range(0, _textures.Length)]);
		}
		
		hat?.SetActive(Random.Range(0, 4) == 0);
		transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
		
		StartCoroutine(AimAtPlayer());

		PlayerController.instance.killsLeft++;
		PlayerController.instance.npcs.Add(this);
	}
	
	// Update is called once per frame
	void Update () {
		
		
		if (currentState != State.Hostile) {
			StopCoroutine(FollowAndPunchPlayer());
			hostileFollow = false;
			
			if (boredOfCurrentTask) {
				StartCoroutine(TaskScheduler());
			}
		} else {
			if(!hostileFollow)
				StartCoroutine(FollowAndPunchPlayer());
		}

		_animator.SetBool("walking", GetComponent<NavMeshAgent>().velocity.magnitude > 0.2f && GetComponent<NavMeshAgent>().speed < 1.6f);
		_animator.SetBool("running", GetComponent<NavMeshAgent>().velocity.magnitude >= 1.6f);
		
	}

	public IEnumerator Stun() {
		currentState = State.Hostile;
		hostileFollow = true;
		
		//animation change
		
		yield return new WaitForSeconds(0.5f);

		StartCoroutine(AimAtPlayer());
		StartCoroutine(FollowAndPunchPlayer());
	}

	public void Stunned() {
		StartCoroutine(Stun());
	}

	private IEnumerator FollowAndPunchPlayer() {
		hostileFollow = true;
		while (alive) {
			GetComponent<NavMeshAgent>().speed = 4.1f;
			GetComponent<NavMeshAgent>().destination = PlayerController.instance.transform.position;

			if ((PlayerController.instance.transform.position - transform.position).magnitude <= hitRange) {
				GetComponent<NavMeshAgent>().destination = transform.position;
				
				_animator.SetInteger("hitNumber", Random.Range(0, 3));
				_animator.SetTrigger("hit");
				
				//pre-hit
				yield return new WaitForSeconds(.15f);

				for (int i = 0; i < attackSpheres.Length; i++) {
					attackSpheres[i].enabled = true;
				}
				
				//hit activated
				yield return new WaitForSeconds(.35f);
		
				for (int i = 0; i < attackSpheres.Length; i++) {
					attackSpheres[i].enabled = false;
				}
		
				//hit de-activated -> backswing
				yield return new WaitForSeconds(Random.Range(.15f, .6f));
			}
			
			yield return null;
		}

		yield return null;
	}

	public void OnTriggerEnter(Collider other) {
		if (other.CompareTag("PlayerPunch") && alive && !invul) {
			invul = true;
			
			PlayerController.instance.SpawnAudioPopup(PlayerController.instance.kick);
			GetComponentInChildren<SkinnedMeshRenderer>().material.SetColor("_EmissionColor", Color.black);
			GetComponentInChildren<SkinnedMeshRenderer>().material.DOKill();
			GetComponentInChildren<SkinnedMeshRenderer>().material.DOColor(new Color(0.5f, 0f, 0f), "_EmissionColor", 0.1f)
				.SetLoops(2, LoopType.Yoyo).OnComplete(() => invul = false);

			for (int i = 0; i < PlayerController.instance.npcs.Count; i++) {
				if (PlayerController.instance.npcs[i] != null && PlayerController.instance.npcs[i].gameObject.GetInstanceID() != gameObject.GetInstanceID() &&
				    PlayerController.instance.npcs[i].currentState != State.Hostile &&
				    (PlayerController.instance.npcs[i].transform.position - transform.position).magnitude < PlayerController.instance.aggroRange
				    && Random.Range(0, 12) == 0) {
					PlayerController.instance.npcs[i].Stunned();
					
					if (currentState != State.Hostile) {
						PlayerController.instance.SpawnPopup(someoneElseHitText[Random.Range(0, someoneElseHitText.Length)], PlayerController.instance.npcs[i].transform.position + new Vector3(0, 2f, 0));
					}
				}
			}

			hp -= 0.34f;
			StopAllCoroutines();
			
			if (hp > 0f) {
				if (currentState != State.Hostile) {
					PlayerController.instance.SpawnPopup(initialHitText[Random.Range(0, initialHitText.Length)], transform.position + new Vector3(0, 2f, 0));
				}
				Stunned();
			} else {
				Die();
			}
		}
	}

	private void Die() {
		
		alive = false;

		PlayerController.instance.killsLeft--;
				
		//just in case
		currentState = State.Hostile;
		hostileFollow = true;
				
		for (int i = 0; i < attackSpheres.Length; i++) {
			attackSpheres[i].enabled = false;
		}

		_animator.SetBool("running", false);
		_animator.SetBool("walking", false);
		
		PlayerController.instance.AddScore(200);
		
		_animator.SetBool("dead", true);
		
		_animator.CrossFade("Die", 0.1f);

		GetComponentInChildren<CapsuleCollider>().enabled = false;
		
		Destroy(GetComponent<NavMeshAgent>());
		Destroy(this);
	}

	private IEnumerator AimAtPlayer() {
		while (true) {
			if (hostileFollow && (PlayerController.instance.transform.position - transform.position).magnitude <= hitRange * 2) {
				transform.rotation = Quaternion.Euler(0f, Quaternion.LookRotation(PlayerController.instance.transform.position - transform.position, Vector3.up).eulerAngles.y, 0f);
			}
			yield return null;
		}
	}

	private IEnumerator TaskScheduler() {
		if (Random.Range(0, 3) == 0) {
			//1/3 to idle
			currentState = State.Idling;
		} else if (Random.Range(0, 2) == 0) {
			//1/3 to dance
			currentState = State.Dancing;
		} else if (Random.Range(0, 2) == 0) {
			//1/6 to walk
			currentState = State.WalkingAround;
		} else {
			currentState = State.Idling; //rest back to idle
		}

		boredOfCurrentTask = false;
		TaskCompleter();
		
		yield return new WaitForSeconds(Random.Range(actionCooldownRange.x, actionCooldownRange.y));
		
		boredOfCurrentTask = true;
		
		yield return null;
	}

	private void TaskCompleter() {
		switch (currentState) {
			case State.Dancing:
			case State.Idling:
				GetComponent<NavMeshAgent>().destination = transform.position;
				UpdateAnimations();
				break;
				
			case State.WalkingAround:
				GameObject room = PlayerController.instance.rooms[Random.Range(0, PlayerController.instance.rooms.Length)];

				Vector3 newDestination = room.transform.position +  room.GetComponent<BoxCollider>().center
					+ new Vector3(Random.Range(-room.GetComponent<BoxCollider>().size.x/2.3f, room.GetComponent<BoxCollider>().size.x/2.3f), 
					              0f,
					              Random.Range(-room.GetComponent<BoxCollider>().size.z/2.3f, room.GetComponent<BoxCollider>().size.z/2.3f));

				GetComponent<NavMeshAgent>().destination = newDestination;
				GetComponent<NavMeshAgent>().speed = 1.4f;
				break;
		}
	}

	#if UNITY_EDITOR


	private void OnDrawGizmosSelected() {
		Gizmos.DrawSphere(GetComponent<NavMeshAgent>().destination, 0.2f);
	}

#endif

	private void SwitchState(State newState) {
		currentState = newState;
	}

	private void UpdateAnimations() {
		switch (currentState) {
				case State.Idling:
					_animator.SetTrigger("idle");
					break;
				case State.Dancing:
					_animator.SetTrigger(danceAnimations[Random.Range(0, danceAnimations.Length)]);
					break;
		}
	}
}
