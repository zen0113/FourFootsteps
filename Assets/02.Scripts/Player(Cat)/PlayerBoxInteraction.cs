using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBoxInteraction : MonoBehaviour
{
    [Header("박스 상호작용 설정")]
    [SerializeField] private float pushPullSpeed = 3f;        // 밀고 당기는 속도
    [SerializeField] private float interactionRange = 1.5f;   // 상호작용 범위
    [SerializeField] private float touchDistance = 0.3f;      // 접촉 판정 거리
    [SerializeField] private LayerMask boxLayer;             // 박스 레이어 마스크
    
    private GameObject currentBox;                           // 현재 상호작용 중인 박스
    private Rigidbody2D boxRb;                              // 박스의 Rigidbody2D
    private BoxCollider2D playerCollider;                   // 플레이어 충돌체
    private BoxCollider2D boxCollider;                      // 박스 충돌체
    
    private bool isEKeyPressed = false;                     // E키 눌림 상태
    private bool isInteracting = false;                     // 실제 상호작용 중
    private bool isBoxOnRight;                              // 박스가 플레이어 오른쪽에 있는지
    private bool isPushing = false;                         // 밀기 중인지
    private bool isPulling = false;                         // 당기기 중인지
    
    // public 프로퍼티
    public bool IsInteracting => isInteracting;
    public GameObject CurrentBox => currentBox;
    public bool IsPushing => isPushing;
    public bool IsPulling => isPulling;

    private void Start()
    {
        playerCollider = GetComponent<BoxCollider2D>();
    }

    private void Update()
    {
        // E키 입력 처리
        HandleEKeyInput();
        
        // 상호작용 중일 때만 처리
        if (isEKeyPressed)
        {
            // 박스 찾기 및 상호작용 확인
            CheckBoxInteraction();
            
            // 박스 이동 처리
            if (isInteracting && currentBox != null)
            {
                HandleBoxMovement();
            }
        }
        else
        {
            // E키를 떼면 상호작용 종료
            EndInteraction();
        }
    }

    private void HandleEKeyInput()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            isEKeyPressed = true;
            Debug.Log("E키 누름");
        }
        else if (Input.GetKeyUp(KeyCode.E))
        {
            isEKeyPressed = false;
            Debug.Log("E키 뗌");
        }
    }

    private void CheckBoxInteraction()
    {
        // 이미 상호작용 중인 박스가 있으면 거리 체크
        if (currentBox != null)
        {
            float distance = GetDistanceToBox(currentBox);
            
            // 거리가 너무 멀어지면 상호작용 종료
            if (distance > interactionRange * 1.5f)
            {
                EndInteraction();
                return;
            }
        }
        else
        {
            // 새로운 박스 찾기
            FindNearestBox();
        }
        
        // 접촉 상태 확인 및 상호작용 가능 여부 판단
        if (currentBox != null)
        {
            float distance = GetDistanceToBox(currentBox);
            bool isTouching = IsTouchingBox(currentBox);
            
            // 접촉 중이거나 매우 가까울 때만 상호작용 가능
            if (isTouching || distance < touchDistance)
            {
                if (!isInteracting)
                {
                    StartInteraction();
                }
            }
        }
    }

    private void FindNearestBox()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, interactionRange, boxLayer);
        
        float closestDistance = float.MaxValue;
        GameObject closestBox = null;
        
        foreach (Collider2D collider in colliders)
        {
            if (collider.CompareTag("Box"))
            {
                float distance = GetDistanceToBox(collider.gameObject);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestBox = collider.gameObject;
                }
            }
        }
        
        if (closestBox != null && closestDistance < touchDistance)
        {
            currentBox = closestBox;
            boxRb = currentBox.GetComponent<Rigidbody2D>();
            boxCollider = currentBox.GetComponent<BoxCollider2D>();
        }
    }

    private void StartInteraction()
    {
        isInteracting = true;
        
        // 박스가 플레이어의 어느 쪽에 있는지 저장
        isBoxOnRight = currentBox.transform.position.x > transform.position.x;
        
        Debug.Log($"박스 상호작용 시작 - 박스가 {(isBoxOnRight ? "오른쪽" : "왼쪽")}에 있음");
    }

    private void EndInteraction()
    {
        if (isInteracting || currentBox != null)
        {
            isInteracting = false;
            currentBox = null;
            boxRb = null;
            boxCollider = null;
            isPushing = false;
            isPulling = false;
            Debug.Log("박스 상호작용 종료");
        }
    }

    private void HandleBoxMovement()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        
        // A 또는 D 키를 누르고 있을 때만 이동
        if (Mathf.Abs(horizontalInput) > 0.1f)
        {
            // 현재 박스 위치 업데이트
            isBoxOnRight = currentBox.transform.position.x > transform.position.x;
            
            // 밀기/당기기 판정
            bool movingTowardsBox = (isBoxOnRight && horizontalInput > 0) || (!isBoxOnRight && horizontalInput < 0);
            
            if (movingTowardsBox)
            {
                // 밀기
                isPushing = true;
                isPulling = false;
                PushBox(horizontalInput);
            }
            else
            {
                // 당기기
                isPushing = false;
                isPulling = true;
                PullBox(horizontalInput);
            }
            
            Debug.Log($"이동 방향: {(horizontalInput > 0 ? "오른쪽" : "왼쪽")}, 동작: {(isPushing ? "밀기" : "당기기")}");
        }
        else
        {
            // 움직이지 않으면 밀기/당기기 상태 초기화
            isPushing = false;
            isPulling = false;
        }
    }

    private void PushBox(float direction)
    {
        // 박스 이동
        Vector3 moveDirection = new Vector3(direction, 0, 0);
        boxRb.velocity = new Vector2(direction * pushPullSpeed, boxRb.velocity.y);
        
        // 밀기에서는 플레이어가 박스와 함께 이동 (PlayerCatMovement에서는 자체 이동 처리)
    }

    private void PullBox(float direction)
    {
        // 당기기 방향 계산 (여기가 핵심: 당길 때는 플레이어가 이동하는 방향의 '반대'로 박스가 이동)
        float pullDirection = -direction; // 플레이어 이동 방향의 반대 방향으로 박스가 이동해야 함
        
        // 박스를 플레이어 쪽으로 당기기
        boxRb.velocity = new Vector2(pullDirection * pushPullSpeed, boxRb.velocity.y);
        
        // 플레이어는 자신의 방향으로 계속 이동 (PlayerCatMovement에서 처리)
        // 여기서는 플레이어의 velocity를 설정하지 않고, 기존 이동 시스템을 활용
    }

    private float GetDistanceToBox(GameObject box)
    {
        if (box == null) return float.MaxValue;
        
        // 충돌체 간의 최단 거리 계산
        if (boxCollider != null && playerCollider != null)
        {
            ColliderDistance2D distance = playerCollider.Distance(boxCollider);
            return distance.distance;
        }
        
        // 대안: 변환 위치 기반 거리
        return Vector2.Distance(transform.position, box.transform.position);
    }

    private bool IsTouchingBox(GameObject box)
    {
        if (box == null || playerCollider == null) return false;
        
        BoxCollider2D boxCol = box.GetComponent<BoxCollider2D>();
        if (boxCol == null) return false;
        
        // 충돌체가 접촉 중인지 확인
        return playerCollider.IsTouching(boxCol);
    }

    private void OnDrawGizmosSelected()
    {
        // 상호작용 범위 시각화
        Gizmos.color = isInteracting ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
        
        // 접촉 판정 범위
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, touchDistance);
        
        // 현재 상호작용 중인 박스와의 연결선
        if (currentBox != null)
        {
            Gizmos.color = isPushing ? Color.cyan : (isPulling ? Color.magenta : Color.yellow);
            Gizmos.DrawLine(transform.position, currentBox.transform.position);
            
            // 상태 표시
            Vector3 textPos = transform.position + Vector3.up * 1.5f;
            if (isPushing) Debug.DrawRay(textPos, Vector3.right * 0.5f, Color.cyan);
            if (isPulling) Debug.DrawRay(textPos, Vector3.left * 0.5f, Color.magenta);
        }
    }
}