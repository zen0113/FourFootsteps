using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class KidWatcher : MonoBehaviour
{
    public static KidWatcher Instance { get; private set; }

    public bool isPlaying = false;

    [Header("Refs")]
    [SerializeField] private StealthSettingsSO settings;
    private StealthSettingsSO _settings;                 // 런타임 복제본

    public Transform player;
    public RectTransform kidsRectTransform;
    public Image kidsImage;
    public Sprite normalSprite;
    public Sprite watchingSprite;
    public SuspicionMeter meter;
    [SerializeField] private CameraShake cameraShake;
    [SerializeField] private GameObject StageBlockingWall;
    [SerializeField] private ObjectSfxController sfxController;
    [SerializeField] private Canvas StealthCanvas;
    [SerializeField] private Camera mainCamera;

    public float smoothTime = 0.15f;
    private float _velX;

    public bool isWatching { get; private set; }

    [SerializeField] private Transform goal_Transform;
    private bool isRandomWatchLooping = true;

    private string StealthEndDialogueID = "Stage03_004";

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        _settings = Instantiate(settings); // 공유 SO 상태오염 방지
        _settings.ResetRuntime();

        isPlaying = false;

        cameraShake = Camera.main.GetComponent<CameraShake>();
        cameraShake.enabled = false;

        kidsRectTransform = GameObject.Find("Kids_Image").GetComponent<RectTransform>();
        kidsImage = GameObject.Find("Kids_Image").GetComponent<Image>();
        goal_Transform = GameObject.Find("Bench chair").GetComponent<Transform>(); 

        player = GameObject.FindWithTag("Player").GetComponent<Transform>();

        float maxDistance =Vector2.Distance(goal_Transform.position, player.position);
        _settings.maxDistance = maxDistance;
        Debug.Log($"은신 maxDistance: {maxDistance}");

        if (StealthCanvas==null) StealthCanvas=GetComponent<Canvas>();
        StealthCanvas.renderMode = UnityEngine.RenderMode.WorldSpace;

        if (mainCamera == null) mainCamera = Camera.main;
    }

    public void StartStealthGame()
    {
        sfxController.StartPlayLoopByDefault();
        PlayerCatMovement.Instance.IsJumpingBlocked = true;
        isPlaying = true;
        StartCoroutine(RandomWatchLoop());
        if (meter) meter.OnReachedMax += HandleCaught;
        meter.ResetMeter();

        Vector3 canvasPos = StealthCanvas.transform.position;
        canvasPos.x = player.position.x;
        StealthCanvas.transform.position = canvasPos;
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
        float d, t;
        if (kidsRectTransform)
        {
            d = Vector2.Distance(goal_Transform.position, player.position);
            t = Mathf.InverseLerp(_settings.maxDistance, _settings.minDistance, d); // 가까울수록 1
            float target = Mathf.Lerp(_settings.scaleAtMax, _settings.scaleAtMin, t);
            float next = Mathf.Lerp(kidsRectTransform.localScale.x, target, Time.deltaTime * _settings.scaleSmooth);
            kidsRectTransform.localScale = new Vector3(next, next, 1f);
        }

        // 2) 의심 게이지 적용
        if (meter)
        {
            float perSec = EvaluateSuspicionPerSecond();
            meter.ApplyDelta(perSec * Time.deltaTime);
        }

    }

    private float _autoDMax = -1f; // 첫 프레임의 d를 저장
    [SerializeField] private float nearDistance = 3f; // "가까움"으로 보는 기준 (씬 스케일에 맞게)

    private void LateUpdate()
    {
        if (!StealthCanvas || !isPlaying || !player) return;
        var cam = mainCamera;

        float halfW = cam.orthographicSize * cam.aspect;
        float leftLX = -halfW;
        float rightLX = halfW;

        float edgeRatio = 0.6f; // 0.0=센터, 1.0=완전 끝
        float innerLeft = Mathf.Lerp(0f, leftLX, edgeRatio);  // leftLX는 음수 → 0쪽으로 당겨짐
        float innerRight = Mathf.Lerp(0f, rightLX, edgeRatio);  // rightLX는 양수 → 0쪽으로 당겨짐

        // 거리
        float d = Vector2.Distance(goal_Transform.position, player.position);

        // 오토 dMax (첫 프레임 캡쳐)
        if (_autoDMax < 0f) _autoDMax = Mathf.Max(d, nearDistance + 0.01f);

        float dMin = nearDistance;
        float dMax = _autoDMax;

        // 멀리=0, 가까이=1
        float t = 1f - Mathf.InverseLerp(dMin, dMax, d);
        t = Mathf.Clamp01(t);

        // 좌/우 반전(미러링) 보정
        if (StealthCanvas.transform.lossyScale.x < 0f)
            (leftLX, rightLX) = (rightLX, leftLX);

        // 멀리(0)=오른쪽, 가까이(1)=왼쪽 → 즉시 스냅
        float targetLocalX = Mathf.Lerp(innerRight, innerLeft, t);

        var lp = StealthCanvas.transform.localPosition;
        lp.x = targetLocalX;
        StealthCanvas.transform.localPosition = lp;

        //Debug.Log($"d:{d:F2} t:{t:F2} dMin:{dMin:F2} dMax:{dMax:F2} left:{leftLX:F2} right:{rightLX:F2} lp.x:{lp.x:F2}");
    }
    


    IEnumerator RandomWatchLoop()
    {
        while (isRandomWatchLooping)
        {
            // Idle
            yield return new WaitForSeconds(Random.Range(_settings.idleInterval.x, _settings.idleInterval.y));
            // Watch
            float dur = Random.Range(_settings.watchDuration.x, _settings.watchDuration.y);
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

        if (inGrace) return -_settings.decayPerSecond; // 면책: 무조건 감소

        if (isWatching && !playerHidden)
        {
            // 가까울수록 더 많이 오른다
            float d = Vector2.Distance(goal_Transform.position, player.position);
            float norm = Mathf.InverseLerp(_settings.maxDistance, _settings.minDistance, d); // 0~1 (가까울수록 1)
            float weight = _settings.proximityFactor.Evaluate(norm);
            return Mathf.Max(0f, _settings.gainPerSecond * weight);
        }
        return -_settings.decayPerSecond;
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

        // 카메라 Shake 효과 전에 카메라의 자식 오브젝트로 Canvas 넣어둔 거 풀기
        // 로컬 좌표를 월드 좌표로 변환
        Vector3 worldPos = transform.TransformPoint(this.transform.localPosition);

        this.transform.SetParent(null);
        this.transform.position = worldPos;

        // 카메라 shake에 아이들이 저기 고양이다!! 다이얼로그[Stage03_004] 재생
        cameraShake.enabled = true;
        cameraShake.ShakeAndDisable(0.5f, 0.25f);
        isPlaying = false;
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
