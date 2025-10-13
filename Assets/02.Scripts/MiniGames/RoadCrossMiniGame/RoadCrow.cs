using System.Collections;
using UnityEngine;

/// <summary>
/// 까마귀 낙하 공격을 처리
/// 경고 표시(반원 + 아이콘) → 날아오기 → 호버링 → 낙하 공격 순서로 진행
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

    [Header("Flight Animation")]
    [Tooltip("까마귀 대기 위치 (경고 위치 기준 오프셋)")]
    public Vector2 waitOffset = new Vector2(8f, 6f);
    
    [Tooltip("낙하 시작 시 수평 속도")]
    public float horizontalSpeed = 5f;
    
    [Tooltip("낙하 가속도 (중력처럼)")]
    public float fallAcceleration = 15f;

    [Header("Attack Settings")]
    [Tooltip("낙하 속도")]
    public float fallSpeed = 8f;
    
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
    
    private bool isWarningPhase = true;
    private bool isAttacking = false;
    private bool hasHitPlayer = false;
    private float warningTimer = 0f;
    private Vector2 targetGroundPosition;
    private Vector2 waitPosition;
    private float currentFallSpeed = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        crowRenderer = GetComponent<SpriteRenderer>();
        
        // 물리 설정
        rb.gravityScale = 0;
        rb.velocity = Vector2.zero;
        rb.isKinematic = true;
        
        // 경고가 표시될 위치 저장 (현재 생성된 위치 = 도로 위치)
        targetGroundPosition = transform.position;
        
        // 까마귀 대기 위치 계산
        waitPosition = targetGroundPosition + waitOffset;
        
        // 까마귀를 대기 위치에 바로 배치
        transform.position = waitPosition;
        
        // 까마귀 활성화 (처음부터 보이게)
        crowRenderer.enabled = true;
        
        Debug.Log($"[RoadCrow] 까마귀 대기 중 - 대기 위치: {waitPosition}, 목표: {targetGroundPosition}");
        
        // 경고 표시 생성 (도로 위치에 생성)
        CreateWarningDisplay();
        
        // 최대 생존 시간 후 자동 제거
        Destroy(gameObject, maxLifeTime);
    }

    void Update()
    {
        if (isWarningPhase)
        {
            // 경고 단계 (까마귀는 대기 위치에서 대기)
            UpdateWarningAnimation();
            
            warningTimer += Time.deltaTime;
            if (warningTimer >= warningDuration)
            {
                // 경고 종료, 낙하 공격 시작
                StartAttack();
            }
        }
        else if (isAttacking)
        {
            // 포물선 낙하 (수평 이동 + 가속 낙하)
            currentFallSpeed += fallAcceleration * Time.deltaTime;
            Vector2 velocity = new Vector2(-horizontalSpeed, -currentFallSpeed);
            rb.velocity = velocity;
        }
    }

    /// <summary>
    /// 경고 표시 생성 (반원 + 아이콘) - 도로 위치에 생성
    /// </summary>
    void CreateWarningDisplay()
    {
        // 반원 오브젝트 생성 (도로 위치에)
        if (warningCircleSprite != null)
        {
            warningCircleObj = new GameObject("WarningCircle");
            warningCircleObj.transform.position = targetGroundPosition;
            warningCircleObj.transform.localScale = circleScale;
            
            circleRenderer = warningCircleObj.AddComponent<SpriteRenderer>();
            circleRenderer.sprite = warningCircleSprite;
            circleRenderer.sortingOrder = circleSortingOrder;
        }
        
        // 아이콘 오브젝트 생성 (도로 위치에)
        if (warningIconSprite != null)
        {
            warningIconObj = new GameObject("WarningIcon");
            warningIconObj.transform.position = targetGroundPosition + iconOffset;
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
        float t = (Mathf.Sin(Time.time * blinkSpeed) + 1f) * 0.5f; // 0~1 사이 값
        
        // 반원 애니메이션
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
        
        // 아이콘 애니메이션 (반원과 동기화)
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
    /// 포물선 낙하 공격 시작
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
        rb.gravityScale = 0; // 직접 속도 제어
        
        // 초기 낙하 속도 설정
        currentFallSpeed = 0f;
        
        Debug.Log($"[RoadCrow] 포물선 낙하 시작! 위치: {transform.position}");
    }

    /// <summary>
    /// 플레이어 또는 지면과 충돌 시 처리
    /// </summary>
    void OnTriggerEnter2D(Collider2D collision)
    {
        // 경고 단계에서는 충돌 처리 안 함
        if (isWarningPhase) return;
        
        // 플레이어와 충돌
        if (collision.CompareTag("Player") && !hasHitPlayer)
        {
            hasHitPlayer = true;

            // 플레이어에게 데미지
            PlayerHp playerHp = collision.GetComponent<PlayerHp>();
            if (playerHp != null)
            {
                playerHp.TakeDamage(damage);
                Debug.Log("[RoadCrow] 플레이어와 충돌! 데미지 적용");
            }

            // 까마귀 제거
            Destroy(gameObject);
        }
        // 지면과 충돌
        else if (destroyOnGround && (collision.CompareTag("Ground") || collision.CompareTag("wall")))
        {
            Debug.Log("[RoadCrow] 지면에 도착, 제거");
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 씬 뷰에서 경고 위치와 까마귀 위치 시각화
    /// </summary>
    void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            if (isWarningPhase)
            {
                // 경고 위치 표시 (노란색 원)
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(targetGroundPosition, 0.5f);
                
                // 까마귀 대기 위치 표시 (빨간색)
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(waitPosition, 0.4f);
                
                // 대기 위치에서 경고 위치로 화살표
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(waitPosition, targetGroundPosition);
            }
            else if (isAttacking)
            {
                // 낙하 경로 표시 (마젠타색)
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(transform.position, transform.position + new Vector3(-2f, -5f, 0f));
            }
        }
    }
}