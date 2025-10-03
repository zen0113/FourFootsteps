using UnityEngine;

/// <summary>
/// 발전기 오브젝트와의 상호작용을 담당하는 스크립트
/// 특정 오브젝트가 활성화되어 있을 때 E키를 누르면 미니게임 시작
/// </summary>
public class GeneratorInteraction : MonoBehaviour
{
    [Header("미니게임 UI")]
    [SerializeField] private GeneratorMinigameUI minigameUI;
    
    [Header("트리거 조건 오브젝트")]
    [Tooltip("이 오브젝트가 활성화되어 있을 때만 E키 입력을 받습니다")]
    [SerializeField] private GameObject triggerObject;
    
    [Header("디버그")]
    [SerializeField] private bool showDebugLog = true;
    
    private bool hasCompletedMinigame = false; // 미니게임을 이미 완료했는지 여부
    
    void Start()
    {
        // 미니게임 UI 자동 찾기
        if (minigameUI == null)
        {
            minigameUI = FindObjectOfType<GeneratorMinigameUI>();
            if (minigameUI == null)
            {
                Debug.LogError("[GeneratorInteraction] GeneratorMinigameUI를 찾을 수 없습니다!");
            }
        }
    }
    
    void Update()
    {
        // 미니게임을 이미 완료했으면 더 이상 입력 받지 않음
        if (hasCompletedMinigame)
        {
            return;
        }
        
        // 트리거 오브젝트가 활성화되어 있는지 확인
        bool canInteract = triggerObject != null && triggerObject.activeSelf;
        
        // 트리거 오브젝트가 활성화되어 있고 E키를 눌렀을 때
        if (canInteract && Input.GetKeyDown(KeyCode.E))
        {
            StartGeneratorMinigame();
        }
    }
    
    /// <summary>
    /// 발전기 미니게임 시작
    /// </summary>
    private void StartGeneratorMinigame()
    {
        if (showDebugLog)
        {
            Debug.Log("[GeneratorInteraction] 발전기 미니게임 시작!");
        }
        
        // 미니게임 시작
        if (minigameUI != null)
        {
            minigameUI.StartMinigame(OnMinigameComplete);
        }
        else
        {
            Debug.LogError("[GeneratorInteraction] minigameUI가 null입니다!");
        }
    }
    
    /// <summary>
    /// 미니게임 완료 콜백
    /// </summary>
    /// <param name="success">성공 여부</param>
    private void OnMinigameComplete(bool success)
    {
        if (showDebugLog)
        {
            Debug.Log($"[GeneratorInteraction] 미니게임 완료! 성공 여부: {success}");
        }
        
        if (success)
        {
            // 성공 시 다시 시작할 수 없도록 설정
            hasCompletedMinigame = true;
            
            // 트리거 오브젝트 비활성화 (선택사항)
            if (triggerObject != null)
            {
                triggerObject.SetActive(false);
                if (showDebugLog)
                {
                    Debug.Log("[GeneratorInteraction] 트리거 오브젝트 비활성화");
                }
            }
        }
        else
        {
            // 실패 시 다시 시도 가능
            if (showDebugLog)
            {
                Debug.Log("[GeneratorInteraction] 미니게임 실패 - 다시 시도 가능");
            }
        }
    }
    
    /// <summary>
    /// 미니게임 완료 상태를 외부에서 확인할 수 있도록
    /// </summary>
    public bool IsCompleted => hasCompletedMinigame;
    
    /// <summary>
    /// 미니게임 완료 상태를 외부에서 리셋할 수 있도록 (테스트용)
    /// </summary>
    public void ResetCompletion()
    {
        hasCompletedMinigame = false;
        if (showDebugLog)
        {
            Debug.Log("[GeneratorInteraction] 완료 상태 리셋");
        }
    }
}