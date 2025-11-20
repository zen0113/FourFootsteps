using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChaserFollower : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private PlayerAutoRunner playerRunner;
    [SerializeField] private Camera cam;
    [SerializeField] private AnimationCurve difficultyOverX; // x→0~1
    [SerializeField] private ObjectSfxController sfxController;
    [SerializeField] private Animator animator;

    public bool isStartChasing = false;

    [Header("Home(왼쪽 위치) 설정")]
    [Tooltip("true면 카메라 왼쪽 가장자리 + margin을 홈X로 사용, false면 homeAnchor 또는 manualHomeX 사용")]
    [SerializeField] private bool useCameraLeftAsHome = true;
    [SerializeField] private float screenMargin = 0.08f; // 뷰포트 X(0~1)
    [SerializeField] private float minLeftBuffer = 0.5f; // 월드 좌표 여유
    [SerializeField] private Transform homeAnchor;        // 수동 기준점(선택)
    [SerializeField] private float manualHomeX = 0f;      // 수동 값(선택)

    [Header("동작 범위/속도")]
    [SerializeField] private float maxAdvance = 8.0f;     // 홈에서 최대 전진 거리(플레이어쪽)
    [SerializeField] private float lurkSpeed = 6.0f;      // 홈 주변 배회/복귀 속도
    [SerializeField] private float returnSpeed = 8.0f;    // 복귀 속도(살짝 더 빠르게)
    [SerializeField] private float surgeSpeed = 12f;      // 돌진 속도
    [SerializeField] private float xDamp = 0.9f;          // 속도 감쇠(부드러움)

    [Header("Surge 조건/완료 판정")]
    [SerializeField] private Vector2 surgeCooldownRange = new Vector2(0.8f, 1.6f); // 다음 돌진까지 대기
    [SerializeField] private Vector2 surgeDurationRange = new Vector2(0.45f, 0.85f); // 돌진 최대 지속시간
    [SerializeField] private float surgeArriveEps = 0.15f; // 목표 지점 도착 판정 허용오차
    [SerializeField] private float approachOffset = 0.5f;  // 플레이어 바로 앞에서 살짝 덜 붙도록

    [Header("난이도 영향")]
    [SerializeField] private float diffAffectsSurgeFreq = 0.5f; // 난이도↑ → 쿨다운 ↓
    [SerializeField] private float diffAffectsAdvance = 2f;     // 난이도↑ → 더 깊게 전진(+)

    [Header("미세 흔들림")]
    [SerializeField] private float noiseAmp = 0.25f;
    [SerializeField] private float noiseFreq = 0.9f;

    [Header("가시성(선택)")]
    [Tooltip("홈이 화면 밖으로 밀릴 때 강제로 화면 좌측 가장자리 안쪽으로 끌어옴")]
    [SerializeField] private bool keepVisibleAtLeft = true;

    [Header("Pavilion Clamp")]
    [Tooltip("Chase_Goal Area에 있는 Key Icon으로 할당")]
    [SerializeField] private Transform pavilionClampPoint;  // 정자 앞 X 기준 오브젝트
    [SerializeField] private float clampArriveSpeed = 5f;   // 정자 앞까지 다가오는 속도
    [SerializeField] private float clampStopEpsilon = 0.02f;// 이 오차 이내면 도착으로 간주

    [Header("Catch(잡으러 가기)")]
    [SerializeField] private float catchSpeed = 3f;
    [SerializeField] private BoxCollider2D col;

    private TutorialController tutorialController;
    public enum Phase { Chasing, Bird }
    public Phase phase;

    private int birdTutoIndex = 11, chasingTutoIndex = 12;

    void CheckPhase()
    {
        if (tutorialController.CurrentIndex == birdTutoIndex || tutorialController.CurrentIndex == chasingTutoIndex)
            phase = Phase.Bird;
        else if (tutorialController.CurrentIndex > chasingTutoIndex)
            phase = Phase.Chasing;
        else
            phase = Phase.Chasing;
    }

    private enum State { Lurk, SurgeForward, ReturnHome }

    [SerializeField] private bool isThrower = false;
    public bool IsThrower => isThrower;
    private ChaserThrower thrower;

    private Rigidbody2D rb;
    private float yLock;
    private float tNoise;
    private float homeX;
    private float curVx;

    // 상태 타이머
    private float surgeTimer;      // 돌진 남은 시간
    private float cooldownTimer;   // 다음 돌진까지 대기

    // 목표
    private float surgeTargetX;    // 이번 돌진의 목표 X (플레이어쪽으로 최대 전진)

    // 게임오버/캐치 흐름
    private bool gameOverTriggered = false;
    public bool IsGameOverTriggered => gameOverTriggered;
    private bool chasingToCatch = false; // 잡으러 가는 상태
    private string gameOverEventID = "EventChaseGameFailed";

    private State state;

    // 이벤트
    public event System.Action OnSurge;       // 돌진 시작(투척 중단)
    public event System.Action OnLurk;        // 대기 전환(투척 재개)
    public event System.Action OnCatchMode;   // 잡으러 가기(정자 도달 but 숨지 않음)
    public void InvokeOnCatchMode()
    {
        OnCatchMode?.Invoke();
    }

    void SwitchToLurk() { state = State.Lurk; OnLurk?.Invoke(); }
    // Lurk 인 동안에는 투척O
    void SwitchToSurgeForward() { state = State.SurgeForward; OnSurge?.Invoke(); }

    void SwitchToReturnHome() { state = State.ReturnHome; }

    public bool chaserCatchPlayer = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        if (!col) col = GetComponent<BoxCollider2D>();
        if (!cam) cam = Camera.main;

        var p = GameObject.FindWithTag("Player");
        if (p)
        {
            if (!playerTransform) playerTransform = p.transform;
            if (!playerRunner) playerRunner = p.GetComponent<PlayerAutoRunner>();
        }

        tutorialController = GameObject.Find("TutorialController").GetComponent<TutorialController>();

        yLock = transform.position.y;                // y 고정
        tNoise = Random.value * 10f;
        isStartChasing = false;

        if (isThrower) thrower = GetComponent<ChaserThrower>();
    }
    private void Start()
    {
        chaserCatchPlayer = false;
    }

    private void OnEnable()
    {
        // 시작은 잠복
        SwitchToLurk();
        cooldownTimer = Random.Range(surgeCooldownRange.x, surgeCooldownRange.y);
        StartCoroutine(StartToChase());
    }

    IEnumerator StartToChase()
    {
        sfxController.StartPlayLoopByDefault();
        animator.SetBool("Moving", true);
        if (this.name== "chasingKids")
        {
            yield return new WaitForSeconds(3f);
        }
        isStartChasing = true;
    }

    public void StopAnimationRunning()
    {
        if (isStartChasing) return;
        animator.SetBool("Moving", false);
        sfxController.StopLoop(0.5f);
        if (isThrower) thrower.StopAll();
    }

    private void FixedUpdate()
    {
        if (!isStartChasing || !playerRunner || !playerTransform) return;

        if (playerRunner.AtHide)
        {
            if (col != null && col.isTrigger) col.isTrigger = false;
        }

        // 1) 숨었으면 정자 앞에서 멈춤
        if (playerRunner.IsHiding)
        {
            StopAtPavilionFront();
            return;
        }

        // 2) 게임오버 연출 중이면 정지
        if (gameOverTriggered)
        {
            rb.velocity = Vector2.zero;
            animator.SetBool("Moving", false);
            return;
        }

        // 3) 정자 도착 but 숨지 않음 → 잡으러 가기 모드
        if (!chasingToCatch && playerRunner.AtHide && !playerRunner.IsHiding)
        {
            EnterCatchMode();
        }

        // 4) 잡으러 가기 상태는 전용 업데이트
        if (chasingToCatch)
        {
            UpdateCatchChase();
            return;
        }

        // --- 일반 패턴 업데이트 ---
        homeX = ComputeHomeX();
        float diff = difficultyOverX?.Evaluate(playerTransform.position.x) ?? 0f;
        UpdateStateMachine(diff);
    }

    // =========================
    // 상태 머신
    // =========================
    private void UpdateStateMachine(float diff)
    {
        CheckPhase();

        switch (state)
        {
            case State.Lurk:
                UpdateLurk(diff);
                break;
            case State.SurgeForward:
                UpdateSurgeForward();
                break;
            case State.ReturnHome:
                UpdateReturnHome();
                break;
        }

        // 화면 좌측 가시성 보장
        if (keepVisibleAtLeft && cam && cam.orthographic)
        {
            float minX = CameraLeftEdgeX() + minLeftBuffer;
            if (transform.position.x < minX)
            {
                transform.position = new Vector3(minX, yLock, transform.position.z);
                curVx = Mathf.Max(curVx, 0f);
            }
        }
    }

    private void UpdateLurk(float diff)
    {
        // 홈X 주변에서만 소폭 배회 (플레이어 쪽으로는 안 나감)
        tNoise += Time.fixedDeltaTime * noiseFreq;
        float n = (Mathf.PerlinNoise(tNoise, 0f) - 0.5f) * 2f; // -1~1

        float targetX = homeX + n * noiseAmp;
        MoveTowardsX(targetX, lurkSpeed);

        // 다음 돌진까지 쿨다운 감소 (난이도 높을수록 빨리 줄어듦)
        float cdMul = Mathf.Lerp(1f, 0.6f, diff * diffAffectsSurgeFreq);
        cooldownTimer -= Time.fixedDeltaTime * cdMul;

        if (cooldownTimer <= 0f)
        {
            // 이번 돌진 목표 계산: 플레이어 앞쪽으로, 단 홈에서 최대 전진 거리 제한
            float maxAdvanceThis = homeX + maxAdvance + diff * diffAffectsAdvance;
            float playerFrontX = playerTransform.position.x - approachOffset;
            surgeTargetX = Mathf.Min(playerFrontX, maxAdvanceThis);

            SwitchToSurgeForward();
            surgeTimer = Random.Range(surgeDurationRange.x, surgeDurationRange.y);
        }
    }

    private void UpdateSurgeForward()
    {
        // 돌진 목표까지 빠르게 접근
        MoveTowardsX(surgeTargetX, surgeSpeed);

        // 돌진 완료 조건: 목표 근접 or 시간 초과
        surgeTimer -= Time.fixedDeltaTime;
        if (Mathf.Abs(transform.position.x - surgeTargetX) <= surgeArriveEps || surgeTimer <= 0f)
        {
            SwitchToReturnHome();
        }
    }

    private void UpdateReturnHome()
    {
        // 홈으로 빠르게 복귀
        MoveTowardsX(homeX, returnSpeed);

        // 홈 도착하면 다시 잠복 + 다음 돌진까지 쿨다운 리셋
        if (Mathf.Abs(transform.position.x - homeX) <= surgeArriveEps)
        {
            SwitchToLurk();
            cooldownTimer = Random.Range(surgeCooldownRange.x, surgeCooldownRange.y);
        }
    }

    // x만 부드럽게 이동
    private void MoveTowardsX(float targetX, float speed)
    {
        float dir = Mathf.Sign(targetX - transform.position.x);
        float desiredVx = dir * speed;
        curVx = Mathf.MoveTowards(curVx, desiredVx, speed * Time.fixedDeltaTime);
        curVx *= xDamp;

        float newX = transform.position.x + curVx * Time.fixedDeltaTime;
        rb.MovePosition(new Vector2(newX, yLock));
    }

    // =========================
    // Catch(잡으러 가기) 흐름
    // =========================
    private void EnterCatchMode()
    {
        OnCatchMode?.Invoke();
        if (isThrower) thrower.enabled = false;
        chasingToCatch = true;

        //if (col != null) col.isTrigger = false; // 실제 충돌로 전환
        if (col != null) col.isTrigger = true;
        Vector2 dir = (playerRunner.transform.position - transform.position).normalized;
        rb.velocity = dir * catchSpeed;
    }

    private void UpdateCatchChase()
    {
        OnCatchMode?.Invoke();

        Vector2 dir = (playerRunner.transform.position - transform.position).normalized;
        rb.velocity = dir * catchSpeed;
        // 실제 GameOver는 충돌에서 1회 실행
    }

    private void ChaseGame_GameOver()
    {
        Debug.Log("추격 게임 : 추격자한테 잡힘[게임오버]");
        chaserCatchPlayer = true;

        if (isThrower)
        {
            sfxController.StopLoop(1f);
            StealthSFX.Instance.StopEnterSFX();
            CatStealthController.Instance.Chase_GameOver();
            EventManager.Instance.CallEvent(gameOverEventID);
        }

        this.enabled = false;
    }

    // =========================
    // 유틸
    // =========================
    private float ComputeHomeX()
    {
        if (useCameraLeftAsHome && cam && cam.orthographic)
            return CameraLeftEdgeX() + minLeftBuffer;
        if (homeAnchor) return homeAnchor.position.x;
        return manualHomeX;
    }

    private float CameraLeftEdgeX()
    {
        Vector3 p = cam.ViewportToWorldPoint(new Vector3(screenMargin, 0.5f, Mathf.Abs(cam.transform.position.z - transform.position.z)));
        return p.x;
    }

    private void StopAtPavilionFront()
    {
        if (!pavilionClampPoint) { rb.velocity = Vector2.zero; return; }

        float targetX = pavilionClampPoint.position.x;
        float newX = Mathf.MoveTowards(transform.position.x, targetX, clampArriveSpeed * Time.fixedDeltaTime);

        rb.MovePosition(new Vector2(newX, yLock));
        if (Mathf.Abs(newX - targetX) <= clampStopEpsilon)
        {
            rb.velocity = Vector2.zero;
            col.isTrigger = false;
            animator.SetBool("Moving", false);
            this.enabled = false;
        }
        OnCatchMode?.Invoke();
        sfxController.StopLoop(1f);
    }

    private void OnDrawGizmosSelected()
    {
        if (!cam) cam = Camera.main;
        float hx = Application.isPlaying ? homeX : (useCameraLeftAsHome && cam && cam.orthographic ? CameraLeftEdgeX() + minLeftBuffer : (homeAnchor ? homeAnchor.position.x : manualHomeX));

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(hx, (Application.isPlaying ? yLock : transform.position.y) - 0.5f, 0),
                        new Vector3(hx, (Application.isPlaying ? yLock : transform.position.y) + 0.5f, 0));

        Gizmos.color = Color.red;
        float ax = hx + maxAdvance;
        Gizmos.DrawLine(new Vector3(ax, (Application.isPlaying ? yLock : transform.position.y) - 0.5f, 0),
                        new Vector3(ax, (Application.isPlaying ? yLock : transform.position.y) + 0.5f, 0));
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!gameOverTriggered && chasingToCatch && collision.collider.CompareTag("Player"))
        {
            gameOverTriggered = true;
            rb.velocity = Vector2.zero;
            animator.SetBool("Moving", false);
            ChaseGame_GameOver();
        }
    }
}
