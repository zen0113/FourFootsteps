using UnityEngine;
using System.Collections;

/// <summary>
/// 도르레 시스템의 개별 플랫폼을 관리하는 컴포넌트 (완전 월드 좌표 고정 버전)
/// </summary>
public class PulleyPlatform : MonoBehaviour
{
    [Header("플랫폼 설정")]
    [SerializeField] private string platformName = "Platform";
    
    [Header("이동 설정")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("현재 상태 (읽기 전용)")]
    [SerializeField] private float currentHeight = 0f;
    [SerializeField] private float targetHeight = 0f;
    [SerializeField] private bool isMoving = false;
    
    private ObjectDetector detector;
    private PulleySystem parentSystem;
    private Coroutine moveCoroutine;
    
    // 완전히 고정된 월드 좌표
    private Vector3 absoluteWorldPosition;
    
    // 상태 정보
    public ObjectType CurrentPriority { get; private set; } = ObjectType.Empty;
    public float CurrentWeight { get; private set; } = 0f;
    public bool IsMoving => isMoving;
    public float CurrentHeight => currentHeight;
    public float TargetHeight => targetHeight;
    
    // 이벤트
    public System.Action<PulleyPlatform, ObjectType, float> OnPriorityChanged;
    public System.Action<PulleyPlatform> OnMoveComplete;
    
    private void Awake()
    {
        // 부모에서 완전히 분리하여 월드 루트에 배치
        DetachFromParent();
        
        // 에디터에서 설정한 원래 위치를 절대 좌표로 저장 (가장 중요!)
        absoluteWorldPosition = transform.position;
    }
    
    private void Start()
    {
        // 에디터에서 설정된 원래 위치를 절대 월드 좌표로 사용
        // (Start에서 transform.position을 사용하지 않음)
        if (absoluteWorldPosition == Vector3.zero)
        {
            // Awake에서 설정되지 않았다면 현재 위치 사용
            absoluteWorldPosition = transform.position;
        }
        currentHeight = 0f;
        targetHeight = 0f;
        
        // 컴포넌트 찾기
        detector = GetComponentInChildren<ObjectDetector>();
        parentSystem = FindObjectOfType<PulleySystem>(); // 부모 관계 없이 찾기
        
        if (detector == null)
        {
            Debug.LogError($"PulleyPlatform({name})에서 ObjectDetector를 찾을 수 없습니다!");
            return;
        }
        
        // 이벤트 연결
        detector.OnPriorityChanged += OnDetectorPriorityChanged;
        
        Debug.Log($"PulleyPlatform({platformName}) 절대 월드 좌표 고정: {absoluteWorldPosition}");
    }
    
    private void DetachFromParent()
    {
        // 현재 월드 위치 저장
        Vector3 worldPos = transform.position;
        Vector3 worldRotation = transform.eulerAngles;
        Vector3 worldScale = transform.lossyScale;
        
        // 부모에서 분리
        transform.SetParent(null);
        
        // 월드 좌표 그대로 유지
        transform.position = worldPos;
        transform.eulerAngles = worldRotation;
        transform.localScale = worldScale;
        
        Debug.Log($"PulleyPlatform({name}) 부모에서 분리됨. 월드 위치: {worldPos}");
    }
    
    private void LateUpdate()
    {
        // 매 프레임 강제로 월드 좌표 고정 (다른 시스템의 영향 차단)
        if (!isMoving)
        {
            Vector3 expectedPos = absoluteWorldPosition + Vector3.up * currentHeight;
            if (Vector3.Distance(transform.position, expectedPos) > 0.01f)
            {
                transform.position = expectedPos;
                Debug.LogWarning($"PulleyPlatform({platformName}) 위치 강제 복구: {expectedPos}");
            }
        }
    }
    
    private void OnDetectorPriorityChanged(ObjectType priority, float weight)
    {
        CurrentPriority = priority;
        CurrentWeight = weight;
        
        // 부모 시스템에 변경 사항 알림
        OnPriorityChanged?.Invoke(this, priority, weight);
    }
    
    /// <summary>
    /// 특정 높이로 이동 (완전 절대 월드 좌표 기반)
    /// </summary>
    public void MoveToHeight(float height)
    {
        if (Mathf.Approximately(currentHeight, height))
        {
            return;
        }
        
        targetHeight = height;
        
        // 기존 이동 중지
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
        }
        
        moveCoroutine = StartCoroutine(MovePlatformCoroutine());
    }
    
    private IEnumerator MovePlatformCoroutine()
    {
        isMoving = true;
        
        float startHeight = currentHeight;
        float distance = Mathf.Abs(targetHeight - startHeight);
        
        // 이동 시간 계산
        float duration = distance / moveSpeed;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            
            // 애니메이션 커브 적용
            float curveValue = moveCurve.Evaluate(progress);
            
            // 높이 보간
            currentHeight = Mathf.Lerp(startHeight, targetHeight, curveValue);
            
            // 절대 월드 좌표로 위치 설정
            Vector3 newPosition = absoluteWorldPosition + Vector3.up * currentHeight;
            transform.position = newPosition;
            
            yield return null;
        }
        
        // 최종 위치 설정
        currentHeight = targetHeight;
        transform.position = absoluteWorldPosition + Vector3.up * currentHeight;
        isMoving = false;
        
        // 이동 완료 이벤트
        OnMoveComplete?.Invoke(this);
        
        moveCoroutine = null;
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
    /// 초기 높이 설정 (절대 월드 좌표 기준)
    /// </summary>
    public void SetInitialHeight(float height)
    {
        currentHeight = height;
        targetHeight = height;
        transform.position = absoluteWorldPosition + Vector3.up * height;
    }
    
    /// <summary>
    /// 절대 월드 시작 위치 재설정 (에디터에서만 사용)
    /// </summary>
    public void SetAbsoluteWorldPosition(Vector3 position)
    {
        absoluteWorldPosition = position;
        transform.position = absoluteWorldPosition + Vector3.up * currentHeight;
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
        // 절대 월드 기준점 표시
        Gizmos.color = Color.gray;
        Vector3 basePos = Application.isPlaying ? absoluteWorldPosition : transform.position;
        Gizmos.DrawWireCube(basePos, Vector3.one * 0.3f);
        
        // 현재 위치에서 기준점으로의 연결선
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(basePos, transform.position);
        
        // 현재 플랫폼 상태 표시
        Gizmos.color = isMoving ? Color.red : Color.green;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
        
        // 상태 정보 표시
        #if UNITY_EDITOR
        UnityEditor.Handles.color = Color.white;
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.8f, 
            $"{platformName}\nH: {currentHeight:F1}\nT: {targetHeight:F1}\nAbs: {(Application.isPlaying ? absoluteWorldPosition.ToString("F1") : "N/A")}\nParent: {(transform.parent?.name ?? "None")}");
        #endif
    }
    
    private void OnDestroy()
    {
        // 이벤트 해제
        if (detector != null)
        {
            detector.OnPriorityChanged -= OnDetectorPriorityChanged;
        }
    }
}