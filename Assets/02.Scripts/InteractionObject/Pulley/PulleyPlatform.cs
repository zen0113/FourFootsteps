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
    
    [Header("이동 설정")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("레이어 설정")]
    [SerializeField] private string pulleyObjectLayerName = "PulleyObject";
    [SerializeField] private string pulleyNullLayerName = "PulleyNull";
    [SerializeField] private string groundLayerName = "Ground";
    
    [Header("현재 상태 (읽기 전용)")]
    [SerializeField] private float currentHeight = 0f;
    [SerializeField] private float targetHeight = 0f;
    [SerializeField] private bool isMoving = false;
    
    private ObjectDetector detector;
    private PulleySystem parentSystem;
    private Coroutine moveCoroutine;
    private Collider2D platformCollider;
    
    // 완전히 고정된 월드 좌표
    private Vector3 absoluteWorldPosition;
    
    // ★ 추가: 플랫폼 위의 PulleyObject 오브젝트들
    private List<GameObject> objectsOnPlatform = new List<GameObject>();
    private int pulleyObjectLayer;
    private int pulleyNullLayer;
    private int groundLayer;
    
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
        
        // 에디터에서 설정한 원래 위치를 절대 좌표로 저장
        absoluteWorldPosition = transform.position;
        
        // Collider 캐시
        platformCollider = GetComponent<Collider2D>();
        
        // ★ 레이어 ID 캐시
        pulleyObjectLayer = LayerMask.NameToLayer(pulleyObjectLayerName);
        pulleyNullLayer = LayerMask.NameToLayer(pulleyNullLayerName);
        groundLayer = LayerMask.NameToLayer(groundLayerName);
        
        if (pulleyObjectLayer == -1 || pulleyNullLayer == -1 || groundLayer == -1)
        {
            Debug.LogError($"PulleyPlatform({name}): 레이어를 찾을 수 없습니다!");
            Debug.LogError($"  - {pulleyObjectLayerName}: {pulleyObjectLayer}");
            Debug.LogError($"  - {pulleyNullLayerName}: {pulleyNullLayer}");
            Debug.LogError($"  - {groundLayerName}: {groundLayer}");
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
        
        // 컴포넌트 찾기
        detector = GetComponentInChildren<ObjectDetector>();
        parentSystem = FindObjectOfType<PulleySystem>();
        
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
        Vector3 worldPos = transform.position;
        Vector3 worldRotation = transform.eulerAngles;
        Vector3 worldScale = transform.lossyScale;
        
        transform.SetParent(null);
        
        transform.position = worldPos;
        transform.eulerAngles = worldRotation;
        transform.localScale = worldScale;
        
        Debug.Log($"PulleyPlatform({name}) 부모에서 분리됨. 월드 위치: {worldPos}");
    }
    
    private void LateUpdate()
    {
        // 매 프레임 강제로 월드 좌표 고정
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
        
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
        }
        
        moveCoroutine = StartCoroutine(MovePlatformCoroutine());
    }
    
    private IEnumerator MovePlatformCoroutine()
    {
        isMoving = true;
        
        // ★ 플랫폼 위의 모든 PulleyObject를 PulleyNull로 변경 + 충돌 무시 설정
        ChangeLayerForObjectsOnPlatform(pulleyNullLayer);
        
        float startHeight = currentHeight;
        float distance = Mathf.Abs(targetHeight - startHeight);
        
        float duration = distance / moveSpeed;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            
            float curveValue = moveCurve.Evaluate(progress);
            
            currentHeight = Mathf.Lerp(startHeight, targetHeight, curveValue);
            
            Vector3 newPosition = absoluteWorldPosition + Vector3.up * currentHeight;
            transform.position = newPosition;
            
            yield return null;
        }
        
        currentHeight = targetHeight;
        transform.position = absoluteWorldPosition + Vector3.up * currentHeight;
        isMoving = false;
        
        // ★ 이동 완료 후 PulleyNull을 다시 PulleyObject로 변경 + 충돌 다시 활성화
        ChangeLayerForObjectsOnPlatform(pulleyObjectLayer);
        
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
            
            // ★ 중단 시에도 레이어 복원
            ChangeLayerForObjectsOnPlatform(pulleyObjectLayer);
        }
    }
    
    /// <summary>
    /// ★ 플랫폼 위의 모든 PulleyObject의 레이어를 변경 + Physics2D 충돌 설정
    /// </summary>
    private void ChangeLayerForObjectsOnPlatform(int targetLayer)
    {
        // ★ 더 큰 범위로 검사
        Vector3 platformMin = transform.position - new Vector3(transform.localScale.x * 0.5f, 0.5f, 0);
        Vector3 platformMax = transform.position + new Vector3(transform.localScale.x * 0.5f, transform.localScale.y * 0.5f + 1f, 0);
        
        Collider2D[] collidersOnPlatform = Physics2D.OverlapAreaAll(platformMin, platformMax);
        
        objectsOnPlatform.Clear();
        foreach (Collider2D col in collidersOnPlatform)
        {
            // 플랫폼 자신은 제외
            if (col.gameObject == gameObject)
                continue;
            
            // PulleyObject 또는 PulleyNull 레이어만 처리
            if (col.gameObject.layer == pulleyObjectLayer || col.gameObject.layer == pulleyNullLayer)
            {
                objectsOnPlatform.Add(col.gameObject);
            }
        }
        
        // 수집된 오브젝트들의 레이어 변경 (자식 포함)
        foreach (GameObject obj in objectsOnPlatform)
        {
            if (obj != null)
            {
                // ★ 자신과 모든 자식의 레이어 변경
                SetLayerRecursively(obj, targetLayer);
                
                // ★ Physics2D 충돌 설정
                if (targetLayer == pulleyNullLayer)
                {
                    // PulleyNull은 Ground와 충돌하지 않도록 설정
                    Physics2D.IgnoreLayerCollision(pulleyNullLayer, groundLayer, true);
                    Debug.Log($"[{platformName}] ✓ '{obj.name}' → PulleyNull (Ground와 충돌 무시)");
                }
                else
                {
                    // PulleyObject는 다시 Ground와 충돌하도록 설정
                    Physics2D.IgnoreLayerCollision(pulleyObjectLayer, groundLayer, false);
                    Debug.Log($"[{platformName}] ✓ '{obj.name}' → PulleyObject (Ground와 충돌 활성화)");
                }
            }
        }
    }
    
    /// <summary>
    /// ★ 오브젝트와 모든 자식의 레이어를 재귀적으로 변경
    /// </summary>
    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        
        // 모든 자식 오브젝트에 적용
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
    
    /// <summary>
    /// 초기 높이 설정
    /// </summary>
    public void SetInitialHeight(float height)
    {
        currentHeight = height;
        targetHeight = height;
        transform.position = absoluteWorldPosition + Vector3.up * height;
    }
    
    /// <summary>
    /// 절대 월드 시작 위치 재설정
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
    
    private void OnDestroy()
    {
        if (detector != null)
        {
            detector.OnPriorityChanged -= OnDetectorPriorityChanged;
        }
    }
}