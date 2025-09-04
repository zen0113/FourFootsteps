using UnityEngine;

[CreateAssetMenu(
    fileName = "StealthSettings",
    menuName = "Game/Stealth Settings",
    order = 0
)]
public class StealthSettingsSO : ScriptableObject
{
    // 플레이어 오브젝트에 숨기 관련 변수들
    [Header("Stealth Timings")]
    [Tooltip("은신 진입 후 무적(면책) 시간")]
    public float graceSeconds = 0.18f;
    [Tooltip("진입/이탈 버퍼(경계 떨림 방지)")]
    public float enterExitBuffer = 0.12f;

    [Header("Player Stealth Movement")]
    public float pushSpeed = 8f;
    [Tooltip("목표 x에 스냅 허용 오차")]
    public float snapTolerance = 0.01f;
    [Tooltip("은신 오브젝트 밖으로 밀어낼 거리 가산값")]
    public float pushOutsidePadding = 0.6f;

    [Header("Input")]
    public KeyCode toggleKey = KeyCode.E;

    // SFX 관련 변수들
    [Header("Camera Effect Value")]
    public float enterHideSize = 3.2f;
    public float exitHideSize = 5f;
    [Tooltip("카메라 줌인/줌아웃 되는 시간")]
    public float sizeDuration = 1f;

    // Kids 관련 변수들
    // 플레이어 쪽을 랜덤하게 바라보는 Watching 시간과 의심 변수,
    // 이미지 Scale 변수 포함
    [Header("Kids Distance → Scale")]
    public float minDistance = 1.0f;      // 가까울수록 크다
    public float maxDistance = 100.0f;      // 멀수록 작다
    public float scaleAtMin = 0.7f;
    public float scaleAtMax = 0.3f;
    public float scaleSmooth = 10f;

    [Header("Kids Watching Timing (random)")]
    public Vector2 idleInterval = new Vector2(3f, 5f);   // 평상시 대기
    public Vector2 watchDuration = new Vector2(5f, 10f);  // 쳐다보는 시간

    [Header("Kids Suspicion (per second)")]
    public float gainPerSecond = 30f;     // 보는 중 + 노출 시 초당 증가
    public float decayPerSecond = 5f;    // 안 보는 중/은신 중 초당 감소
    public AnimationCurve proximityFactor = AnimationCurve.Linear(0, 1, 1, 1); // 0~1(가까울수록 1)

    [Header("Suspicion Gauge [UI]")]
    public float gaugeMax = 100f;
    public float gaugeCurrent = 0f;
}
