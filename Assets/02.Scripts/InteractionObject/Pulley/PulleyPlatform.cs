using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 도르레 시스템의 개별 플랫폼을 관리하는 컴포넌트 (완전 월드 좌표 고정 버전)
/// </summary>
public class PulleyPlatform : MonoBehaviour
{
    [Header("플랫폼 설정")]
    [SerializeField] private string platformName = "Platform";

    [Header("박스 고정 스냅 포인트")]
    [SerializeField] private Transform hardLockSnapPoint;
    [Tooltip("스냅 포인트에 부착된 Collider2D(보통 isTrigger=true 권장). 비워두면 hardLockSnapPoint에서 자동으로 찾습니다.")]
    [SerializeField] private Collider2D hardLockSnapCollider;
    
    [Header("이동 설정")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private bool forceReachTargetPosition = true; // MovePosition이 충돌에 막혀 목표 높이까지 못 가는 경우 방지
    
    [Header("바닥 오브젝트 관리")]
    [SerializeField] private GameObject[] floorObjectsToDisable = new GameObject[0]; // 내려갈 때 비활성화할 바닥 오브젝트들
    [SerializeField] private bool autoFindFloorObjectsByName = false; // 이름으로 자동 찾기
    [SerializeField] private string floorObjectNamePattern = "StealGround"; // 찾을 오브젝트 이름 패턴
    [SerializeField] private float disableFloorHeight = 0f; // 이 높이 이하로 내려가면 바닥 오브젝트 비활성화
    [Tooltip("disableFloorHeight 경계에서 흔들릴 때 토글 스팸을 줄이기 위한 여유값")]
    [SerializeField] private float floorToggleHysteresis = 0.05f;
    private bool floorObjectsDisabled = false;
    private readonly List<Collider2D> disabledColliders = new List<Collider2D>(); // 비활성화한 콜라이더들
    private float floorStateChangeCooldown = 0f; // 상태 변경 쿨다운
    private List<GameObject> actualFloorObjects = new List<GameObject>(); // 실제 비활성화할 오브젝트 리스트

    // 여러 플랫폼이 같은 바닥 오브젝트를 공유할 수 있어, 플랫폼별 토글이 충돌하지 않도록 Ref-count로 관리
    private static readonly Dictionary<GameObject, int> s_floorDisableRefCount = new Dictionary<GameObject, int>();
    private static readonly Dictionary<Collider2D, int> s_colliderDisableRefCount = new Dictionary<Collider2D, int>();
    
    [Header("현재 상태 (읽기 전용)")]
    [SerializeField] private float currentHeight = 0f;
    [SerializeField] private float targetHeight = 0f;
    [SerializeField] private bool isMoving = false;
    
    private ObjectDetector detector;
    private PulleySystem parentSystem;
    private Coroutine moveCoroutine;
    private Collider2D platformCollider;
    private Rigidbody2D platformRigidbody;
    
    // 완전히 고정된 월드 좌표
    private Vector3 absoluteWorldPosition;
    
    // (중요) Rigidbody2D를 사용하는 오브젝트를 Transform으로 직접 이동시키면
    // 물리 Solver가 큰 보정 임펄스를 만들어 “밀면 날아감/튕김”이 발생하기 쉽습니다.
    // 따라서 플랫폼 자체를 Kinematic Rigidbody2D로 움직이고,
    // 탑승 오브젝트는 물리 접촉으로 자연스럽게 따라오도록 합니다.
    
    // 상태 정보
    public ObjectType CurrentPriority { get; private set; } = ObjectType.Empty;
    public float CurrentWeight { get; private set; } = 0f;
    public bool IsMoving => isMoving;
    public float CurrentHeight => currentHeight;
    public float TargetHeight => targetHeight;
    public Transform HardLockSnapPoint => hardLockSnapPoint;
    public Collider2D HardLockSnapCollider => hardLockSnapCollider;
    
    // 이벤트
    public System.Action<PulleyPlatform, ObjectType, float> OnPriorityChanged;
    public System.Action<PulleyPlatform> OnMoveComplete;
    
    private void Awake()
    {
        DetachFromParent();
        absoluteWorldPosition = transform.position;
        platformCollider = GetComponent<Collider2D>();
        platformRigidbody = GetComponent<Rigidbody2D>();

        // 스냅 콜라이더 자동 연결(지정 누락 방지)
        if (hardLockSnapCollider == null && hardLockSnapPoint != null)
            hardLockSnapCollider = hardLockSnapPoint.GetComponent<Collider2D>();

        if (platformRigidbody == null)
        {
            // 씬 세팅 누락 방지: 플랫폼은 움직이므로 Rigidbody2D가 있는 편이 안정적입니다.
            platformRigidbody = gameObject.AddComponent<Rigidbody2D>();
            Debug.LogWarning($"PulleyPlatform({platformName})에 Rigidbody2D가 없어 자동 추가했습니다. (Kinematic)");
        }

        // 플랫폼은 물리계(고정 타임스텝)에서 이동시키는 것이 안정적입니다.
        platformRigidbody.bodyType = RigidbodyType2D.Kinematic;
        platformRigidbody.gravityScale = 0f;
        platformRigidbody.angularVelocity = 0f;
        platformRigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;

        if (platformCollider != null && platformCollider.isTrigger)
        {
            Debug.LogWarning($"PulleyPlatform({platformName})의 플랫폼 Collider가 Trigger입니다. (감지용 Trigger는 자식 ObjectDetector에 두고, 플랫폼은 Non-Trigger 권장)");
        }
    }
    
    private void Start()
    {
        if (absoluteWorldPosition == Vector3.zero)
        {
            absoluteWorldPosition = transform.position;
        }
        currentHeight = 0f;
        targetHeight = 0f;
        
        detector = GetComponentInChildren<ObjectDetector>();
        parentSystem = FindObjectOfType<PulleySystem>();
        
        if (detector == null)
        {
            Debug.LogError($"PulleyPlatform({name})에서 ObjectDetector를 찾을 수 없습니다!");
            return;
        }
        
        detector.OnPriorityChanged += OnDetectorPriorityChanged;
        
        // 바닥 오브젝트 자동 찾기
        CollectFloorObjects();
        
        Debug.Log($"PulleyPlatform({platformName}) 절대 월드 좌표 고정: {absoluteWorldPosition}");
    }
    
    /// <summary>
    /// 바닥 오브젝트 수집 (인스펙터 지정 + 자동 찾기)
    /// </summary>
    private void CollectFloorObjects()
    {
        actualFloorObjects.Clear();
        
        // 1. 인스펙터에서 지정한 오브젝트들 추가
        if (floorObjectsToDisable != null && floorObjectsToDisable.Length > 0)
        {
            foreach (var obj in floorObjectsToDisable)
            {
                if (obj != null && !actualFloorObjects.Contains(obj))
                {
                    actualFloorObjects.Add(obj);
                }
            }
        }
        
        // 2. 자동 찾기 옵션이 켜져있으면 이름으로 찾기
        if (autoFindFloorObjectsByName && !string.IsNullOrEmpty(floorObjectNamePattern))
        {
            GameObject[] allObjects = FindObjectsOfType<GameObject>(true); // 비활성화된 것도 포함
            foreach (var obj in allObjects)
            {
                if (obj != null && obj.name.Contains(floorObjectNamePattern))
                {
                    if (!actualFloorObjects.Contains(obj))
                    {
                        actualFloorObjects.Add(obj);
                    }
                }
            }
        }
        
        if (actualFloorObjects.Count > 0)
        {
            Debug.Log($"[PulleyPlatform][{platformName}] 바닥 오브젝트 {actualFloorObjects.Count}개 수집: {string.Join(", ", actualFloorObjects.ConvertAll(o => o.name))}");
        }
    }
    
    private void DetachFromParent()
    {
        // Unity가 월드 좌표/회전/스케일을 보존하도록 맡기는 게 가장 안전합니다.
        Transform oldParent = transform.parent;
        transform.SetParent(null, true);

        if (oldParent != null)
            Debug.Log($"PulleyPlatform({name}) 부모에서 분리됨. 월드 위치: {transform.position}");
    }
    
    private void LateUpdate()
    {
        // Rigidbody2D가 있는 경우 LateUpdate에서 Transform을 강제로 만지면
        // 물리 계산과 충돌 해결이 꼬일 수 있어 보정하지 않습니다.
        if (platformRigidbody != null) return;

        if (!isMoving)
        {
            Vector3 expectedPos = absoluteWorldPosition + Vector3.up * currentHeight;
            if (Vector3.Distance(transform.position, expectedPos) > 0.01f)
            {
                transform.position = expectedPos;
                Debug.LogWarning($"PulleyPlatform({platformName}) 위치 강제 복구(Transform): {expectedPos}");
            }
        }
    }

    private void FixedUpdate()
    {
        // 이동 코루틴 여부와 무관하게, 현재 높이에 따라 바닥 오브젝트 상태를 일관되게 갱신
        UpdateFloorObjectsByHeight(currentHeight);
    }
    
    private void OnDetectorPriorityChanged(ObjectType priority, float weight)
    {
        CurrentPriority = priority;
        CurrentWeight = weight;
        
        OnPriorityChanged?.Invoke(this, priority, weight);
    }
    
    /// <summary>
    /// 특정 높이로 이동
    /// </summary>
    public void MoveToHeight(float height)
    {
        if (isMoving && Mathf.Approximately(targetHeight, height))
        {
            return;
        }
        
        if (!isMoving && Mathf.Approximately(currentHeight, height))
        {
            return;
        }
        
        targetHeight = height;

        // 목표 높이가 disableFloorHeight 이하로 내려갈 예정이면, 충돌 해제를 위해 즉시 비활성화(실무에서 흔한 선(先)조치)
        float predictedLowest = Mathf.Min(currentHeight, targetHeight);
        UpdateFloorObjectsByHeight(predictedLowest);
        
        if (moveCoroutine != null)
        {
            // (중요) 코루틴을 Stop만 하면 중간 정리 로직이 실행되지 않습니다.
            // 이 플랫폼은 더 이상 충돌 무시/오브젝트 강제이동을 하지 않지만,
            // “중단 후 재시작” 자체가 안전하게 동작하도록 중단 처리를 통일합니다.
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
            isMoving = false;
        }
        
        moveCoroutine = StartCoroutine(MovePlatformCoroutine());
    }
    
    private IEnumerator MovePlatformCoroutine()
    {
        isMoving = true;
        startHeight = currentHeight;
        float distance = Mathf.Abs(targetHeight - startHeight);
        
        float duration = distance / moveSpeed;
        float elapsed = 0f;

        if (duration <= Mathf.Epsilon)
        {
            currentHeight = targetHeight;
            ApplyPlatformPosition(absoluteWorldPosition + Vector3.up * currentHeight);
            isMoving = false;
            OnMoveComplete?.Invoke(this);
            moveCoroutine = null;
            yield break;
        }
        
        // 이동 방향 확인 (내려가는지 올라가는지)
        bool isDescending = targetHeight < startHeight;
        
        while (elapsed < duration)
        {
            // 물리 스텝 기준으로 진행 (FixedUpdate 타이밍)
            yield return new WaitForFixedUpdate();
            elapsed += Time.fixedDeltaTime;
            float progress = elapsed / duration;
            
            float curveValue = moveCurve.Evaluate(progress);
            
            currentHeight = Mathf.Lerp(startHeight, targetHeight, curveValue);
            
            Vector3 newPosition = absoluteWorldPosition + Vector3.up * currentHeight;
            ApplyPlatformPosition(newPosition);

            // 현재 높이에 따라 바닥 상태 갱신(코루틴/타겟 변경에도 일관)
            UpdateFloorObjectsByHeight(currentHeight);
        }
        
        currentHeight = targetHeight;
        ApplyPlatformPosition(absoluteWorldPosition + Vector3.up * currentHeight);
        UpdateFloorObjectsByHeight(currentHeight);
        
        isMoving = false;
        // 바닥 오브젝트 복구/비활성화는 높이 기반으로 이미 처리됨
        
        OnMoveComplete?.Invoke(this);
        
        moveCoroutine = null;
    }

    private void ApplyPlatformPosition(Vector3 worldPos)
    {
        if (platformRigidbody != null)
        {
            // MovePosition은 충돌에 막히면 목표 위치까지 못 갈 수 있습니다.
            // (예: 플랫폼이 올라가며 벽/장치 콜라이더에 스치면 중간에서 멈춤)
            // 기존 Transform 기반 로직처럼 "원하는 높이까지 반드시 도달"이 필요하면 rb.position을 사용합니다.
            if (forceReachTargetPosition)
            {
                platformRigidbody.position = worldPos;
            }
            else
            {
                platformRigidbody.MovePosition(worldPos);
            }
        }
        else
        {
            transform.position = worldPos;
        }
    }
    
    public void StopMovement()
    {
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
            isMoving = false;
        }
    }
    
    /// <summary>
    /// 초기 높이 설정
    /// </summary>
    public void SetInitialHeight(float height)
    {
        currentHeight = height;
        targetHeight = height;
        ApplyPlatformPosition(absoluteWorldPosition + Vector3.up * height);
    }
    
    /// <summary>
    /// 절대 월드 시작 위치 재설정
    /// </summary>
    public void SetAbsoluteWorldPosition(Vector3 position)
    {
        absoluteWorldPosition = position;
        ApplyPlatformPosition(absoluteWorldPosition + Vector3.up * currentHeight);
        Debug.Log($"PulleyPlatform({platformName}) 절대 월드 위치 변경: {absoluteWorldPosition}");
    }
    
    /// <summary>
    /// 현재 위치를 절대 월드 위치로 고정
    /// </summary>
    [ContextMenu("현재 위치를 절대 좌표로 고정")]
    public void LockCurrentPositionAsAbsolute()
    {
        absoluteWorldPosition = transform.position - Vector3.up * currentHeight;
        Debug.Log($"PulleyPlatform({platformName}) 현재 위치를 절대 좌표로 고정: {absoluteWorldPosition}");
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.gray;
        Vector3 basePos = Application.isPlaying ? absoluteWorldPosition : transform.position;
        Gizmos.DrawWireCube(basePos, Vector3.one * 0.3f);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(basePos, transform.position);
        
        Gizmos.color = isMoving ? Color.red : Color.green;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
        
        #if UNITY_EDITOR
        UnityEditor.Handles.color = Color.white;
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.8f, 
            $"{platformName}\nH: {currentHeight:F1}\nT: {targetHeight:F1}\nAbs: {(Application.isPlaying ? absoluteWorldPosition.ToString("F1") : "N/A")}\nParent: {(transform.parent?.name ?? "None")}");
        #endif
    }
    
    /// <summary>
    /// 바닥 오브젝트 상태를 직접 설정 (안정적인 상태 변경)
    /// </summary>
    private void SetFloorObjectsState(bool shouldDisable)
    {
        // 수집한 바닥 오브젝트가 없으면 무시
        if (actualFloorObjects == null || actualFloorObjects.Count == 0) return;
        
        // 이미 원하는 상태면 무시
        if (shouldDisable == floorObjectsDisabled) return;
        
        // 쿨다운 체크 (너무 빠른 상태 변경 방지)
        if (Time.time < floorStateChangeCooldown) return;
        
        floorObjectsDisabled = shouldDisable;
        floorStateChangeCooldown = Time.time + 0.1f; // 0.1초 쿨다운

        int affectedCount = 0;
        foreach (var floorObj in actualFloorObjects)
        {
            if (floorObj == null) continue;

            if (shouldDisable)
            {
                AcquireFloorDisable(floorObj);
                affectedCount++;
            }
            else
            {
                ReleaseFloorDisable(floorObj);
                affectedCount++;
            }
        }
        
        if (shouldDisable)
        {
            Debug.Log($"[PulleyPlatform][{platformName}/{name}] 총 {affectedCount}개 바닥 오브젝트 비활성화 요청 처리 (H={currentHeight:F2}, T={targetHeight:F2})");
        }
    }

    private void AcquireFloorDisable(GameObject floorObj)
    {
        if (floorObj == null) return;

        int newCount = 1;
        if (s_floorDisableRefCount.TryGetValue(floorObj, out int existing))
            newCount = existing + 1;
        s_floorDisableRefCount[floorObj] = newCount;

        // 최초 요청일 때만 실제 disable 수행
        if (newCount != 1) return;

        // 물리 충돌 즉시 해제를 위해 collider도 disable(포함 비활성 오브젝트)
        Collider2D[] colliders = floorObj.GetComponentsInChildren<Collider2D>(true);
        foreach (var col in colliders)
        {
            if (col == null) continue;
            int cCount = 1;
            if (s_colliderDisableRefCount.TryGetValue(col, out int cExisting))
                cCount = cExisting + 1;
            s_colliderDisableRefCount[col] = cCount;

            if (cCount == 1)
                col.enabled = false;
        }

        floorObj.SetActive(false);
        Debug.Log($"[PulleyPlatform][{platformName}/{name}] Floor object '{floorObj.name}' DISABLED (ref=1, colliders={colliders.Length})");
    }

    private void ReleaseFloorDisable(GameObject floorObj)
    {
        if (floorObj == null) return;

        if (!s_floorDisableRefCount.TryGetValue(floorObj, out int existing) || existing <= 0)
            return;

        int newCount = existing - 1;
        if (newCount > 0)
        {
            s_floorDisableRefCount[floorObj] = newCount;
            return; // 아직 다른 플랫폼이 disable 유지 중
        }

        s_floorDisableRefCount.Remove(floorObj);

        // GameObject를 먼저 활성화한 뒤 collider를 복구
        floorObj.SetActive(true);

        Collider2D[] colliders = floorObj.GetComponentsInChildren<Collider2D>(true);
        foreach (var col in colliders)
        {
            if (col == null) continue;
            if (!s_colliderDisableRefCount.TryGetValue(col, out int cExisting) || cExisting <= 0)
            {
                col.enabled = true;
                continue;
            }

            int cNew = cExisting - 1;
            if (cNew > 0)
            {
                s_colliderDisableRefCount[col] = cNew;
            }
            else
            {
                s_colliderDisableRefCount.Remove(col);
                col.enabled = true;
            }
        }

        Debug.Log($"[PulleyPlatform][{platformName}/{name}] Floor object '{floorObj.name}' ENABLED (ref=0, colliders={colliders.Length})");
    }

    /// <summary>
    /// 현재/예측 높이에 따라 StealGround(바닥 오브젝트) 상태를 결정합니다.
    /// - height <= disableFloorHeight 이면 비활성화
    /// - height > disableFloorHeight + hysteresis 이면 활성화
    /// </summary>
    private void UpdateFloorObjectsByHeight(float height)
    {
        if (actualFloorObjects == null || actualFloorObjects.Count == 0) return;

        if (!floorObjectsDisabled)
        {
            if (height <= disableFloorHeight)
                SetFloorObjectsState(shouldDisable: true);
        }
        else
        {
            if (height > disableFloorHeight + floorToggleHysteresis)
                SetFloorObjectsState(shouldDisable: false);
        }
    }
    
    private float startHeight; // 이동 시작 높이 (ManageFloorObjects에서 사용)

    private void OnDestroy()
    {
        if (detector != null)
        {
            detector.OnPriorityChanged -= OnDetectorPriorityChanged;
        }
        
        // 파괴 시 바닥 오브젝트 복구
        if (floorObjectsDisabled)
        {
            SetFloorObjectsState(shouldDisable: false);
        }
    }
}