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
    [SerializeField] private Vector2 throwIntervalRange = new Vector2(1.2f, 2.4f);
    [SerializeField] private AnimationCurve difficultyOverX; // x좌표→난이도(0~1)
    [SerializeField] private float minFlightTime = 0.6f;
    [SerializeField] private float maxFlightTime = 1.4f;

    [Header("Aiming")]
    [SerializeField] private float leadFactor = 0.55f; // 플레이어 속도 반영 비율
    [SerializeField] private float spreadRadius = 0.6f; // 랜덤 분산
    [SerializeField] private float maxInitSpeed = 18f; // 너무 급한 투척 제한
    [SerializeField] private LayerMask groundMask;     // 사전 충돌 레이캐스트(선택)

    private bool running;

    private void Awake()
    {
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
        while (running)
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
            TryThrowOnce();
        }
    }

    private void TryThrowOnce()
    {
        if (!player || !pool || !pool.IsPlaying) return;

        Vector2 start = chaserMuzzle ? (Vector2)chaserMuzzle.position : (Vector2)transform.position;

        // 예측 목표
        Vector2 v = playerRb ? playerRb.velocity : Vector2.right * 6f;
        float t = Random.Range(minFlightTime, maxFlightTime);
        Vector2 predicted = (Vector2)player.position + v * (t * leadFactor);

        // 랜덤 분산
        predicted += Random.insideUnitCircle * spreadRadius;

        // 초기 속도 계산
        float g = Mathf.Abs(Physics2D.gravity.y) * (playerRb ? playerRb.gravityScale : 1f);
        Vector2 vel = SolveBallisticVelocity(start, predicted, t, g);
        if (vel == Vector2.zero) return; // 실패시 스킵

        // 속도 제한
        if (vel.magnitude > maxInitSpeed)
            vel = vel.normalized * maxInitSpeed;

        var obj = pool.Get();
        obj.Launch(start, vel);
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
}
