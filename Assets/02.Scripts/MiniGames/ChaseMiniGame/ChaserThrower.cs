using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChaserThrower : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform chaserMuzzle; // 던지는 시작점
    [SerializeField] private Transform player;
    [SerializeField] private Rigidbody2D playerRb;   // 플레이어 속도 예측용
    [SerializeField] private ObstaclePool pool;
    [SerializeField] private ChaserFollower chaser;

    [Header("Timing")]
    [SerializeField] private Vector2 throwIntervalRange = new Vector2(3.5f, 6.5f);
    [SerializeField] private AnimationCurve difficultyOverX; // x좌표→난이도(0~1)
    [SerializeField] private float minFlightTime = 0.6f;
    [SerializeField] private float maxFlightTime = 1.4f;

    [Header("Aiming")]
    [SerializeField] private float leadFactor = 0.55f; // 플레이어 속도 반영 비율
    [SerializeField] private float spreadRadius = 0.6f; // 랜덤 분산
    [SerializeField] private float maxInitSpeed = 18f; // 너무 급한 투척 제한
    [SerializeField] private LayerMask groundMask;     // 사전 충돌 레이캐스트(선택)

    [Header("Sound")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip ThrowSound;

    [Header("Animation")]
    public Animator animator;
    private static readonly int ThrowOnceTrigger = Animator.StringToHash("ThrowOnce");
    private static readonly int SwingShortHash = Animator.StringToHash("Kids_Swing"); // 상태 이름의 shortNameHash
    private static readonly int RunShortHash = Animator.StringToHash("Kids_Run");   // 시작 안전용
    private const int BaseLayer = 0;
    // 스윙 중복 방지용
    private bool isSwinging;
    private Coroutine swingWatchRoutine;

    [Header("Animation Timing")]
    [SerializeField] private Vector2 throwAfterSwingDelayRange = new Vector2(0.15f, 0.17f);
    private Coroutine throwAfterRoutine;

    private bool running;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        var p = GameObject.FindWithTag("Player");
        if (p)
        {
            if (!player) player = p.transform;
            if (!playerRb) playerRb = p.GetComponent<Rigidbody2D>();
        }
        var c = GetComponent<ChaserFollower>();
        if (c && c.IsThrower)
            chaser = c;

        chaser.OnSurge += StopAll;
        chaser.OnLurk += Resume;
        chaser.OnCatchMode += StopAll;
        chaser.OnCatchMode += pool.DisableAllObstacle;
    }

    public void DisconnectEvent()
    {
        chaser.OnSurge -= StopAll;
        chaser.OnLurk -= Resume;
        chaser.OnCatchMode -= StopAll;
        chaser.OnCatchMode -= pool.DisableAllObstacle;
    }

    private void OnEnable()
    {
        // 이벤트 신호를 놓쳤더라도, 켜질 때 스스로 루프 시작
        if (animator) animator.Play(RunShortHash, BaseLayer, 0f);

        if (!running) Resume();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        running = false;

        if (chaser != null)
        {
            chaser.OnSurge -= StopAll;
            chaser.OnLurk -= Resume;
            chaser.OnCatchMode -= StopAll;
            chaser.OnCatchMode -= pool.DisableAllObstacle;
        }
        isSwinging = false;
        swingWatchRoutine = null;
    }


    public void Begin()
    {
        running = true;
        StopAllCoroutines();
        StartCoroutine(Loop());
    }

    public void StopAll() { running = false; StopAllCoroutines(); }
    public void Pause() { running = false; }
    public void Resume() { if (!running) { running = true; StartCoroutine(Loop()); } }

    private IEnumerator Loop()
    {
        while (running&& !chaser.chaserCatchPlayer)
        {
            // 새 페이즈에서는 던지지 않음
            if (chaser.phase == ChaserFollower.Phase.Bird|| !pool.IsPlaying)
            {
                yield return null; continue;
            }

            // 난이도에 따라 간격 축소
            float diff = 0f;
            if (difficultyOverX != null)
                diff = difficultyOverX.Evaluate(player.position.x);
            float ivMin = Mathf.Lerp(throwIntervalRange.x * 1.2f, throwIntervalRange.x, diff);
            float ivMax = Mathf.Lerp(throwIntervalRange.y * 1.2f, throwIntervalRange.y, diff);
            float wait = Random.Range(ivMin, ivMax);

            yield return new WaitForSeconds(wait);
            BeginThrowSequence();
        }
    }

    // 애니메이션을 먼저 트리거하고, 스윙 진입을 감지한 뒤 지연 후 던진다
    private void BeginThrowSequence()
    {
        if (!pool || !pool.IsPlaying || !player) return;

        // 스윙 트리거 (스윙 중이면 중복 무시)
        TriggerSwingOnce();

        // 기존 지연 루틴이 돌고 있으면 취소 후 다시 시작 (최신 입력 우선)
        if (throwAfterRoutine != null) StopCoroutine(throwAfterRoutine);
        throwAfterRoutine = StartCoroutine(ThrowAfterSwingRoutine());
    }

    private IEnumerator ThrowAfterSwingRoutine()
    {
        // 1) 스윙 상태 진입 대기 (최대 2초 가드)
        int guard = 0;
        yield return null;
        var st = animator.GetCurrentAnimatorStateInfo(BaseLayer);
        while (!IsInState(st, SwingShortHash) && guard++ < 120)
        {
            yield return null;
            st = animator.GetCurrentAnimatorStateInfo(BaseLayer);
        }

        // 2) 스윙 진입을 못하면(가드 초과) 안전하게 고정 지연 후 던진다
        float delay = Random.Range(throwAfterSwingDelayRange.x, throwAfterSwingDelayRange.y);
        if (!IsInState(st, SwingShortHash))
            yield return new WaitForSeconds(delay);
        else
        {
            // 스윙에 들어갔다면, 그 시점부터 지연
            float t = 0f;
            while (t < delay)
            {
                if (!pool.IsPlaying) { throwAfterRoutine = null; yield break; }
                t += Time.deltaTime;
                yield return null;
            }
        }

        // 3) 여기서 실제 던지기 계산/발사 (사운드도 이 타이밍에)
        DoActualThrow();

        throwAfterRoutine = null;
    }

    private void DoActualThrow()
    {
        if (!player || !pool || !pool.IsPlaying) return;

        Vector2 start = chaserMuzzle ? (Vector2)chaserMuzzle.position : (Vector2)transform.position;

        // 투척 ‘직전’에 최신 상태로 계산 (지연 동안 플레이어가 이동했을 수 있음)
        Vector2 v = playerRb ? playerRb.velocity : Vector2.right * 6f;
        float t = Random.Range(minFlightTime, maxFlightTime);
        Vector2 predicted = (Vector2)player.position + v * (t * leadFactor);
        predicted += Random.insideUnitCircle * spreadRadius;

        float g = Mathf.Abs(Physics2D.gravity.y) * (playerRb ? playerRb.gravityScale : 1f);
        Vector2 vel = SolveBallisticVelocity(start, predicted, t, g);
        if (vel == Vector2.zero) return;

        if (vel.magnitude > maxInitSpeed)
            vel = vel.normalized * maxInitSpeed;

        var obj = pool.Get();
        obj.Launch(start, vel);

        // 사운드를 투척 타이밍에 맞춤
        PlayThrowSound();
    }

    private void PlayThrowSound()
    {
        if (ThrowSound != null)
        {
            audioSource.PlayOneShot(ThrowSound);
        }
    }

    private Vector2 SolveBallisticVelocity(Vector2 start, Vector2 target, float time, float g)
    {
        Vector2 delta = target - start;
        float vx = delta.x / time;
        // y방향: s = v0*t - 0.5*g*t^2  → v0 = (s + 0.5*g*t^2)/t
        float vy = (delta.y + 0.5f * g * time * time) / time;
        if (float.IsNaN(vx) || float.IsNaN(vy)) return Vector2.zero;
        return new Vector2(vx, vy);
    }

    // ====== Trigger 기반 스윙 재생 ======
    private void TriggerSwingOnce()
    {
        if (animator == null) return;
        if (isSwinging) return;                 // 이미 스윙 중이면 무시(중복 방지)

        animator.ResetTrigger(ThrowOnceTrigger);
        animator.SetTrigger(ThrowOnceTrigger);  // Any State → Kids_swing

        // 스윙 진입/종료를 감시해서 isSwinging 토글
        if (swingWatchRoutine != null) StopCoroutine(swingWatchRoutine);
        swingWatchRoutine = StartCoroutine(Co_WatchSwingOnce());
    }

    private IEnumerator Co_WatchSwingOnce()
    {
        isSwinging = true;

        // 1) 스윙 상태 진입 대기 (최대 약 2초 가드)
        int guard = 0;
        yield return null;
        var st = animator.GetCurrentAnimatorStateInfo(BaseLayer);
        while (!IsInState(st, SwingShortHash) && guard++ < 120)
        {
            yield return null;
            st = animator.GetCurrentAnimatorStateInfo(BaseLayer);
        }

        // 2) 스윙 상태에서 normalizedTime >= 1 될 때까지 대기
        // (Kids_swing은 Loop Off여야 함)
        while (IsInState(st, SwingShortHash) && st.normalizedTime < 1f)
        {
            yield return null;
            st = animator.GetCurrentAnimatorStateInfo(BaseLayer);
        }

        // 3) 이후 전이는 Animator의 Kids_swing → Kids_run 전이가 처리
        isSwinging = false;
        swingWatchRoutine = null;
    }

    private bool IsInState(AnimatorStateInfo st, int shortNameHash)
    {
        return st.shortNameHash == shortNameHash || st.fullPathHash == shortNameHash;
    }
}
