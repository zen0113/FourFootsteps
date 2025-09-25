using UnityEngine;

public class MinigameManager : MonoBehaviour
{
    [Header("UI 참조")]
    public GeneratorMinigameUI minigameUI;
    
    private GeneratorInteractable currentGenerator;
    private bool isMinigameActive = false;
    
    void Start()
    {
        Debug.Log("MinigameManager Start() 호출됨");
        
        if (minigameUI == null)
        {
            Debug.LogError("MinigameUI가 할당되지 않았습니다! Inspector에서 GeneratorMinigameCanvas를 할당해주세요.");
        }
        else
        {
            Debug.Log("MinigameUI가 성공적으로 할당되었습니다!");
            Debug.Log($"할당된 UI 오브젝트 이름: {minigameUI.gameObject.name}");
        }
    }
    
    public void StartGeneratorMinigame(GeneratorInteractable generator)
    {
        Debug.Log("=== StartGeneratorMinigame 호출 ===");
        
        if (isMinigameActive)
        {
            Debug.Log("이미 미니게임이 활성화되어 있음");
            return;
        }
        
        if (minigameUI == null)
        {
            Debug.LogError("MinigameUI가 null이어서 미니게임을 시작할 수 없습니다!");
            return;
        }
        
        Debug.Log("미니게임 시작!");
        
        currentGenerator = generator;
        isMinigameActive = true;
        
        Debug.Log("MinigameUI.StartMinigame() 호출 시도...");
        
        // 이 부분이 실행되는지 확인
        try
        {
            minigameUI.StartMinigame(OnMinigameComplete);
            Debug.Log("MinigameUI.StartMinigame() 호출 성공!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"MinigameUI.StartMinigame() 호출 중 오류: {e.Message}");
        }
    }
    
    void OnMinigameComplete(bool success)
    {
        Debug.Log($"=== OnMinigameComplete 호출: {success} ===");
        isMinigameActive = false;
        
        if (success && currentGenerator != null)
        {
            currentGenerator.CompleteGenerator();
        }
        
        currentGenerator = null;
        
        Debug.Log($"미니게임 결과: {(success ? "성공" : "실패")}");
    }
}