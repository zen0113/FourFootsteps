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
    
    // ★ 추가: 플랫폼 위의 오브젝트들
    private List<Transform> objectsOnPlatform = new List<Transform>();
    private Vector3 lastPlatformPosition;
    
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
        DetachFromParent();
        absoluteWorldPosition = transform.position;
        platformCollider = GetComponent<Collider2D>();
        lastPlatformPosition = transform.position;
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
        lastPlatformPosition = transform.position;
        
        // ★ 1. 플랫폼 위의 오브젝트 수집
        CollectObjectsOnPlatform();
        
        // ★ 2. 모든 오브젝트와 Ground의 충돌을 무시
        IgnoreGroundCollision(true);
        
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
            Vector3 platformDelta = newPosition - transform.position;
            
            transform.position = newPosition;
            
            // ★ 3. 플랫폼과 함께 오브젝트도 이동
            MoveObjectsWithPlatform(platformDelta);
            
            yield return null;
        }
        
        currentHeight = targetHeight;
        transform.position = absoluteWorldPosition + Vector3.up * currentHeight;
        
        // ★ 4. 이동 완료 후 충돌 복구
        IgnoreGroundCollision(false);
        
        isMoving = false;
        
        OnMoveComplete?.Invoke(this);
        
        moveCoroutine = null;
    }
    
    /// <summary>
    /// ★ 플랫폼 위의 오브젝트들을 수집
    /// </summary>
    private void CollectObjectsOnPlatform()
    {
        objectsOnPlatform.Clear();
        
        // 플랫폼 위의 모든 오브젝트 검사
        Collider2D[] collidersOnPlatform = Physics2D.OverlapAreaAll(
            transform.position - new Vector3(transform.localScale.x * 0.5f, 0.5f, 0),
            transform.position + new Vector3(transform.localScale.x * 0.5f, transform.localScale.y * 0.5f + 0.5f, 0)
        );
        
        foreach (Collider2D col in collidersOnPlatform)
        {
            if (col.gameObject == gameObject)
                continue;
            
            Rigidbody2D rb = col.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                objectsOnPlatform.Add(col.transform);
                Debug.Log($"[{platformName}] 수집: '{col.name}'");
            }
        }
    }
    
    /// <summary>
    /// ★ 오브젝트들과 Ground 레이어의 충돌 설정 변경
    /// </summary>
   private void IgnoreGroundCollision(bool ignore)
    {
        int groundLayer = LayerMask.NameToLayer("Ground");
        Collider2D[] groundColliders = FindObjectsOfType<Collider2D>();

        foreach (Transform objTransform in objectsOnPlatform)
        {
            if (objTransform == null) continue;

            Collider2D objCol = objTransform.GetComponent<Collider2D>();
            if (objCol == null) continue;

            foreach (var groundCol in groundColliders)
            {
                if (groundCol.gameObject.layer == groundLayer)
                {
                    Physics2D.IgnoreCollision(objCol, groundCol, ignore);
                }
            }

            Debug.Log($"[{platformName}] '{objTransform.name}' → Ground 충돌 {(ignore ? "무시" : "복구")}");
        }
    }
    
    /// <summary>
    /// ★ 플랫폼과 함께 오브젝트들도 이동
    /// </summary>
    private void MoveObjectsWithPlatform(Vector3 delta)
    {
        foreach (Transform objTransform in objectsOnPlatform)
        {
            if (objTransform != null)
            {
                objTransform.position += delta;
            }
        }
    }
    
    public void StopMovement()
    {
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
            isMoving = false;
            
            // ★ 중단 시 충돌 복구
            IgnoreGroundCollision(false);
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