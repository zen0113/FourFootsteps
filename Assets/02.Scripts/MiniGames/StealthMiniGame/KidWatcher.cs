using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class KidWatcher : MonoBehaviour
{
    public static KidWatcher Instance { get; private set; }

    public bool isPlaying = false;

    [Header("Refs")]
    [SerializeField] private StealthSettingsSO settings;
    public Transform player;
    public RectTransform kidsRectTransform;
    public Image kidsImage;
    public Sprite normalSprite;
    public Sprite watchingSprite;
    public SuspicionMeter meter;
    [SerializeField] private CameraShake cameraShake;
    [SerializeField] private GameObject StageBlockingWall;
    [SerializeField] private ObjectSfxController sfxController;

    public bool isWatching { get; private set; }

    [SerializeField] private Transform goal_Transform;
    private bool isRandomWatchLooping = true;

    private string StealthEndDialogueID = "Stage03_004";

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        isPlaying = false;

        cameraShake = Camera.main.GetComponent<CameraShake>();
        cameraShake.enabled = false;

        kidsRectTransform = GameObject.Find("Kids_Image").GetComponent<RectTransform>();
        kidsImage = GameObject.Find("Kids_Image").GetComponent<Image>();
        goal_Transform = GameObject.Find("Bench chair").GetComponent<Transform>(); 

        player = GameObject.FindWithTag("Player").GetComponent<Transform>();

        float maxDistance =Vector2.Distance(goal_Transform.position, player.position);
        settings.maxDistance = maxDistance;
        Debug.Log(maxDistance);
    }

    private void Start()
    {
        gameObject.SetActive(false);
    }

    public void StartStealthGame()
    {
        sfxController.StartPlayLoopByDefault();
        PlayerCatMovement.Instance.IsJumpingBlocked = true;
        isPlaying = true;
        StartCoroutine(RandomWatchLoop());
        if (meter) meter.OnReachedMax += HandleCaught;
        meter.ResetMeter();
    }

    //void OnDisable()
    //{
    //    StopAllCoroutines();
    //    if (meter) meter.OnReachedMax -= HandleCaught;
    //    SetWatching(false);
    //}

    void Update()
    {
        if (!player|| !isPlaying) return;

        // 1) 거리 기반 kids 이미지 스케일
        if (kidsRectTransform)
        {
            float d = Vector2.Distance(goal_Transform.position, player.position);
            float t = Mathf.InverseLerp(settings.maxDistance, settings.minDistance, d); // 가까울수록 1
            float target = Mathf.Lerp(settings.scaleAtMax, settings.scaleAtMin, t);
            float next = Mathf.Lerp(kidsRectTransform.localScale.x, target, Time.deltaTime * settings.scaleSmooth);
            kidsRectTransform.localScale = new Vector3(next, next, 1f);
        }

        // 2) 의심 게이지 적용
        if (meter)
        {
            float perSec = EvaluateSuspicionPerSecond();
            meter.ApplyDelta(perSec * Time.deltaTime);
        }
    }

    IEnumerator RandomWatchLoop()
    {
        while (isRandomWatchLooping)
        {
            // Idle
            yield return new WaitForSeconds(Random.Range(settings.idleInterval.x, settings.idleInterval.y));
            // Watch
            float dur = Random.Range(settings.watchDuration.x, settings.watchDuration.y);
            SetWatching(true);
            yield return new WaitForSeconds(dur);
            SetWatching(false);
        }
    }

    public void FinalWatchLoop()
    {
        isRandomWatchLooping = false;
        SetWatching(true);
    }


    void SetWatching(bool watching)
    {
        isWatching = watching;
        if (kidsImage) kidsImage.sprite = watching ? watchingSprite : normalSprite;

        if (watching)
            sfxController.Pause(1f);
        else
            sfxController.Resume(1f);
    }

    float EvaluateSuspicionPerSecond()
    {
        var stealth = CatStealthController.Instance;
        bool playerHidden = stealth && stealth.IsHiding;
        bool inGrace = stealth && stealth.IsInGrace;

        if (inGrace) return -settings.decayPerSecond; // 면책: 무조건 감소

        if (isWatching && !playerHidden)
        {
            // 가까울수록 더 많이 오른다
            float d = Vector2.Distance(goal_Transform.position, player.position);
            float norm = Mathf.InverseLerp(settings.maxDistance, settings.minDistance, d); // 0~1 (가까울수록 1)
            float weight = settings.proximityFactor.Evaluate(norm);
            return Mathf.Max(0f, settings.gainPerSecond * weight);
        }
        return -settings.decayPerSecond;
    }

    void HandleCaught()
    {
        Debug.Log("[Stealth] Caught by Kid!");

        StageBlockingWall.SetActive(false);
        PlayerCatMovement.Instance.SetMiniGameInputBlocked(true);
        StopAllCoroutines();
        SetWatching(true);
        // 발각 연출
        // 은신 비네팅 효과 사라지게
        StealthSFX.Instance.CaughtByKidFX();
        // 카메라 shake에 아이들이 저기 고양이다!! 다이얼로그[Stage03_004] 재생
        cameraShake.enabled = true;
        cameraShake.ShakeAndDisable(0.5f, 0.25f);
        DialogueManager.Instance.StartDialogue(StealthEndDialogueID);
        //cameraShake.enabled = false;
        // 이벤트 훅 해제
        if (meter) meter.OnReachedMax -= HandleCaught;

        // 다이얼로그 진행 후
        // 카메라 Target 해제 후 플레이어 오른쪽으로 강제 대쉬 이동시켜서
        // 시야에서 벗어나고 다음 추격 미니게임 시작
        // Result_StartChaseGameIntro 에서 진행됨
        FollowCamera followCamera = Camera.main.GetComponent<FollowCamera>();
        followCamera.enabled = false;
    }

}
