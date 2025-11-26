using UnityEngine;

public enum DistanceMode { FixedRangeLegacy, AutoMaxFromFirstSample, AutoWindow }
public enum DistanceAxis { Euclidean2D, HorizontalX }

[CreateAssetMenu(fileName = "StealthSettings", menuName = "Game/Stealth Settings", order = 0)]
public class StealthSettingsSO : ScriptableObject
{
    // ===== 기존 필드 (당신 코드와 호환) =====
    [Header("Stealth Timings")]
    public float graceSeconds = 0.18f;
    public float enterExitBuffer = 0.12f;

    [Header("Hiding Alpha")]
    public float HidingAlphaValue = 0.8f;

    [Header("Player Stealth Movement")]
    public float pushSpeed = 8f;
    public float snapTolerance = 0.01f;
    public float pushOutsidePadding = 0.6f;

    [Header("Input")]
    public KeyCode toggleKey = KeyCode.E;

    [Header("Camera Effect Value")]
    public float enterHideSize = 3.2f;
    public float exitHideSize = 5f;
    public float sizeDuration = 1f;

    [Header("Kids Distance → Scale (LEGACY names)")]
    public float minDistance = 1.0f;   // 가까울수록 1
    public float maxDistance = 100.0f; // 멀수록 0
    public float scaleAtMin = 0.7f;
    public float scaleAtMax = 0.3f;
    public float scaleSmooth = 10f;

    [Header("Kids Watching Timing (random)")]
    public Vector2 idleInterval = new Vector2(3f, 5f);
    public Vector2 watchDuration = new Vector2(5f, 10f);

    [Header("Kids Suspicion (per second)")]
    public float gainPerSecond = 30f;
    public float decayPerSecond = 5f;
    public AnimationCurve proximityFactor = AnimationCurve.Linear(0, 1, 1, 1);

    [Header("Suspicion Gauge [UI]")]
    public float gaugeMax = 100f;
    public float gaugeCurrent = 0f;

    // ===== 새 로직용 추가 옵션 =====
    [Header("Distance→t Mapping (New)")]
    public DistanceMode distanceMode = DistanceMode.AutoMaxFromFirstSample;
    public DistanceAxis distanceAxis = DistanceAxis.Euclidean2D;

    [Tooltip("t=1로 보는 '가까움' 기준. <0이면 legacy minDistance 사용")]
    public float nearDistanceOverride = -1f;

    [Tooltip("FixedRangeLegacy에서 t=0 상한. <0이면 legacy maxDistance 사용")]
    public float fixedMaxOverride = -1f;

    [Range(0.01f, 1f)]
    public float autoWindowLerp = 0.15f;

    [Tooltip("이징(1=선형, 2~3: 오른쪽에 오래 머무름)")]
    public float easeExponent = 1f;

    public bool logComputedT = false;

    // 런타임 상태 (공유 SO 오염 방지: 반드시 Instantiate로 복제해서 사용)
    [System.NonSerialized] private bool _initialized;
    [System.NonSerialized] private float _runtimeMax;

    public void ResetRuntime()
    {
        _initialized = false;
        _runtimeMax = 0f;
    }

    public float GetDistance(Transform a, Transform b)
    {
        if (distanceAxis == DistanceAxis.HorizontalX)
            return Mathf.Abs(a.position.x - b.position.x);
        return Vector2.Distance(a.position, b.position);
    }

    /// <summary>
    /// 멀리=0, 가까이=1 로 클램프된 t (오토 캘리브레이션/윈도우/이징 포함)
    /// </summary>
    public float ComputeT(float d)
    {
        // 기준 하한(dMin)
        float dMin = (nearDistanceOverride >= 0f) ? Mathf.Max(0f, nearDistanceOverride)
                                                  : Mathf.Max(0f, minDistance);

        // 기준 상한 후보(dMax)
        float legacyMax = Mathf.Max(dMin + 0.01f, maxDistance);
        float fixedMax = (fixedMaxOverride >= 0f) ? Mathf.Max(dMin + 0.01f, fixedMaxOverride) : legacyMax;
        float dMax = fixedMax;

        switch (distanceMode)
        {
            case DistanceMode.FixedRangeLegacy:
                // dMax = fixedMax 그대로
                break;

            case DistanceMode.AutoMaxFromFirstSample:
                if (!_initialized)
                {
                    _runtimeMax = Mathf.Max(d, dMin + 0.01f);
                    _initialized = true;
                }
                dMax = Mathf.Max(_runtimeMax, dMin + 0.01f);
                break;

            case DistanceMode.AutoWindow:
                if (!_initialized)
                {
                    _runtimeMax = Mathf.Max(d, dMin + 0.01f);
                    _initialized = true;
                }
                _runtimeMax = Mathf.Lerp(_runtimeMax, Mathf.Max(d, dMin + 0.01f), autoWindowLerp);
                dMax = Mathf.Max(_runtimeMax, dMin + 0.01f);
                break;
        }

        float t = 1f - Mathf.InverseLerp(dMin, dMax, d);
        t = Mathf.Clamp01(t);
        if (!Mathf.Approximately(easeExponent, 1f))
            t = Mathf.Pow(t, easeExponent);

        if (logComputedT)
            Debug.Log($"[StealthSettingsSO] d:{d:F2} dMin:{dMin:F2} dMax:{dMax:F2} t:{t:F2}");
        return t;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        minDistance = Mathf.Max(0f, minDistance);
        maxDistance = Mathf.Max(minDistance + 0.01f, maxDistance);

        if (nearDistanceOverride >= 0f)
            nearDistanceOverride = Mathf.Max(0f, nearDistanceOverride);

        if (fixedMaxOverride >= 0f)
            fixedMaxOverride = Mathf.Max(((nearDistanceOverride >= 0f) ? nearDistanceOverride : minDistance) + 0.01f,
                                         fixedMaxOverride);

        autoWindowLerp = Mathf.Clamp01(autoWindowLerp);
        easeExponent = Mathf.Max(0.01f, easeExponent);
    }
#endif
}
