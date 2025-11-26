using System.Collections;
using UnityEngine;

/// <summary>
/// 도로를 달리는 차량의 이동과 원근 효과를 처리
/// 멀리서 작고 흐릿하게 시작해서 가까워질수록 크고 선명해짐
/// 라인 각도에 맞춰 대각선 이동 가능
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
    private float laneX; // 고정된 차선 X 좌표 (각도 0도 기준)
    private float laneRotation = 0f; // 차선 각도 (도 단위)
    private Vector3 startPosition; // 시작 위치 저장
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
        
        startPosition = transform.position; // 시작 위치 저장
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
        // 라인 각도에 맞춰서 이동 (대각선 이동 가능)
        MoveAlongLane();
        
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
    /// 라인 각도에 맞춰 이동 (대각선 이동 가능)
    /// </summary>
    void MoveAlongLane()
    {
        // 라인 각도를 라디안으로 변환
        float radians = laneRotation * Mathf.Deg2Rad;
        
        // 차량이 이동할 방향 벡터
        // 0도 = 순수 오른쪽, -90도 = 순수 아래
        Vector3 movementDirection = new Vector3(
            Mathf.Sin(radians),     // X는 sin으로
            -Mathf.Cos(radians),    // Y는 -cos으로 (아래 방향)
            0f
        ).normalized;
        
        // 이동 적용
        transform.position += movementDirection * moveSpeed * Time.deltaTime;
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
    /// 차선 각도 설정 (외부에서 호출) - 도 단위
    /// 0도 = 순수 오른쪽
    /// -90도 = 순수 아래 (기본값)
    /// -45도 = 오른쪽 아래 대각선
    /// </summary>
    public void SetLaneRotation(float rotation)
    {
        laneRotation = rotation;
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
    /// 이동 거리 기반으로 진행률 계산
    /// </summary>
    void ApplyPerspectiveEffect()
    {
        // 시작 위치에서 현재 위치까지의 이동 거리 계산
        float distanceTraveled = Vector3.Distance(transform.position, startPosition);
        
        // 최대 이동 거리 (startY에서 fullVisibilityY까지 대각선 이동 거리)
        float radians = laneRotation * Mathf.Deg2Rad;
        float maxDistance = Mathf.Abs((startY - fullVisibilityY) / Mathf.Cos(radians));
        
        // 진행률 계산 (0 = 시작, 1 = 끝)
        float progress = Mathf.Clamp01(distanceTraveled / maxDistance);
        
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
    /// 씬 뷰에서 차량 이동 경로 시각화 (각도 반영)
    /// </summary>
    void OnDrawGizmos()
    {
        // 차선 이동 경로 (각도 반영)
        float radians = laneRotation * Mathf.Deg2Rad;
        Vector3 direction = new Vector3(
            Mathf.Sin(radians),
            -Mathf.Cos(radians),
            0f
        ).normalized;
        
        Gizmos.color = Color.red;
        Vector3 startPos = new Vector3(transform.position.x, startY, 0);
        Vector3 endPos = startPos + direction * (startY - endY) * 2f;
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