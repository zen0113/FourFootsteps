using UnityEngine;

public class MinigameManager : MonoBehaviour
{
    [Header("UI 참조")]
    public GeneratorMinigameUI minigameUI;
    
    private GeneratorInteractable currentGenerator;
    private bool isMinigameActive = false;
    private MonoBehaviour[] disabledScripts;
    
    void Start()
    {
        if (minigameUI == null)
        {
            Debug.LogError("MinigameUI가 할당되지 않았습니다! Inspector에서 GeneratorMinigameCanvas를 할당해주세요.");
        }
        else
        {
            Debug.Log("MinigameUI가 성공적으로 할당되었습니다!");
        }
    }
    
    public void StartGeneratorMinigame(GeneratorInteractable generator)
    {
        if (isMinigameActive) return;
        
        if (minigameUI == null)
        {
            Debug.LogError("MinigameUI가 없어서 미니게임을 시작할 수 없습니다!");
            return;
        }
        
        currentGenerator = generator;
        isMinigameActive = true;
        
        // 플레이어 완전 정지
        DisablePlayer();
        
        // 미니게임 UI 시작
        minigameUI.StartMinigame(OnMinigameComplete);
        Debug.Log("미니게임 시작!");
    }
    
    void DisablePlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;
        
        // Rigidbody2D 정지
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.simulated = false;
        }
        
        // 모든 스크립트 비활성화
        MonoBehaviour[] allScripts = player.GetComponents<MonoBehaviour>();
        System.Collections.Generic.List<MonoBehaviour> scriptsToDisable = new System.Collections.Generic.List<MonoBehaviour>();
        
        foreach (var script in allScripts)
        {
            if (script != null && script.enabled && script != this)
            {
                scriptsToDisable.Add(script);
                script.enabled = false;
            }
        }
        
        disabledScripts = scriptsToDisable.ToArray();
        
        // Animator 정지
        Animator animator = player.GetComponent<Animator>();
        if (animator != null)
        {
            animator.enabled = false;
        }
    }
    
    void EnablePlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;
        
        // Rigidbody2D 재개
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.simulated = true;
        }
        
        // 저장된 스크립트 재활성화
        if (disabledScripts != null)
        {
            foreach (var script in disabledScripts)
            {
                if (script != null)
                {
                    script.enabled = true;
                }
            }
        }
        
        // Animator 재개
        Animator animator = player.GetComponent<Animator>();
        if (animator != null)
        {
            animator.enabled = true;
        }
        
        disabledScripts = null;
    }
    
    void OnMinigameComplete(bool success)
    {
        isMinigameActive = false;
        
        // 플레이어 복구
        EnablePlayer();
        
        if (success && currentGenerator != null)
        {
            currentGenerator.CompleteGenerator();
        }
        
        currentGenerator = null;
        
        Debug.Log($"미니게임 결과: {(success ? "성공" : "실패")}");
    }
}