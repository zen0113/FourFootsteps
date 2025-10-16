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
    public float maxScale = 1f;
    
    [Tooltip("최소 투명도 (멀리 있을 때)")]
    [Range(0f, 1f)]
    public float minAlpha = 0.2f;
    
    [Tooltip("최대 투명도 (가까이 있을 때)")]
    [Range(0f, 1f)]
    public float maxAlpha = 1f;
    
    [Tooltip("완전히 선명해지는 Y 위치 (이 위치 이후로는 알파값 1 유지)")]
    public float fullVisibilityY = 2f;

    [Header("Collision Settings")]
    [Tooltip("충돌 데미지")]
    public int damage = 1;

    // 내부 변수
    private float laneX; // 고정된 차선 X 좌표
    private SpriteRenderer spriteRenderer;
    private bool hasHitPlayer = false; // 플레이어와 충돌했는지 여부

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // 차량 스프라이트 랜덤 선택
        if (vehicleSprites != null && vehicleSprites.Length > 0)
        {
            int randomIndex = Random.Range(0, vehicleSprites.Length);
            spriteRenderer.sprite = vehicleSprites[randomIndex];
            Debug.Log($"[RoadVehicle] 차량 스프라이트 선택: {randomIndex + 1}/{vehicleSprites.Length}");
        }
        else
        {
            Debug.LogWarning("[RoadVehicle] 차량 스프라이트가 설정되지 않았습니다!");
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
    }

    void Update()
    {
        // Y축 이동 (차선 X는 고정)
        transform.position += Vector3.down * moveSpeed * Time.deltaTime;
        
        // 차선 X 좌표 유지
        Vector3 pos = transform.position;
        pos.x = laneX;
        transform.position = pos;
        
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
    /// 원근 효과 적용 (크기와 투명도 조절)
    /// </summary>
    void ApplyPerspectiveEffect()
    {
        // 진행률 계산 (0 = 시작, 1 = 끝)
        float progress = Mathf.InverseLerp(startY, endY, transform.position.y);
        
        // 크기 조절
        float scale = Mathf.Lerp(minScale, maxScale, progress);
        transform.localScale = Vector3.one * scale;
        
        // 투명도 조절 (fullVisibilityY 이후로는 완전히 불투명)
        float alpha;
        if (transform.position.y <= fullVisibilityY)
        {
            // fullVisibilityY 이하에서는 완전히 불투명 (알파값 1)
            alpha = maxAlpha;
        }
        else
        {
            // fullVisibilityY 위에서는 startY ~ fullVisibilityY 구간에서 투명도 변화
            float alphaProgress = Mathf.InverseLerp(startY, fullVisibilityY, transform.position.y);
            alpha = Mathf.Lerp(minAlpha, maxAlpha, alphaProgress);
        }
        
        Color color = spriteRenderer.color;
        color.a = alpha;
        spriteRenderer.color = color;
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

            // 차량 제거
            Destroy(gameObject);
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
    }
}