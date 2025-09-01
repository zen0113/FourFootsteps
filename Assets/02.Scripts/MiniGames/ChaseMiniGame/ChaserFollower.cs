using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChaserFollower : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private PlayerAutoRunner playerRunner;
    [SerializeField] private Transform muzzle;          // 투척 시작점
    [SerializeField] private Camera cam;
    [SerializeField] private AnimationCurve difficultyOverX; // x→0~1

    [Header("Home(왼쪽 위치) 설정")]
    [Tooltip("true면 카메라 왼쪽 가장자리 + margin을 홈X로 사용, false면 homeAnchor 또는 manualHomeX 사용")]
    [SerializeField] private bool useCameraLeftAsHome = true;
    [SerializeField] private float screenMargin = 0.08f; // 뷰포트 X(0~1)
    [SerializeField] private float minLeftBuffer = 0.5f; // 월드 좌표 여유
    [SerializeField] private Transform homeAnchor;        // 수동 기준점(선택)
    [SerializeField] private float manualHomeX = 0f;      // 수동 값(선택)

    [Header("동작 범위/속도")]
    [SerializeField] private float maxAdvance = 7.0f;     // 홈에서 최대 전진 거리(플레이어쪽)
    [SerializeField] private float lurkSpeed = 6.0f;      // 홈 주변 복귀/배회 속도
    [SerializeField] private float surgeSpeed = 10f;     // 돌진 속도
    [SerializeField] private float xDamp = 0.9f;          // 속도 감쇠(부드러움)
    [SerializeField] private float catchSpeed = 1.5f;

    [Header("상태 전이(랜덤)")]
    [SerializeField] private Vector2 lurkDurationRange = new Vector2(0.8f, 1.6f); // 대기 시간
    [SerializeField] private Vector2 surgeDurationRange = new Vector2(0.45f, 0.85f); // 돌진 시간
    [SerializeField] private float diffAffectsSurgeFreq = 0.5f; // 난이도가 높을수록 돌진 더 잦게(+)
    [SerializeField] private float diffAffectsAdvance = 2f;   // 난이도가 높을수록 더 깊게 진입(+)

    [Header("미세 흔들림")]
    [SerializeField] private float noiseAmp = 0.25f;      // 목표 주위 흔들림
    [SerializeField] private float noiseFreq = 0.9f;

    [Header("가시성(선택)")]
    [Tooltip("홈이 화면 밖으로 밀릴 때 강제로 화면 좌측 가장자리 안쪽으로 끌어옴")]
    [SerializeField] private bool keepVisibleAtLeft = true;

    [Header("Pavilion Clamp")]
    [SerializeField] private Transform pavilionClampPoint;  // 정자 앞 X 기준 오브젝트
    [SerializeField] private float clampArriveSpeed = 5f;   // 정자 앞까지 다가오는 속도
    [SerializeField] private float clampStopEpsilon = 0.02f;// 이 오차 이내면 도착으로 간주

    private Rigidbody2D rb;
    [SerializeField] private BoxCollider2D col;
    private float yLock;
    private float tNoise;
    private float stateTimer;
    private float homeX;            // 현재 프레임 기준 홈 X
    private float curVx;            // 내부 속도(부드럽게 이동)

    private bool gameOverTriggered = false;
    private bool chasingToCatch = false; // 잡으러 가는 상태
    private string gameOverEventID = "EventChaseGameFailed";

    private State state;

    private enum State { Lurk, Surge }

    public Transform Muzzle => muzzle ? muzzle : transform;


    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<BoxCollider2D>();
        if (!cam) cam = Camera.main;
        var p = GameObject.FindWithTag("Player");
        if (p)
        {
            if (!playerTransform) playerTransform = p.transform;
            if (!playerRunner) playerRunner = p.GetComponent<PlayerAutoRunner>();
        }

        // 시작 y 고정
        yLock = transform.position.y;
        tNoise = Random.value * 10f;
    }
    private void OnEnable()
    {
        SwitchTo(State.Lurk);
    }

    private void FixedUpdate()
    {
        // 1) 플레이어가 정자에 도착해서 숨었다면: 정자 앞에서 멈추고 더 이상 따라가지 않음
        if (playerRunner.IsHiding)
        {
            StopAtPavilionFront();      // rb.velocity = Vector2.zero; 등
            return;                     // 더 이상 처리하지 않음
        }

        // 2) 이미 게임오버 연출 중이라면: 아무 것도 하지 않음
        if (gameOverTriggered)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        // 3) 잡힘 조건 진입: 정자 도착했지만 숨지 않은 경우 → '잡으러 가기' 모드로 1회 전환
        if (!chasingToCatch && playerRunner.AtHide && !playerRunner.IsHiding)
        {
            EnterCatchMode();           // chasingToCatch=true, col.isTrigger=false, 초기 속도 등
                                        // 이후 바로 아래에서 '잡으러 가기' 업데이트가 실행됨
        }

        // 4) '잡으러 가기' 상태면: 플레이어에게 직진만 수행 (일반 추격 로직은 중지)
        if (chasingToCatch)
        {
            UpdateCatchChase();         // rb.velocity = (toPlayer.normalized * catchSpeed);
            return;
        }


        UpdateNormalFollow();
    }

    private void StopAtPavilionFront()
    {
        float targetX = pavilionClampPoint.position.x;
        float newX = Mathf.MoveTowards(transform.position.x, targetX, clampArriveSpeed * Time.fixedDeltaTime);

        // Rigidbody2D가 있다면 MovePosition으로, 없으면 transform.position으로 적용
        if (rb != null)
            rb.MovePosition(new Vector2(newX, yLock));
        else
            transform.position = new Vector3(newX, yLock, transform.position.z);

        // 거의 도착했으면 속도 0으로 정지(진동 방지)
        if (Mathf.Abs(newX - targetX) <= clampStopEpsilon && rb != null)
            rb.velocity = Vector2.zero;
    }

    private void EnterCatchMode()
    {
        chasingToCatch = true;

        // 연속 HP 깎임 방지를 위해 실제 충돌로 전환
        if (col != null) col.isTrigger = false;

        // 초기 속도(선택)
        Vector2 dir = (playerRunner.transform.position - transform.position).normalized;
        rb.velocity = dir * catchSpeed;
    }

    private void UpdateCatchChase()
    {
        Vector2 dir = (playerRunner.transform.position - transform.position).normalized;
        rb.velocity = dir * catchSpeed;
        // 실제 GameOver 호출은 OnCollisionEnter2D에서 Player와 닿았을 때 1회 실행
    }

    // 게임 오버 처리
    private void ChaseGame_GameOver()
    {
        Debug.Log("추격 게임 : 추격자한테 잡힘[게임오버]");
        // 추격 게임 실패 다이얼로그 재생 및 게임오버 씬 로드
        EventManager.Instance.CallEvent(gameOverEventID);
    }

    private void UpdateNormalFollow()
    {
        // 난이도(0~1)
        float diff = difficultyOverX?.Evaluate(playerTransform.position.x) ?? 0f;

        // 홈 X 계산
        homeX = ComputeHomeX();

        // 상태 별 목표 X 계산
        float targetX = state == State.Lurk
            ? GetLurkTargetX()
            : GetSurgeTargetX(diff);

        // 부드러운 속도 기반 이동(x만)
        float targetSpeed = (state == State.Lurk ? lurkSpeed : surgeSpeed);
        float dir = Mathf.Sign(targetX - transform.position.x);
        float desiredVx = dir * targetSpeed;

        // 가속/감속: current -> desired 로 보정
        curVx = Mathf.MoveTowards(curVx, desiredVx, targetSpeed * Time.fixedDeltaTime);
        curVx *= xDamp; // 약간 감쇠로 부드럽게

        // 실제 이동: x만
        float newX = transform.position.x + curVx * Time.fixedDeltaTime;
        rb.MovePosition(new Vector2(newX, yLock)); // y 고정

        // 상태 타이머/전이
        stateTimer -= Time.fixedDeltaTime;
        if (stateTimer <= 0f)
        {
            if (state == State.Lurk)
            {
                SwitchTo(State.Surge, CalcSurgeDuration(diff));
            }
            else
            {
                SwitchTo(State.Lurk, CalcLurkDuration(diff));
            }
        }

        // 화면 좌측 가시성 보장
        if (keepVisibleAtLeft && cam && cam.orthographic)
        {
            float minX = CameraLeftEdgeX() + minLeftBuffer;
            if (transform.position.x < minX)
            {
                transform.position = new Vector3(minX, yLock, transform.position.z);
                curVx = Mathf.Max(curVx, 0f); // 왼쪽으로 끌려들지 않도록
            }
        }

    }

    // --- 상태 전이 ---
    private void SwitchTo(State next, float duration = -1f)
    {
        state = next;
        if (duration < 0f)
        {
            stateTimer = (next == State.Lurk) ? CalcLurkDuration(0f) : CalcSurgeDuration(0f);
        }
        else stateTimer = duration;

        // 돌진 시작 시 플레이어 쪽으로 바라보도록 (스프라이트가 있다면)
        // flip은 너의 애니 파이프에 맞게 조절
        // 여기서는 고정 y라 굳이 회전은 안 건드림
    }

    // --- Target(player) X 계산 ---
    private float GetLurkTargetX()
    {
        // 홈 주변에서만 머뭇거림 + 미세 노이즈
        tNoise += Time.fixedDeltaTime * noiseFreq;
        float n = (Mathf.PerlinNoise(tNoise, 0f) - 0.5f) * 2f; // -1~1
        return homeX + n * noiseAmp;
    }

    private float GetSurgeTargetX(float diff)
    {
        // 플레이어 쪽으로 '최대 전진 거리'까지만 접근
        float desired = Mathf.Min(playerTransform.position.x - 0.3f, homeX + maxAdvance + diff * diffAffectsAdvance);
        // 약간의 흔들림 추가
        float n = (Mathf.PerlinNoise(tNoise, 0f) - 0.5f) * 2f;
        return desired + n * (noiseAmp * 0.5f);
    }

    // --- 지속시간 ---
    private float CalcLurkDuration(float diff)
    {
        // 난이도 높을수록 대기시간 조금 짧게
        float t = Random.Range(lurkDurationRange.x, lurkDurationRange.y);
        t *= Mathf.Lerp(1f, 0.8f, diff * diffAffectsSurgeFreq);
        return Mathf.Max(0.1f, t);
    }

    private float CalcSurgeDuration(float diff)
    {
        // 난이도 높을수록 돌진 조금 더 길게
        float t = Random.Range(surgeDurationRange.x, surgeDurationRange.y);
        t *= Mathf.Lerp(1f, 1.2f, diff * diffAffectsSurgeFreq);
        return Mathf.Max(0.1f, t);
    }

    // --- 홈 X 계산 ---
    private float ComputeHomeX()
    {
        if (useCameraLeftAsHome && cam && cam.orthographic)
        {
            return CameraLeftEdgeX() + minLeftBuffer;
        }
        if (homeAnchor) return homeAnchor.position.x;
        return manualHomeX;
    }

    private float CameraLeftEdgeX()
    {
        // 뷰포트상 left+margin의 월드 X
        Vector3 p = cam.ViewportToWorldPoint(new Vector3(screenMargin, 0.5f, Mathf.Abs(cam.transform.position.z - transform.position.z)));
        return p.x;
    }

    private void OnDrawGizmosSelected()
    {
        // 홈/진입 한계 시각화
        if (!cam) cam = Camera.main;

        float hx = Application.isPlaying ? homeX : PreviewHomeXInEditor();
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(hx, (Application.isPlaying ? yLock : transform.position.y) - 0.5f, 0),
                        new Vector3(hx, (Application.isPlaying ? yLock : transform.position.y) + 0.5f, 0));

        Gizmos.color = Color.red;
        float ax = hx + maxAdvance;
        Gizmos.DrawLine(new Vector3(ax, (Application.isPlaying ? yLock : transform.position.y) - 0.5f, 0),
                        new Vector3(ax, (Application.isPlaying ? yLock : transform.position.y) + 0.5f, 0));
    }

    private float PreviewHomeXInEditor()
    {
        if (useCameraLeftAsHome && cam && cam.orthographic)
            return CameraLeftEdgeX() + minLeftBuffer;
        if (homeAnchor) return homeAnchor.position.x;
        return manualHomeX;
    }

    // 충돌 감지
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!gameOverTriggered && chasingToCatch && collision.collider.CompareTag("Player"))
        {
            gameOverTriggered = true;
            rb.velocity = Vector2.zero;
            ChaseGame_GameOver(); // 실제 게임오버 처리(연출, 씬 전환 등)
        }
    }
}
