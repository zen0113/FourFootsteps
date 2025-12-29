using UnityEngine;

/// <summary>
/// 현실적인 도르레 시스템 - 분리된 플랫폼과 작동 (PhysicsObject 우선권 잠금 버전)
/// </summary>
public class PulleySystem : MonoBehaviour
{
    [Header("플랫폼 설정")]
    [SerializeField] private PulleyPlatform platformA;
    [SerializeField] private PulleyPlatform platformB;

    [Header("도르레 물리 설정")]
    [SerializeField] private float maxHeight = 4f;
    [SerializeField] private float minHeight = -4f;
    [SerializeField] private float weightThreshold = 0.1f;
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private float stateChangeCooldown = 0.2f; // 타겟 흔들림으로 중간에서 멈추는 현상 방지

    [Header("상태 모니터링 (읽기 전용)")]
    [SerializeField] private ObjectType platformA_Priority = ObjectType.Empty;
    [SerializeField] private ObjectType platformB_Priority = ObjectType.Empty;
    [SerializeField] private float platformA_Weight = 0f;
    [SerializeField] private float platformB_Weight = 0f;
    [SerializeField] private string currentState = "Idle";
    [SerializeField] private string lastAction = "None";

    // ✅ 추가: 현재 우선권을 가진 플랫폼 (오브젝트가 올라간 쪽)
    private PulleyPlatform activePlatform = null;

    private float nextAllowedStateChangeTime = 0f;
    private float lastTargetA = float.NaN;
    private float lastTargetB = float.NaN;

    [Header("우선권 해제 유예(떨림 방지)")]
    [SerializeField] private float activeReleaseGraceTime = 0.25f;
    private float activeReleaseAtTime = -1f;

    private void Start()
    {
        if (platformA == null || platformB == null)
            AutoFindPlatforms();

        if (platformA == null || platformB == null)
        {
            Debug.LogError("PulleySystem: 플랫폼 A 또는 B가 설정되지 않았습니다!");
            return;
        }

        SetupInitialPositions();

        platformA.OnPriorityChanged += OnPlatformAChanged;
        platformB.OnPriorityChanged += OnPlatformBChanged;

        platformA.OnMoveComplete += OnPlatformMoveComplete;
        platformB.OnMoveComplete += OnPlatformMoveComplete;

        EvaluateSystemState();

        if (enableDebugLogs)
            Debug.Log("✓ 현실적인 PulleySystem 초기화 완료 (분리된 플랫폼)");
    }

    private void FixedUpdate()
    {
        // 물리 상태(Trigger/Collision) 기반 시스템이므로 FixedUpdate에서 평가하는 게 더 안정적입니다.
        EvaluateSystemState();
    }

    private void SetupInitialPositions()
    {
        platformA.SetInitialHeight(0f);
        platformB.SetInitialHeight(0f);
    }

    private void AutoFindPlatforms()
    {
        PulleyPlatform[] allPlatforms = FindObjectsOfType<PulleyPlatform>();
        if (allPlatforms.Length >= 2)
        {
            platformA = allPlatforms[0];
            platformB = allPlatforms[1];
        }
    }

    private void OnPlatformAChanged(PulleyPlatform platform, ObjectType priority, float weight)
    {
        platformA_Priority = priority;
        platformA_Weight = weight;
        HandlePriorityChange(platformA, priority);
    }

    private void OnPlatformBChanged(PulleyPlatform platform, ObjectType priority, float weight)
    {
        platformB_Priority = priority;
        platformB_Weight = weight;
        HandlePriorityChange(platformB, priority);
    }

    /// <summary>
    /// ✅ 핵심: 이벤트 우선권 잠금 처리
    /// </summary>
    private void HandlePriorityChange(PulleyPlatform source, ObjectType priority)
    {
        if (enableDebugLogs)
            Debug.Log($"[HandlePriorityChange] {source.name}: {priority}");

        // 🔒 현재 오브젝트가 올라간 쪽이 active라면, 그게 유지되는 한 무시
        if (activePlatform != null && activePlatform != source)
        {
            if (activePlatform.CurrentPriority == ObjectType.PhysicsObject)
            {
                if (enableDebugLogs)
                    Debug.Log($"🚫 {source.name} 이벤트 무시됨 — {activePlatform.name}에 오브젝트가 우선권 유지 중");
                return;
            }
        }

        // 🟢 오브젝트가 새로 감지된 경우, 무조건 그쪽이 우선권을 가져감
        if (priority == ObjectType.PhysicsObject)
        {
            activePlatform = source;
            if (enableDebugLogs)
                Debug.Log($"🔒 우선권 설정: {activePlatform.name} (PhysicsObject)");
        }

        // 🟡 오브젝트가 내려가서 감지 해제된 경우 → lock 해제
        if (activePlatform == source && priority == ObjectType.Empty)
        {
            if (enableDebugLogs)
                Debug.Log($"⏳ 우선권 해제 예약: {source.name} (+{activeReleaseGraceTime:F2}s)");
            activeReleaseAtTime = Time.time + activeReleaseGraceTime;
        }

        // 시스템 재평가
        EvaluateSystemState();
    }

    private void OnPlatformMoveComplete(PulleyPlatform platform)
    {
        if (enableDebugLogs)
            Debug.Log($"✓ 플랫폼 이동 완료: {platform.name}");
    }

    private void EvaluateSystemState()
    {
        if (platformA == null || platformB == null) return;

        // 예약된 우선권 해제 처리(유예 시간 동안 다시 들어오면 유지)
        if (activePlatform != null && activeReleaseAtTime > 0f && Time.time >= activeReleaseAtTime)
        {
            if (activePlatform.CurrentPriority == ObjectType.Empty)
            {
                if (enableDebugLogs)
                    Debug.Log($"🔓 우선권 해제 확정: {activePlatform.name}");
                activePlatform = null;
            }
            activeReleaseAtTime = -1f;
        }

        if (activePlatform != null)
        {
            // 다시 감지되면 해제 예약 취소
            if (activePlatform.CurrentPriority == ObjectType.PhysicsObject)
                activeReleaseAtTime = -1f;

            // 🔒 activePlatform이 설정되어 있으면 오브젝트 쪽 유지
            if (activePlatform == platformA)
                MovePlatformsToExtremes(minHeight, maxHeight, $"{activePlatform.name} locked (PhysicsObject)");
            else
                MovePlatformsToExtremes(maxHeight, minHeight, $"{activePlatform.name} locked (PhysicsObject)");
            return;
        }

        // 평상시 로직 (우선권 없을 때만)
        ApplyRealisticPulleyPhysics();
    }

    private void ApplyRealisticPulleyPhysics()
    {
        bool hasWeightA = platformA_Priority != ObjectType.Empty;
        bool hasWeightB = platformB_Priority != ObjectType.Empty;

        if (!hasWeightA && !hasWeightB)
        {
            MovePlatformsToBalance("Both empty");
            return;
        }

        if (hasWeightA && !hasWeightB)
        {
            MovePlatformsToExtremes(minHeight, maxHeight, "Only A has weight");
        }
        else if (!hasWeightA && hasWeightB)
        {
            MovePlatformsToExtremes(maxHeight, minHeight, "Only B has weight");
        }
        else
        {
            CompareWeightsAndMove();
        }
    }

    private void CompareWeightsAndMove()
    {
        if (platformA_Priority > platformB_Priority)
            MovePlatformsToExtremes(minHeight, maxHeight, "A higher priority");
        else if (platformB_Priority > platformA_Priority)
            MovePlatformsToExtremes(maxHeight, minHeight, "B higher priority");
        else
            HandleSameTypeComparison();
    }

    private void HandleSameTypeComparison()
    {
        float diff = platformA_Weight - platformB_Weight;
        if (Mathf.Abs(diff) <= weightThreshold)
        {
            MovePlatformsToBalance("Balanced weights");
        }
        else if (diff > 0)
        {
            MovePlatformsToExtremes(minHeight, maxHeight, "A heavier");
        }
        else
        {
            MovePlatformsToExtremes(maxHeight, minHeight, "B heavier");
        }
    }

    private void MovePlatformsToExtremes(float a, float b, string reason)
    {
        // (중요) 감지 떨림/플레이어 접촉 등으로 상태가 매 프레임 바뀌면
        // 플랫폼이 목표에 도달하기 전에 타겟이 다시 바뀌어 "중간에서 멈춘 것처럼" 보일 수 있습니다.
        // 짧은 쿨다운으로 상태 변경을 억제합니다.
        if (Time.time < nextAllowedStateChangeTime)
            return;

        if (!float.IsNaN(lastTargetA) && Mathf.Approximately(lastTargetA, a) &&
            !float.IsNaN(lastTargetB) && Mathf.Approximately(lastTargetB, b))
        {
            // 같은 타겟이면 굳이 상태 갱신하지 않음
            return;
        }

        platformA.MoveToHeight(a);
        platformB.MoveToHeight(b);
        currentState = reason;
        lastAction = $"A→{a:F1}, B→{b:F1}";

        lastTargetA = a;
        lastTargetB = b;
        nextAllowedStateChangeTime = Time.time + stateChangeCooldown;
    }

    private void MovePlatformsToBalance(string reason)
    {
        if (Time.time < nextAllowedStateChangeTime)
            return;

        if (!float.IsNaN(lastTargetA) && Mathf.Approximately(lastTargetA, 0f) &&
            !float.IsNaN(lastTargetB) && Mathf.Approximately(lastTargetB, 0f))
        {
            return;
        }

        platformA.MoveToHeight(0);
        platformB.MoveToHeight(0);
        currentState = reason;
        lastAction = "Balance";

        lastTargetA = 0f;
        lastTargetB = 0f;
        nextAllowedStateChangeTime = Time.time + stateChangeCooldown;
    }

    private void OnDestroy()
    {
        if (platformA != null)
        {
            platformA.OnPriorityChanged -= OnPlatformAChanged;
            platformA.OnMoveComplete -= OnPlatformMoveComplete;
        }
        if (platformB != null)
        {
            platformB.OnPriorityChanged -= OnPlatformBChanged;
            platformB.OnMoveComplete -= OnPlatformMoveComplete;
        }
    }
}
