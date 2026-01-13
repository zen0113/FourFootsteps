using System.Collections;
using UnityEngine;

/// <summary>
/// 까마귀 낙하 공격을 처리
/// 트리거 방식: 지정된 위치에서 시작 → 경고 → 지정된 위치로 낙하
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
public class RoadCrow : MonoBehaviour
{
    [Header("Warning Display Settings")]
    [Tooltip("경고 지속 시간 (초)")]
    public float warningDuration = 1f;

    [Header("Warning Prefabs (Recommended)")]
    [Tooltip("경고 반원 프리팹 (있으면 이 프리팹을 사용합니다). 프리팹 루트에 SpriteRenderer가 있어야 합니다.")]
    public GameObject warningCirclePrefab;

    [Tooltip("경고 아이콘 프리팹 (있으면 이 프리팹을 사용합니다). 프리팹 루트에 SpriteRenderer가 있어야 합니다. SpriteGlowEffect를 여기에 붙여두면 조절이 편합니다.")]
    public GameObject warningIconPrefab;

    [Tooltip("아이콘 프리팹(또는 생성된 아이콘)에 SpriteGlowEffect가 있으면 자동으로 활성화합니다.")]
    public bool enableWarningIconGlow = true;
    
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
    
    [Header("Escape Settings")]
    [Tooltip("탈출 속도 (왼쪽으로 날아가는 속도)")]
    public float escapeSpeed = 8f;
    
    [Tooltip("탈출 시 위로 올라가는 속도")]
    public float escapeUpSpeed = 5f;
    
    [Tooltip("탈출 시 투명도")]
    [Range(0f, 1f)]
    public float escapeAlpha = 0.3f;
    
    [Tooltip("투명 전환 속도")]
    public float fadeSpeed = 3f;
    
    [Tooltip("탈출 시간 (이 시간 후 자동 제거)")]
    public float escapeLifetime = 5f;

    // 내부 변수
    private Rigidbody2D rb;
    private SpriteRenderer crowRenderer;
    private Animator animator;
    private Collider2D crowCollider;
    private GameObject warningCircleObj;
    private GameObject warningIconObj;
    private SpriteRenderer circleRenderer;
    private SpriteRenderer iconRenderer;
    
    private bool isWaiting = true;
    private bool isWarningPhase = false;
    private bool isAttacking = false;
    private bool isEscaping = false;
    private bool hasReachedStartHeight = false; // 시작 높이에 도달했는지
    private bool hasHitPlayer = false;
    private float warningTimer = 0f;
    private float targetAlpha = 1f;
    
    // 트리거에서 설정하는 위치들
    private Vector2 crowStartPosition;      // 까마귀 시작 위치
    private Vector2 attackTargetPosition;   // 경고 및 낙하 목표 위치
    private bool isInitialized = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        crowRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        crowCollider = GetComponent<Collider2D>();
        
        if (rb == null)
        {
            Debug.LogError("[RoadCrow] Rigidbody2D를 찾을 수 없습니다!");
        }
        
        if (crowRenderer == null)
        {
            Debug.LogError("[RoadCrow] SpriteRenderer를 찾을 수 없습니다!");
        }
        
        if (animator == null)
        {
            Debug.LogWarning("[RoadCrow] Animator를 찾을 수 없습니다! 애니메이션이 재생되지 않습니다.");
        }
        
        if (crowCollider == null)
        {
            Debug.LogWarning("[RoadCrow] Collider2D를 찾을 수 없습니다!");
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
            // 대기 중 - 애니메이션 계속 재생
        }
        else if (isWarningPhase)
        {
            // 경고 단계 - 애니메이션 계속 재생
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
            // 낙하 - 애니메이션 계속 재생
            // 목표 지점으로 직선 낙하
            Vector2 direction = (attackTargetPosition - (Vector2)transform.position).normalized;
            
            // Transform으로 직접 이동 (Kinematic이므로 velocity 대신)
            transform.position += (Vector3)(direction * fallSpeed * Time.deltaTime);
            
            // 목표 지점에 가까워졌는지 체크 (충돌이 안 될 경우 대비)
            float distanceToTarget = Vector2.Distance(transform.position, attackTargetPosition);
            if (distanceToTarget < 0.5f) // 0.5 유닛 이내로 접근하면
            {
                Debug.Log($"[RoadCrow] 목표 지점 근접! 거리: {distanceToTarget}");
                StartEscape(false); // 불투명 상태로 탈출
            }
        }
        else if (isEscaping)
        {
            // 탈출 중 - 시작 높이까지 올라간 후 왼쪽으로 계속 날아감
            
            // 1단계: 시작 높이에 도달할 때까지 왼쪽 위로 이동
            if (!hasReachedStartHeight)
            {
                // 현재 높이가 시작 높이보다 낮으면
                if (transform.position.y < crowStartPosition.y - 0.2f)
                {
                    // 왼쪽 위로 이동 (대각선)
                    Vector2 escapeDirection = new Vector2(-1f, 1f).normalized;
                    Vector3 movement = new Vector3(
                        escapeDirection.x * escapeSpeed * Time.deltaTime,
                        escapeDirection.y * escapeUpSpeed * Time.deltaTime,
                        0
                    );
                    transform.position += movement;
                }
                else
                {
                    // 시작 높이에 도달
                    hasReachedStartHeight = true;
                    Debug.Log("[RoadCrow] 시작 높이 도달! 이제 왼쪽으로만 이동");
                }
            }
            // 2단계: 시작 높이 도달 후 왼쪽으로만 이동
            else
            {
                // 왼쪽으로만 이동 (Y축 고정)
                Vector3 movement = new Vector3(-escapeSpeed * Time.deltaTime, 0, 0);
                transform.position += movement;
            }
            
            // 투명도 부드럽게 변경
            if (crowRenderer != null)
            {
                Color color = crowRenderer.color;
                color.a = Mathf.Lerp(color.a, targetAlpha, Time.deltaTime * fadeSpeed);
                crowRenderer.color = color;
            }
        }
    }

    /// <summary>
    /// 애니메이션 이후 실행 - flipX 강제 적용
    /// </summary>
    void LateUpdate()
    {
        if (crowRenderer == null) return;
        
        // 공격 중: 이동 방향에 따라 flipX 설정
        if (isAttacking)
        {
            Vector2 direction = (attackTargetPosition - (Vector2)transform.position).normalized;
            if (direction.x != 0)
            {
                // 왼쪽(음수)으로 가면 false, 오른쪽(양수)으로 가면 true
                crowRenderer.flipX = direction.x > 0;
            }
        }
        // 탈출 중: 왼쪽으로 날아가므로 false
        else if (isEscaping)
        {
            crowRenderer.flipX = false; // 왼쪽 = false
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
        if (animator == null) animator = GetComponent<Animator>();
        
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
        // 기존 경고 표시가 남아있으면 정리
        DestroyWarningDisplay();

        // 반원 오브젝트 생성
        if (warningCirclePrefab != null)
        {
            warningCircleObj = Instantiate(warningCirclePrefab, attackTargetPosition, Quaternion.identity);
            warningCircleObj.name = "WarningCircle";
            warningCircleObj.transform.localScale = circleScale;

            circleRenderer = warningCircleObj.GetComponent<SpriteRenderer>();
            if (circleRenderer == null)
            {
                Debug.LogWarning("[RoadCrow] warningCirclePrefab에 SpriteRenderer가 없습니다. 경고 반원 표시가 정상 동작하지 않을 수 있습니다.");
            }
            else
            {
                if (warningCircleSprite != null) circleRenderer.sprite = warningCircleSprite; // 선택적 오버라이드
                circleRenderer.sortingOrder = circleSortingOrder;
            }
        }
        else if (warningCircleSprite != null)
        {
            warningCircleObj = new GameObject("WarningCircle");
            warningCircleObj.transform.position = attackTargetPosition;
            warningCircleObj.transform.localScale = circleScale;

            circleRenderer = warningCircleObj.AddComponent<SpriteRenderer>();
            circleRenderer.sprite = warningCircleSprite;
            circleRenderer.sortingOrder = circleSortingOrder;
        }
        
        // 아이콘 오브젝트 생성
        if (warningIconPrefab != null)
        {
            warningIconObj = Instantiate(warningIconPrefab, (Vector3)(attackTargetPosition + iconOffset), Quaternion.identity);
            warningIconObj.name = "WarningIcon";
            warningIconObj.transform.localScale = iconScale;

            iconRenderer = warningIconObj.GetComponent<SpriteRenderer>();
            if (iconRenderer == null)
            {
                Debug.LogWarning("[RoadCrow] warningIconPrefab에 SpriteRenderer가 없습니다. 경고 아이콘 표시가 정상 동작하지 않을 수 있습니다.");
            }
            else
            {
                if (warningIconSprite != null) iconRenderer.sprite = warningIconSprite; // 선택적 오버라이드
                iconRenderer.sortingOrder = iconSortingOrder;
            }

            // SpriteGlowEffect가 있으면 활성화(조절은 프리팹에서)
            if (enableWarningIconGlow)
            {
                var glow = warningIconObj.GetComponentInChildren<SpriteGlow.SpriteGlowEffect>(true);
                if (glow != null) glow.enabled = true;
            }
        }
        else if (warningIconSprite != null)
        {
            warningIconObj = new GameObject("WarningIcon");
            warningIconObj.transform.position = attackTargetPosition + iconOffset;
            warningIconObj.transform.localScale = iconScale;

            iconRenderer = warningIconObj.AddComponent<SpriteRenderer>();
            iconRenderer.sprite = warningIconSprite;
            iconRenderer.sortingOrder = iconSortingOrder;

            // 런타임 생성이라도, 혹시 SpriteGlowEffect를 붙여둔 경우 활성화
            if (enableWarningIconGlow)
            {
                var glow = warningIconObj.GetComponentInChildren<SpriteGlow.SpriteGlowEffect>(true);
                if (glow != null) glow.enabled = true;
            }
        }
    }

    void DestroyWarningDisplay()
    {
        if (warningCircleObj != null)
        {
            Destroy(warningCircleObj);
            warningCircleObj = null;
            circleRenderer = null;
        }
        if (warningIconObj != null)
        {
            Destroy(warningIconObj);
            warningIconObj = null;
            iconRenderer = null;
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
        Debug.Log("[RoadCrow] StartAttack 호출됨!");
        
        isWarningPhase = false;
        isAttacking = true;
        
        Debug.Log($"[RoadCrow] isAttacking = {isAttacking}");
        
        // 경고 표시 제거
        DestroyWarningDisplay();
        
        // Kinematic 유지 (애니메이션을 위해)
        rb.isKinematic = true;
        rb.gravityScale = 0;
        rb.velocity = Vector2.zero;
        
        Debug.Log($"[RoadCrow] 낙하 시작! 현재: {transform.position}, 목표: {attackTargetPosition}");
    }

    /// <summary>
    /// 탈출 모드 시작 (왼쪽 위로 날아간 후 왼쪽으로 계속 이동)
    /// </summary>
    /// <param name="makeTransparent">반투명하게 만들지 여부</param>
    void StartEscape(bool makeTransparent = true)
    {
        Debug.Log($"[RoadCrow] StartEscape 호출됨! 반투명: {makeTransparent}, 현재 위치: {transform.position}");
        
        isWaiting = false;
        isWarningPhase = false;
        isAttacking = false;
        isEscaping = true;
        hasReachedStartHeight = false; // 초기화
        
        Debug.Log($"[RoadCrow] isEscaping = {isEscaping}");
        
        // 경고 표시 제거 (혹시 남아있을 경우)
        DestroyWarningDisplay();
        
        // 콜라이더 비활성화 (더 이상 충돌 안 함)
        if (crowCollider != null)
        {
            crowCollider.enabled = false;
            Debug.Log("[RoadCrow] 콜라이더 비활성화");
        }
        
        // 애니메이터는 활성화 유지 (탈출 중에도 애니메이션 계속 재생)
        // flipX는 LateUpdate에서 제어하므로 문제 없음
        if (animator != null)
        {
            animator.enabled = true;
            Debug.Log("[RoadCrow] 애니메이터 활성화 - 탈출 중 애니메이션 계속 재생");
        }
        
        // 목표 투명도 설정
        if (makeTransparent)
        {
            targetAlpha = escapeAlpha; // 반투명
            Debug.Log($"[RoadCrow] 반투명 모드 - 목표 알파: {escapeAlpha}");
        }
        else
        {
            targetAlpha = 1f; // 불투명 유지
            Debug.Log("[RoadCrow] 불투명 모드 - 목표 알파: 1.0");
        }
        
        // 물리 설정 (Kinematic 유지)
        rb.isKinematic = true;
        rb.gravityScale = 0;
        rb.velocity = Vector2.zero;
        
        Debug.Log($"[RoadCrow] Rigidbody 설정 - isKinematic: {rb.isKinematic}");
        
        // 까마귀 방향 설정 (왼쪽으로 날아가므로 flipX = false)
        if (crowRenderer != null)
        {
            crowRenderer.flipX = false;
            Debug.Log($"[RoadCrow] FlipX 설정: {crowRenderer.flipX} (왼쪽)");
        }
        
        // 일정 시간 후 제거
        Destroy(gameObject, escapeLifetime);
        
        Debug.Log($"[RoadCrow] 탈출 시작 완료! 시작 높이: {crowStartPosition.y}, {escapeLifetime}초 후 제거");
    }

    /// <summary>
    /// 플레이어 또는 지면과 충돌 시 처리
    /// </summary>
    void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log($"[RoadCrow] 충돌 감지! Tag: {collision.tag}, isAttacking: {isAttacking}, isEscaping: {isEscaping}");
        
        // 공격 중이 아니거나 이미 탈출 중이면 충돌 처리 안 함
        if (!isAttacking || isEscaping)
        {
            Debug.Log("[RoadCrow] 충돌 무시됨 (공격 중이 아니거나 이미 탈출 중)");
            return;
        }
        
        // 플레이어와 충돌
        if (collision.CompareTag("Player") && !hasHitPlayer)
        {
            Debug.Log("[RoadCrow] 플레이어와 충돌!");
            hasHitPlayer = true;

            PlayerHp playerHp = collision.GetComponent<PlayerHp>();
            if (playerHp != null)
            {
                playerHp.TakeDamage(damage);
                Debug.Log("[RoadCrow] 플레이어에게 데미지 적용");
            }

            // 탈출 모드로 전환 (반투명)
            StartEscape(true);
        }
        // 지면과 충돌 (목표 지점 도달)
        else if (collision.CompareTag("Ground") || collision.CompareTag("wall"))
        {
            Debug.Log($"[RoadCrow] {collision.tag}와 충돌! 목표 지점 도착");
            
            // 탈출 모드로 전환 (불투명)
            StartEscape(false);
        }
        else
        {
            Debug.Log($"[RoadCrow] 알 수 없는 충돌: {collision.tag}");
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