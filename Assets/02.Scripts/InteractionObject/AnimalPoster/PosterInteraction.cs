using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PosterInteraction : MonoBehaviour
{
    [SerializeField] private float interactionRange = 2f;
    [SerializeField] private Vector2 interactionCenter = Vector2.zero; // 상호작용 중심점 오프셋
    
    private PosterUIController uiController;
    private bool playerInRange = false;
    private bool isUIActive = false;
    private bool hasRequestedApiData = false; // API 데이터 요청 여부 확인
    
    private void Start()
    {
        // Canvas의 PosterPanel에 직접 접근
        Transform canvas = GameObject.Find("PosterUICanvas").transform;
        if (canvas != null)
        {
            Transform posterPanel = canvas.Find("PosterPanel");
            if (posterPanel != null)
            {
                uiController = posterPanel.GetComponent<PosterUIController>();
                if (uiController == null)
                {
                    Debug.LogError("PosterPanel에 PosterUIController 컴포넌트가 없습니다!");
                }
            }
            else
            {
                Debug.LogError("Canvas > PosterPanel을 찾을 수 없습니다!");
            }
        }
        else
        {
            Debug.LogError("Canvas를 찾을 수 없습니다!");
        }
    }
    
    private void Update()
    {
        // 플레이어가 범위 내에 있고 E키를 눌렀을 때
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            // UI 토글
            isUIActive = !isUIActive;
            if (uiController != null)
            {
                uiController.ToggleUI(isUIActive);
            }
        }
    }
    
    // 플레이어 위치가 상호작용 범위 내에 있는지 확인
    private bool IsPlayerInRange(Vector2 playerPosition)
    {
        Vector2 centerPosition = (Vector2)transform.position + interactionCenter;
        return Vector2.Distance(playerPosition, centerPosition) <= interactionRange;
    }
    
    // 플레이어가 트리거에 들어왔을 때
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // 범위 내에 있는지 추가 확인
            if (IsPlayerInRange(other.transform.position))
            {
                playerInRange = true;
                ShowInteractionPrompt(true);
                
                // 플레이어가 포스터 근처에 왔을 때 API 데이터 요청
                /*if (!hasRequestedApiData)
                {
                    hasRequestedApiData = true;
                    
                    // 0.5초 후에 API 데이터 요청 (플레이어 움직임에 영향 최소화)
                    StartCoroutine(RequestApiWithDelay(0.5f));
                }*/
            }
        }
    }
    
    // 지연 API 요청
    private IEnumerator RequestApiWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (AnimalDataManager.Instance != null)
        {
            Debug.Log("포스터 근처에서 API 데이터 요청");
            AnimalDataManager.Instance.RequestApiDataIfNeeded();
        }
    }
    
    // 플레이어가 트리거에서 나갔을 때
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            ShowInteractionPrompt(false);
            
            // UI가 열려있으면 닫기
            if (isUIActive)
            {
                isUIActive = false;
                if (uiController != null)
                {
                    uiController.ToggleUI(false);
                }
            }
        }
    }
    
    // 상호작용 힌트 표시/숨김
    private void ShowInteractionPrompt(bool show)
    {
        // 상호작용 힌트 UI 표시 로직
        // 예: 포스터 위에 "E키" 아이콘 표시 등
    }
    
    // 기즈모로 상호작용 범위 시각화
    private void OnDrawGizmosSelected()
    {
        Vector3 centerPosition = transform.position + new Vector3(interactionCenter.x, interactionCenter.y, 0);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(centerPosition, interactionRange);
    }
}