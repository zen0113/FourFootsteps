using System.Collections;
using UnityEngine;

/// <summary>
/// 도로를 달리는 차량의 이동과 원근 효과를 처리
/// 멀리서 작고 흐릿하게 시작해서 가까워질수록 크고 선명해짐
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class RoadVehicle : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("차량 이동 속도")]
    public float moveSpeed = 2f;
    
    [Tooltip("시작 Y 위치")]
    public float startY = 10f;
    
    [Tooltip("종료 Y 위치 (이 지점을 지나면 파괴)")]
    public float endY = -10f;

    [Header("Visual Effect Settings")]
    [Tooltip("차량 스프라이트 목록 (랜덤 선택)")]
    public Sprite[] vehicleSprites;
    
    [Tooltip("최소 크기 (멀리 있을 때)")]
    public float minScale = 0.3f;
    
    [Tooltip("최대 크기 (가까이 있을 때)")]
    public float maxScale = 1.5f;
    
    [Tooltip("최소 투명도 (멀리 있을 때)")]
    [Range(0f, 1f)]
    public float minAlpha = 0.2f;
    
    [Tooltip("최대 투명도 (가까이 있을 때)")]
    [Range(0f, 1f)]
    public float maxAlpha = 1f;
    
    [Tooltip("완전히 선명해지는 Y 위치 (이 위치 이후로는 알파값 1 유지)")]
    public float fullVisibilityY = 2f;

    [Header("Collision Settings")]
    [Tooltip("충돌을 활성화할 Y 위치 (이 지점부터 충돌 가능)")]
    public float collisionEnableY = 5f;
    
    [Header("Headlight Settings")]
    [Tooltip("헤드라이트 오브젝트 (자식 오브젝트)")]
    public GameObject headlights;
    
    [Tooltip("헤드라이트를 활성화할 Y 위치")]
    public float headlightEnableY = 7f;
    
    [Header("Damage Settings")]
    [Tooltip("충돌 데미지")]
    public int damage = 1;
    
    [Tooltip("충돌 후 투명도 (0~1, 완전 투명=0)")]
    [Range(0f, 1f)]
    public float ghostAlpha = 0.15f;
    
    [Tooltip("고스트 모드로 전환되는 속도")]
    public float ghostTransitionSpeed = 2f;
    
    [Tooltip("충돌 후 Sorting Order")]
    public int ghostSortingOrder = 3;

    // 내부 변수
    private float laneX; // 고정된 차선 X 좌표
    private SpriteRenderer spriteRenderer;
    private bool hasHitPlayer = false;
    private Collider2D vehicleCollider;
    private bool collisionEnabled = false;
    private bool headlightEnabled = false;
    private float targetAlpha; // 목표 투명도
    private bool isGhostMode = false; // 고스트 모드 (충돌 후)
    private int originalSortingOrder; // 원래 Sorting Order 저장

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        vehicleCollider = GetComponent<Collider2D>();
        
        // 원래 Sorting Order 저장
        originalSortingOrder = spriteRenderer.sortingOrder;
        
        // 차량 스프라이트 랜덤 선택
        if (vehicleSprites != null && vehicleSprites.Length > 0)
        {
            int randomIndex = Random.Range(0, vehicleSprites.Length);
            spriteRenderer.sprite = vehicleSprites[randomIndex];
            //Debug.Log($"[RoadVehicle] 차량 스프라이트 선택: {randomIndex + 1}/{vehicleSprites.Length}");
        }
        else
        {
            //Debug.LogWarning("[RoadVehicle] 차량 스프라이트가 설정되지 않았습니다!");
        }
        
        // 시작 위치 설정
        Vector3 pos = transform.position;
        pos.y = startY;
        transform.position = pos;
        
        laneX = pos.x; // 현재 X 좌표를 차선으로 설정
        
        // 초기 색상 설정 (투명도만 변경, RGB는 유지)
        Color initialColor = spriteRenderer.color;
        initialColor.a = minAlpha;
        spriteRenderer.color = initialColor;
        targetAlpha = minAlpha;
        
        // 헤드라이트 초기 비활성화
        if (headlights != null)
        {
            headlights.SetActive(false);
        }
        
        // 콜라이더 초기 비활성화
        if (vehicleCollider != null)
        {
            vehicleCollider.enabled = false;
        }
    }

    void Update()
    {
        // Y축 이동 (차선 X는 고정)
        transform.position += Vector3.down * moveSpeed * Time.deltaTime;
        
        // 차선 X 좌표 유지
        Vector3 pos = transform.position;
        pos.x = laneX;
        transform.position = pos;
        
        // 헤드라이트 활성화 체크
        if (!headlightEnabled && transform.position.y <= headlightEnableY)
        {
            EnableHeadlight();
        }
        
        // 충돌 활성화 체크
        if (!collisionEnabled && transform.position.y <= collisionEnableY)
        {
            EnableCollision();
        }
        
        // 원근 효과 적용
        ApplyPerspectiveEffect();
        
        // 화면 밖으로 나가면 제거
        if (transform.position.y < endY)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 차선 X 좌표 설정 (외부에서 호출)
    /// </summary>
    public void SetLaneX(float x)
    {
        laneX = x;
        Vector3 pos = transform.position;
        pos.x = laneX;
        transform.position = pos;
    }
    
    /// <summary>
    /// 헤드라이트 활성화
    /// </summary>
    void EnableHeadlight()
    {
        headlightEnabled = true;
        
        // 헤드라이트 활성화
        if (headlights != null)
        {
            headlights.SetActive(true);
            //Debug.Log("[RoadVehicle] 헤드라이트 켜짐!");
        }
    }
    
    /// <summary>
    /// 충돌 활성화
    /// </summary>
    void EnableCollision()
    {
        collisionEnabled = true;
        
        // 콜라이더 활성화
        if (vehicleCollider != null)
        {
            vehicleCollider.enabled = true;
            //Debug.Log("[RoadVehicle] 충돌 활성화!");
        }
    }

    /// <summary>
    /// 원근 효과 적용 (크기와 투명도 조절)
    /// </summary>
    void ApplyPerspectiveEffect()
    {
        // 진행률 계산 (0 = 시작, 1 = 끝)
        float progress = Mathf.InverseLerp(startY, endY, transform.position.y);
        
        // 크기 조절
        float scale = Mathf.Lerp(minScale, maxScale, progress);
        transform.localScale = Vector3.one * scale;
        
        // 투명도 조절
        if (!isGhostMode)
        {
            // 일반 모드: 원근감에 따른 투명도
            if (transform.position.y <= fullVisibilityY)
            {
                targetAlpha = maxAlpha;
            }
            else
            {
                float alphaProgress = Mathf.InverseLerp(startY, fullVisibilityY, transform.position.y);
                targetAlpha = Mathf.Lerp(minAlpha, maxAlpha, alphaProgress);
            }
        }
        else
        {
            // 고스트 모드: 고정된 반투명 상태
            targetAlpha = ghostAlpha;
        }
        
        // 부드럽게 투명도 전환
        Color color = spriteRenderer.color;
        color.a = Mathf.Lerp(color.a, targetAlpha, Time.deltaTime * ghostTransitionSpeed);
        spriteRenderer.color = color;
    }
    
    /// <summary>
    /// 고스트 모드 활성화 (충돌 후)
    /// </summary>
    void EnableGhostMode()
    {
        isGhostMode = true;
        
        // Sorting Order 변경 (플레이어 뒤로)
        spriteRenderer.sortingOrder = ghostSortingOrder;
        
        // 콜라이더 비활성화 (더 이상 충돌 안 함)
        if (vehicleCollider != null)
        {
            vehicleCollider.enabled = false;
        }
        
        // 헤드라이트 비활성화 (선택사항)
        if (headlights != null)
        {
            headlights.SetActive(false);
        }
        
        Debug.Log($"[RoadVehicle] 고스트 모드 활성화! 플레이어를 통과합니다. (Order: {ghostSortingOrder})");
    }

    /// <summary>
    /// 플레이어와 충돌 시 처리
    /// </summary>
    void OnTriggerEnter2D(Collider2D collision)
    {
        // 이미 충돌했다면 무시
        if (hasHitPlayer) return;

        if (collision.CompareTag("Player"))
        {
            hasHitPlayer = true;

            // 플레이어에게 데미지
            PlayerHp playerHp = collision.GetComponent<PlayerHp>();
            if (playerHp != null)
            {
                playerHp.TakeDamage(damage);
                Debug.Log("[RoadVehicle] 플레이어와 충돌! 데미지 적용");
            }

            // 차량을 고스트 모드로 전환 (사라지지 않고 반투명하게)
            EnableGhostMode();
        }
    }

    /// <summary>
    /// 씬 뷰에서 차량 이동 경로 시각화
    /// </summary>
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Vector3 startPos = new Vector3(transform.position.x, startY, 0);
        Vector3 endPos = new Vector3(transform.position.x, endY, 0);
        Gizmos.DrawLine(startPos, endPos);
        
        // 헤드라이트 활성화 지점 표시
        Gizmos.color = Color.cyan;
        Vector3 headlightPos = new Vector3(transform.position.x, headlightEnableY, 0);
        Gizmos.DrawWireSphere(headlightPos, 0.5f);
        
        // 충돌 활성화 지점 표시
        Gizmos.color = Color.yellow;
        Vector3 collisionPos = new Vector3(transform.position.x, collisionEnableY, 0);
        Gizmos.DrawWireSphere(collisionPos, 0.5f);
    }
}