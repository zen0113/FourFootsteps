using System.Collections;
using UnityEngine;

/// <summary>
/// 까마귀 낙하 공격을 처리
/// 트리거 방식: 지정된 위치에서 시작 → 경고 → 지정된 위치로 낙하
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class RoadCrow : MonoBehaviour
{
    [Header("Warning Display Settings")]
    [Tooltip("경고 지속 시간 (초)")]
    public float warningDuration = 1f;
    
    [Tooltip("경고 반원 스프라이트")]
    public Sprite warningCircleSprite;
    
    [Tooltip("경고 아이콘 스프라이트 (느낌표 등)")]
    public Sprite warningIconSprite;
    
    [Tooltip("반원 크기")]
    public Vector2 circleScale = Vector2.one;
    
    [Tooltip("아이콘 크기")]
    public Vector2 iconScale = Vector2.one * 0.5f;
    
    [Tooltip("아이콘 위치 오프셋 (반원 기준)")]
    public Vector2 iconOffset = new Vector2(0f, 0.5f);
    
    [Tooltip("반원 Sorting Order")]
    public int circleSortingOrder = 5;
    
    [Tooltip("아이콘 Sorting Order")]
    public int iconSortingOrder = 6;

    [Header("Warning Animation")]
    [Tooltip("깜빡임 속도")]
    public float blinkSpeed = 5f;
    
    [Tooltip("최소 투명도")]
    [Range(0f, 1f)]
    public float minAlpha = 0.3f;
    
    [Tooltip("최대 투명도")]
    [Range(0f, 1f)]
    public float maxAlpha = 1f;
    
    [Tooltip("크기 변화 효과")]
    public bool scaleEffect = true;
    
    [Tooltip("크기 변화 최소값")]
    public float minScaleMultiplier = 0.9f;
    
    [Tooltip("크기 변화 최대값")]
    public float maxScaleMultiplier = 1.1f;

    [Header("Attack Settings")]
    [Tooltip("낙하 속도")]
    public float fallSpeed = 10f;
    
    [Tooltip("충돌 데미지")]
    public int damage = 1;
    
    [Tooltip("땅에 닿으면 자동 제거")]
    public bool destroyOnGround = true;
    
    [Tooltip("최대 생존 시간 (초)")]
    public float maxLifeTime = 10f;

    // 내부 변수
    private Rigidbody2D rb;
    private SpriteRenderer crowRenderer;
    private GameObject warningCircleObj;
    private GameObject warningIconObj;
    private SpriteRenderer circleRenderer;
    private SpriteRenderer iconRenderer;
    
    private bool isWaiting = true;
    private bool isWarningPhase = false;
    private bool isAttacking = false;
    private bool hasHitPlayer = false;
    private float warningTimer = 0f;
    
    // 트리거에서 설정하는 위치들
    private Vector2 crowStartPosition;      // 까마귀 시작 위치
    private Vector2 attackTargetPosition;   // 경고 및 낙하 목표 위치
    private bool isInitialized = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        crowRenderer = GetComponent<SpriteRenderer>();
        
        if (rb == null)
        {
            Debug.LogError("[RoadCrow] Rigidbody2D를 찾을 수 없습니다!");
        }
        
        if (crowRenderer == null)
        {
            Debug.LogError("[RoadCrow] SpriteRenderer를 찾을 수 없습니다!");
        }
        
        // 물리 설정
        if (rb != null)
        {
            rb.gravityScale = 0;
            rb.velocity = Vector2.zero;
            rb.isKinematic = true;
        }
        
        // 까마귀 비활성화 (초기화될 때까지)
        if (crowRenderer != null)
        {
            crowRenderer.enabled = false;
        }
        
        Debug.Log("[RoadCrow] Awake 완료");
    }

    void Start()
    {
        // 최대 생존 시간 후 자동 제거
        Destroy(gameObject, maxLifeTime);
        
        Debug.Log("[RoadCrow] Start 완료");
    }

    void Update()
    {
        // 초기화되지 않았으면 아무것도 안 함
        if (!isInitialized) return;

        if (isWaiting)
        {
            // 대기 중 - 아무것도 안 함
        }
        else if (isWarningPhase)
        {
            // 경고 단계
            UpdateWarningAnimation();
            
            warningTimer += Time.deltaTime;
            if (warningTimer >= warningDuration)
            {
                // 경고 종료, 낙하 시작
                StartAttack();
            }
        }
        else if (isAttacking)
        {
            // 낙하 - 목표 지점으로 직선 낙하
            Vector2 direction = (attackTargetPosition - (Vector2)transform.position).normalized;
            rb.velocity = direction * fallSpeed;
        }
    }

    /// <summary>
    /// 트리거에서 호출: 까마귀 초기화 및 시작
    /// </summary>
    public void Initialize(Vector2 startPos, Vector2 targetPos)
    {
        // 컴포넌트가 없으면 가져오기
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (crowRenderer == null) crowRenderer = GetComponent<SpriteRenderer>();
        
        crowStartPosition = startPos;
        attackTargetPosition = targetPos;
        
        // 까마귀를 시작 위치에 배치
        transform.position = crowStartPosition;
        
        // 까마귀 활성화
        if (crowRenderer != null)
        {
            crowRenderer.enabled = true;
        }
        else
        {
            Debug.LogError("[RoadCrow] SpriteRenderer를 찾을 수 없습니다!");
            return;
        }
        
        // 물리 설정
        if (rb != null)
        {
            rb.gravityScale = 0;
            rb.velocity = Vector2.zero;
            rb.isKinematic = true;
        }
        
        isInitialized = true;
        isWaiting = false;
        isWarningPhase = true;
        
        // 경고 표시 생성 (목표 위치에)
        CreateWarningDisplay();
        
        Debug.Log($"[RoadCrow] 초기화 완료 - 시작: {startPos}, 목표: {targetPos}");
    }

    /// <summary>
    /// 경고 표시 생성 - 목표 위치(attackTargetPosition)에 생성
    /// </summary>
    void CreateWarningDisplay()
    {
        // 반원 오브젝트 생성
        if (warningCircleSprite != null)
        {
            warningCircleObj = new GameObject("WarningCircle");
            warningCircleObj.transform.position = attackTargetPosition;
            warningCircleObj.transform.localScale = circleScale;
            
            circleRenderer = warningCircleObj.AddComponent<SpriteRenderer>();
            circleRenderer.sprite = warningCircleSprite;
            circleRenderer.sortingOrder = circleSortingOrder;
        }
        
        // 아이콘 오브젝트 생성
        if (warningIconSprite != null)
        {
            warningIconObj = new GameObject("WarningIcon");
            warningIconObj.transform.position = attackTargetPosition + iconOffset;
            warningIconObj.transform.localScale = iconScale;
            
            iconRenderer = warningIconObj.AddComponent<SpriteRenderer>();
            iconRenderer.sprite = warningIconSprite;
            iconRenderer.sortingOrder = iconSortingOrder;
        }
    }

    /// <summary>
    /// 경고 애니메이션 업데이트 (깜빡임 효과)
    /// </summary>
    void UpdateWarningAnimation()
    {
        float t = (Mathf.Sin(Time.time * blinkSpeed) + 1f) * 0.5f;
        
        if (circleRenderer != null)
        {
            Color color = circleRenderer.color;
            color.a = Mathf.Lerp(minAlpha, maxAlpha, t);
            circleRenderer.color = color;
            
            if (scaleEffect && warningCircleObj != null)
            {
                float scaleMultiplier = Mathf.Lerp(minScaleMultiplier, maxScaleMultiplier, t);
                warningCircleObj.transform.localScale = circleScale * scaleMultiplier;
            }
        }
        
        if (iconRenderer != null)
        {
            Color color = iconRenderer.color;
            color.a = Mathf.Lerp(minAlpha, maxAlpha, t);
            iconRenderer.color = color;
            
            if (scaleEffect && warningIconObj != null)
            {
                float scaleMultiplier = Mathf.Lerp(minScaleMultiplier, maxScaleMultiplier, t);
                warningIconObj.transform.localScale = iconScale * scaleMultiplier;
            }
        }
    }

    /// <summary>
    /// 낙하 공격 시작
    /// </summary>
    void StartAttack()
    {
        isWarningPhase = false;
        isAttacking = true;
        
        // 경고 표시 제거
        if (warningCircleObj != null) Destroy(warningCircleObj);
        if (warningIconObj != null) Destroy(warningIconObj);
        
        // 물리 활성화
        rb.isKinematic = false;
        rb.gravityScale = 0;
        
        Debug.Log($"[RoadCrow] 낙하 시작! 현재: {transform.position}, 목표: {attackTargetPosition}");
    }

    /// <summary>
    /// 플레이어 또는 지면과 충돌 시 처리
    /// </summary>
    void OnTriggerEnter2D(Collider2D collision)
    {
        // 공격 중이 아니면 충돌 처리 안 함
        if (!isAttacking) return;
        
        // 플레이어와 충돌
        if (collision.CompareTag("Player") && !hasHitPlayer)
        {
            hasHitPlayer = true;

            PlayerHp playerHp = collision.GetComponent<PlayerHp>();
            if (playerHp != null)
            {
                playerHp.TakeDamage(damage);
                Debug.Log("[RoadCrow] 플레이어와 충돌! 데미지 적용");
            }

            Destroy(gameObject);
        }
        // 지면과 충돌
        else if (destroyOnGround && (collision.CompareTag("Ground") || collision.CompareTag("wall")))
        {
            Debug.Log("[RoadCrow] 지면에 도착, 제거");
            Destroy(gameObject);
        }
    }

    void OnDrawGizmos()
    {
        if (isInitialized && Application.isPlaying)
        {
            // 시작 위치 (파란색)
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(crowStartPosition, 0.5f);
            
            // 목표 위치 (빨간색)
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackTargetPosition, 0.5f);
            
            // 경로 (시안색)
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(crowStartPosition, attackTargetPosition);
        }
    }
}