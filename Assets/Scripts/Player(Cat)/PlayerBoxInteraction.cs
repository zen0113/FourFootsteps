using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBoxInteraction : MonoBehaviour
{
    [Header("박스 상호작용 설정")]
    [SerializeField] public float pushPullSpeed = 3f;     // 밀고 당기는 속도
    [SerializeField] public float interactionRange = 1.2f; // 상호작용 범위
    [SerializeField] public LayerMask boxLayer;          // 박스 레이어 마스크
    
    private GameObject currentBox;                       // 현재 상호작용 중인 박스
    private bool isInteracting = false;                  // E키를 누르고 있는지
    private bool isBoxOnRight;                          // 박스가 플레이어 오른쪽에 있는지
    
    // public 프로퍼티
    public bool IsInteracting => isInteracting;
    public GameObject CurrentBox => currentBox;

    private void Update()
    {
        // E키 입력 처리
        if (Input.GetKeyDown(KeyCode.E))
        {
            TryStartInteraction();
        }
        else if (Input.GetKeyUp(KeyCode.E))
        {
            EndInteraction();
        }

        // 상호작용 중일 때 박스 이동
        if (isInteracting && currentBox != null)
        {
            MoveBox();
        }
    }

    private void TryStartInteraction()
    {
        // 가장 가까운 박스 찾기
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, interactionRange);
        
        float closestDistance = float.MaxValue;
        GameObject closestBox = null;
        
        foreach (Collider2D collider in colliders)
        {
            if (collider.CompareTag("Box"))
            {
                float distance = Vector2.Distance(transform.position, collider.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestBox = collider.gameObject;
                }
            }
        }
        
        if (closestBox != null)
        {
            currentBox = closestBox;
            isInteracting = true;
            
            // 박스가 플레이어의 어느 쪽에 있는지 저장
            isBoxOnRight = currentBox.transform.position.x > transform.position.x;
            
            Debug.Log($"박스 상호작용 시작 - 박스가 {(isBoxOnRight ? "오른쪽" : "왼쪽")}에 있음");
        }
    }

    private void EndInteraction()
    {
        if (isInteracting)
        {
            isInteracting = false;
            currentBox = null;
            Debug.Log("박스 상호작용 종료");
        }
    }

    private void MoveBox()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        
        if (Mathf.Abs(horizontalInput) > 0.1f)
        {
            // 박스와 플레이어 사이의 거리 체크
            float distance = Mathf.Abs(currentBox.transform.position.x - transform.position.x);
            
            // 너무 멀어지면 상호작용 종료
            if (distance > interactionRange * 1.2f)
            {
                EndInteraction();
                return;
            }
            
            // 박스 이동 방향 결정
            bool movingTowardsBox = (isBoxOnRight && horizontalInput > 0) || (!isBoxOnRight && horizontalInput < 0);
            
            // 박스 이동
            Vector3 moveDirection = new Vector3(horizontalInput, 0, 0);
            currentBox.transform.position += moveDirection * pushPullSpeed * Time.deltaTime;
            
            // 당길 때: 박스에서 멀어지는 방향으로 이동할 때
            if (!movingTowardsBox)
            {
                // 플레이어도 같이 이동 (당기는 느낌을 주기 위해)
                transform.position += moveDirection * pushPullSpeed * 0.8f * Time.deltaTime;
            }
            
            Debug.Log(movingTowardsBox ? "박스 밀기" : "박스 당기기");
        }
    }

    private void OnDrawGizmosSelected()
    {
        // 상호작용 범위 시각화
        Gizmos.color = isInteracting ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
        
        // 현재 상호작용 중인 박스와의 연결선
        if (currentBox != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, currentBox.transform.position);
        }
    }
}