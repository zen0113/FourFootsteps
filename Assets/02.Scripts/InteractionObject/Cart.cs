using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cart : MonoBehaviour
{
    [Header("카트 설정")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private Transform goalTarget; // 목표 지점 Transform

    [Header("상태 확인")]
    [SerializeField] private bool isPlayerOnCart = false;
    [SerializeField] private bool isMoving = false;

    private Vector2 targetPosition;
    private Vector2 startPosition;
    private Vector3 lastCartPosition; // 카트의 이전 프레임 위치
    private Rigidbody2D rb;
    private Transform playerTransform; // 플레이어 Transform 저장
    private Rigidbody2D playerRigidbody; // 플레이어 Rigidbody 저장

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        startPosition = transform.position;
        lastCartPosition = transform.position;

        // goalTarget이 할당되었다면 목표 위치 설정
        if (goalTarget != null)
        {
            targetPosition = goalTarget.position;
        }
        else
        {
            Debug.LogWarning("Goal Target이 할당되지 않았습니다!");
        }

        // Rigidbody2D 설정 (물리 기반으로 부드럽게 이동)
        rb.gravityScale = 1f;
        rb.freezeRotation = true;

        // 카트가 밀리지 않도록 무거운 질량 설정
        rb.mass = 1000f; // 매우 무거운 질량
        rb.drag = 10f;    // 저항력 증가

        // 자식 오브젝트의 Trigger 이벤트를 감지하기 위해 설정
        SetupChildTriggers();
    }

    void SetupChildTriggers()
    {
        // 모든 자식 오브젝트에서 Trigger Collider를 찾아서 이벤트 연결
        Collider2D[] childColliders = GetComponentsInChildren<Collider2D>();

        foreach (Collider2D col in childColliders)
        {
            if (col.isTrigger && col.gameObject != gameObject)
            {
                // 자식 오브젝트에 PlayerDetector 컴포넌트 추가
                PlayerDetector detector = col.GetComponent<PlayerDetector>();
                if (detector == null)
                {
                    detector = col.gameObject.AddComponent<PlayerDetector>();
                }

                // 이벤트 연결
                detector.SetCart(this);
                Debug.Log($"PlayerDetector가 {col.name}에 설정되었습니다.");
            }
        }
    }

    void Update()
    {
        // 플레이어가 카트 위에 있고 아직 이동하지 않았다면 이동 시작
        if (isPlayerOnCart && !isMoving && goalTarget != null)
        {
            StartMoving();
        }

        // 이동 중일 때 목표 지점으로 이동
        if (isMoving)
        {
            MoveToTarget();
        }

        // 플레이어가 카트 위에 있다면 카트와 함께 이동
        if (isPlayerOnCart && playerTransform != null)
        {
            MovePlayerWithCart();

        }

        // 현재 프레임의 카트 위치 저장
        lastCartPosition = transform.position;
    }

    void StartMoving()
    {
        isMoving = true;

        // 이동 시작 시 질량을 가볍게 조정 (부드러운 이동을 위해)
        rb.mass = 1f;
        rb.drag = 0f;

        Debug.Log("카트가 이동을 시작합니다!");
    }

    void MoveToTarget()
    {
        // 현재 위치에서 목표 위치로의 방향 계산
        Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;

        // 목표 지점에 도달했는지 확인 (거리 체크)
        float distanceToTarget = Vector2.Distance(transform.position, targetPosition);

        if (distanceToTarget > 0.2f) // 도달 거리를 약간 늘림
        {
            // Rigidbody2D를 사용해 부드럽게 이동
            rb.velocity = new Vector2(direction.x * moveSpeed, rb.velocity.y);
        }
        else
        {
            // 목표 지점 도달 시 이동 정지
            StopMoving();
        }
    }

    void MovePlayerWithCart()
    {
        // 카트가 이동한 거리만큼 플레이어도 함께 이동
        Vector3 cartMovement = transform.position - lastCartPosition;

        if (cartMovement.magnitude > 0.001f) // 미세한 움직임 무시
        {
            if (playerRigidbody != null)
            {
                playerRigidbody.velocity = new Vector2(rb.velocity.x, playerRigidbody.velocity.y);
            }
        }
    }

    void StopMoving()
    {
        isMoving = false;
        rb.velocity = new Vector2(0, rb.velocity.y);

        // 목표 지점 도달 시 플레이어 해제
        ReleasePlayer();

        Debug.Log("카트가 목표 지점에 도달했습니다!");
    }

    // 플레이어를 카트에서 강제로 해제
    void ReleasePlayer()
    {
        if (isPlayerOnCart && playerTransform != null)
        {
            isPlayerOnCart = false;

            // 플레이어를 카트 옆쪽으로 살짝 이동 (겹치지 않도록)
            Vector3 releasePosition = playerTransform.position;
            releasePosition.x += 1.5f; // 카트 오른쪽으로 이동
            playerTransform.position = releasePosition;

            // 플레이어의 부모 관계 해제
            playerTransform.SetParent(null);

            // 플레이어 Rigidbody 속도 초기화
            if (playerRigidbody != null)
            {
                playerRigidbody.velocity = Vector2.zero;
            }

            playerTransform = null;
            playerRigidbody = null;

            // 카트를 다시 무겁게 만들어서 밀리지 않도록 설정
            rb.mass = 1000f;
            rb.drag = 10f;

            Debug.Log("플레이어가 카트에서 자동으로 해제되었습니다!");
        }
    }

    // 플레이어가 카트 위에 올라왔을 때 (자식 오브젝트에서 호출됨)
    public void OnPlayerEnterCart(Transform player)
    {
        isPlayerOnCart = true;
        playerTransform = player;
        playerRigidbody = player.GetComponent<Rigidbody2D>();

        // 플레이어를 카트의 자식으로 설정 (카트와 함께 이동)
        playerTransform.SetParent(transform);

        Debug.Log("플레이어가 카트에 올라탔습니다!");
    }

    // 플레이어가 카트에서 내려갔을 때 (자식 오브젝트에서 호출됨)
    public void OnPlayerExitCart(Transform player)
    {
        isPlayerOnCart = false;

        // 플레이어의 부모 관계 해제
        if (playerTransform != null)
        {
            playerTransform.SetParent(null);
            playerTransform = null;
            playerRigidbody = null;
        }

        Debug.Log("플레이어가 카트에서 내려갔습니다!");
    }

    // 카트 초기화 (테스트용)
    [ContextMenu("카트 초기화")]
    public void ResetCart()
    {
        transform.position = startPosition;
        lastCartPosition = startPosition;

        // 초기화 시 무거운 질량으로 복원
        rb.mass = 1000f;
        rb.drag = 10f;
        isMoving = false;
        isPlayerOnCart = false;
        rb.velocity = Vector2.zero;

        playerTransform = null;
        playerRigidbody = null;
    }
}

// 자식 오브젝트의 Trigger 이벤트를 Cart로 전달하는 헬퍼 클래스
public class PlayerDetector : MonoBehaviour
{
    private Cart parentCart;

    public void SetCart(Cart cart)
    {
        parentCart = cart;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && parentCart != null)
        {
            parentCart.OnPlayerEnterCart(other.transform);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") && parentCart != null)
        {
            parentCart.OnPlayerExitCart(other.transform);
        }
    }
}