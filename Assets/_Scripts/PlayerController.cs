using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.PostProcessing;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class PlayerController : MonoBehaviour {
    public static PlayerController instance = null;

    [SerializeField] private float speed = 3f;
    [SerializeField] private float runMultiplier = 1.45f;
    private float rotationAngle = 0;

    private Animator _animator;

    private bool allowMove = true;
    private bool allowHit = true;

    [SerializeField] private SphereCollider[] hitColliders;

    [Range(0f, 1f)] public float drunk = 0f;
    public float hp = 0f;
    public float combo = 0f;

    [SerializeField] private Vector2 chunksDrunkRange = new Vector2(512, 64);
    [SerializeField] private Vector2 vignetteDrunkRange = new Vector2(0.45f, 1f);

    public TextMeshProUGUI interactableText;
    public GameObject interactableObject;

    [SerializeField] private Image hpBar;
    [SerializeField] private Image drunkBar;

    [SerializeField] private float drunkDecay = 1f / 600f; //per second
    [SerializeField] private bool firstShot = true;
    [SerializeField] private GameObject drunkVisual;
    [SerializeField] private TextMeshProUGUI elevationWarning;

    [Header("scoring")] [SerializeField] private int score;
    [SerializeField] private float scoreMult = 1;
    [SerializeField] public int killsLeft = 0;


    [SerializeField] private GameObject popupText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI scoreMultText;

    [Header("end game stuff")] [SerializeField] private GameObject endGameScreen;
    [SerializeField] private TextMeshProUGUI finalScoreText;
    [SerializeField] private TextMeshProUGUI winText;
    [SerializeField] private TextMeshProUGUI lostText;
    [SerializeField] private TextMeshProUGUI objectiveText;

    public GameObject[] rooms;

    private bool invul = false;
    private bool alive = true;

    public List<NPCController> npcs = new List<NPCController>();
    public float aggroRange = 5f;

    public AudioClip kick;
    public AudioClip use;

    public GameObject AudioPopup;
    


    // Use this for initialization
    void Start() {
        if (instance == null) {
            instance = this;
        } else {
            Destroy(gameObject);
        }

        UpdateUIBars(false, false);
        _animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update() {
        if (!alive) {
            GetComponent<Rigidbody>().velocity = Vector3.zero;
            return;
        }

        Vector3 inputVector3 = (Vector3.forward + Vector3.right) * Input.GetAxis("Horizontal") +
                               Input.GetAxis("Vertical") * (-Vector3.right + Vector3.forward);

        if (Input.GetButtonDown("Interact") && allowHit && allowMove && interactableObject != null) {
            bool hpChange = false;
            bool drunkChange = false;

            if (firstShot) {
                objectiveText.text = "Objective: kill them all";
                Sequence tweenSequence = DOTween.Sequence();
                tweenSequence.Append(drunkVisual.GetComponent<RectTransform>().DOAnchorPosY(0, 0.2f))
                    .AppendInterval(3f)
                    .Append(drunkVisual.GetComponent<RectTransform>().DOAnchorPosY(400, 0.2f));
            }
            firstShot = false;
            AddScore(100);

            interactableObject.GetComponent<InteractableObjectHighlightScript>().Use(out hpChange, out drunkChange);

            UpdateUIBars(hpChange, drunkChange);

            _animator.SetTrigger("drugsHit");
            StartCoroutine(UseItem(2f));
        }

        if (Input.GetButtonDown("Punch") && allowHit) {
            if (firstShot) {
                SpawnPopup("<size=70%>No.. I should get a drink instead", transform.position + new Vector3(0, 2f, 0));
            } else {
                Hit();
                GetComponent<Rigidbody>().velocity = Vector3.zero;
                _animator?.SetBool("walking", false);
                _animator?.SetBool("running", false);
            }
        }

        if (allowMove) {
            if (inputVector3.magnitude < 0.2f)
                inputVector3 = Vector3.zero;

            GetComponent<Rigidbody>().velocity = inputVector3 * speed;

            if (inputVector3.magnitude > 0) {
                if (Input.GetButton("Run")) {
                    GetComponent<Rigidbody>().velocity *= runMultiplier;
                    _animator?.SetBool("running", true);
                    _animator?.SetBool("walking", false);
                } else {
                    _animator?.SetBool("walking", true);
                    _animator?.SetBool("running", false);
                }

                rotationAngle = Vector3.Angle(inputVector3, Vector3.forward);
                if (inputVector3.x < 0)
                    rotationAngle = -rotationAngle;
            } else {
                _animator?.SetBool("walking", false);
                _animator?.SetBool("running", false);
            }
        }
        transform.rotation = Quaternion.Euler(0, rotationAngle, 0);

        drunk -= drunkDecay * Time.deltaTime;

        if (drunk < 0) {
            drunk = 0;
        }

        scoreMult = 1 + drunk * 2;
        scoreMultText.text = "score elevation multiplier: " + $"{scoreMult:F1}" + "x";
        if (!firstShot && drunk <= 0) {
            elevationWarning.enabled = true;
            hp -= drunkDecay * Time.deltaTime;
        } else {
            elevationWarning.enabled = false;
        }

        UpdateDrunkVisuals();
        UpdateUIBars(false, false);

        if (hp <= 0) {
            EndGame(false);
        } else if (killsLeft <= 0 && Time.time > 10) {
            //bc you can't win in 10 sec
            EndGame(true);
        }
    }

    private void UpdateDrunkVisuals() {
        Camera.main.GetComponent<PixelationInterface>().chunks =
            (int) Mathf.Lerp(chunksDrunkRange.x, chunksDrunkRange.y, drunk);
        //Camera.main.GetComponent<PostProcessingBehaviour>().profile.vignette = new ChromaticAberrationModel.Settings(); Mathf.Lerp(chunksDrunkRange.x, chunksDrunkRange.y, drunk);
    }

    private void OnTriggerEnter(Collider other) {
        if (other.CompareTag("Room")) {
            Camera.main.GetComponent<CameraController>().MoveCamera(other.gameObject.transform.position);
        }

        if (other.CompareTag("EnemyPunch") && !invul) {
            invul = true;
            Debug.Log("Hit at " + Time.time);

            //Camera.main.DOKill();
            //Camera.main.GetComponent<CameraController>().RestoreCameraPosition();
            Camera.main.DOShakePosition(0.35f, new Vector3(0, 0.7f, 0));


            GetComponentInChildren<SkinnedMeshRenderer>().material.SetColor("_EmissionColor", Color.black);
            GetComponentInChildren<SkinnedMeshRenderer>().material.DOKill();
            GetComponentInChildren<SkinnedMeshRenderer>().material
                .DOColor(new Color(0.5f, 0f, 0f), "_EmissionColor", 0.1f)
                .SetLoops(2, LoopType.Yoyo).OnComplete(() => invul = false);

            hp -= 0.1f;
            UpdateUIBars(true, false);
            SpawnAudioPopup(kick);
        }
    }

    private void Hit() {
        StartCoroutine(HitCoroutine());
    }

    private IEnumerator HitCoroutine() {
        allowMove = false;
        allowHit = false;

        int hitVariation = Random.Range(0, 2);
        _animator.SetInteger("hitNumber", hitVariation);
        _animator.SetTrigger("hit");

        //pre-hit
        yield return new WaitForSeconds(.15f);

        for (int i = 0; i < hitColliders.Length; i++) {
            hitColliders[i].enabled = true;
        }

        Camera.main.DOShakePosition(0.35f, new Vector3(0, 0.5f, 0));
        //hit activated
        yield return new WaitForSeconds(.35f);

        for (int i = 0; i < hitColliders.Length; i++) {
            hitColliders[i].enabled = false;
        }

        //hit de-activated -> backswing
        yield return new WaitForSeconds(.15f);

        allowHit = true;
        allowMove = true;

        yield return null;
    }

    private IEnumerator UseItem(float animationDuration) {
        allowMove = false;
        allowHit = false;
        
        SpawnAudioPopup(use);
        
        Time.timeScale = 0.3f;
        Camera.main.GetComponent<AudioSource>().DOPitch(0.4f, animationDuration / 2).SetLoops(2, LoopType.Yoyo);

        yield return new WaitForSecondsRealtime(animationDuration);

        Time.timeScale = 1f;

        allowHit = true;
        allowMove = true;

        yield return null;
    }

    private void UpdateUIBars(bool hpChange, bool drunkChange) {
        hpBar.fillAmount = hp;
        drunkBar.fillAmount = drunk;

        if (hpChange)
            hpBar.gameObject.transform.DOPunchScale(Vector3.one * 0.2f, 0.1f, 0, 0);
        if (drunkChange)
            drunkBar.gameObject.transform.DOPunchScale(Vector3.one * 0.2f, 0.1f, 0, 0);
    }

    public void AddScore(int add, float popupSpawnYOffset = 2f) {
        score += (int) (add * scoreMult);
        SpawnPopup("+" + add, transform.position + new Vector3(0f, popupSpawnYOffset, 0f));
        
        scoreText.text = "SCORE: " + score;
    }

    public void SpawnPopup(string text, Vector3 position) {
        GameObject popup = Instantiate(popupText, position,
            popupText.transform.rotation);
        popup.GetComponent<TextMeshPro>().text = text;
    }

    public void SpawnAudioPopup(AudioClip clip) {
        GameObject newAP = Instantiate(AudioPopup, transform.position, new Quaternion());
        newAP.GetComponent<AudioSource>().clip = clip;
        newAP.GetComponent<AudioSource>().pitch = Random.Range(0.9f, 1.1f);
        newAP.GetComponent<AudioSource>().Play();
    }

    [ContextMenu("WIN")]
    public void Win() {
        EndGame(true);
    }

    [ContextMenu("LOSE")]
    public void Lose() {
        EndGame(false);
    }
    
    public void EndGame(bool win) {
        alive = false;
        
        endGameScreen.SetActive(true);

        winText.enabled = win;
        lostText.enabled = !win;

        finalScoreText.text = "" + score;
    }

    public void Restart() {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void Exit() {
    Application.Quit();
    }
}