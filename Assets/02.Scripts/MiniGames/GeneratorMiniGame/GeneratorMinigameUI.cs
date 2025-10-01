using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;

public class GeneratorMinigameUI : MonoBehaviour
{
    [Header("UI 컴포넌트")]
    public GameObject minigameCanvas;
    public ProgressGauge progressGauge;
    public CircularTimer circularTimer;
    public Text instructionText;
    
    [Header("게임 설정")]
    public float autoProgressSpeed = 6f;
    public float timingChallengeInterval = 4f;
    public float timingChallengeDuration = 3.5f;
    public float successZoneSize = 15f;
    public float rotationSpeed = 300f;
    
    [Header("오디오")]
    public AudioSource audioSource;
    public AudioClip progressSound;
    public AudioClip timingAppearSound;
    public AudioClip successSound;
    public AudioClip failSound;
    public AudioClip generatorCompleteSound; // 발전기 수리 완료 효과음
    
    [Header("플레이어 컨트롤러 설정")]
    public PlayerCatMovement playerMovement;
    
    [Header("발전기 오브젝트 설정")]
    public SpriteRenderer generatorSpriteRenderer; // 발전기의 SpriteRenderer
    public Sprite generatorRepairedSprite; // 수리 완료 후 스프라이트
    private int originalSortingOrder = 3; // 원래 Sorting Order
    private int repairedSortingOrder = 1; // 수리 후 Sorting Order
    
    private Action<bool> onComplete;
    private bool isActive = false;
    private bool isTimingChallenge = false;
    private float progress = 0f;
    private int consecutiveFails = 0;
    private const int maxConsecutiveFails = 3;
    
    void Start()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
            
        // 플레이어 자동 찾기
        if (playerMovement == null)
        {
            playerMovement = FindObjectOfType<PlayerCatMovement>();
            if (playerMovement == null)
            {
                Debug.LogError("[GeneratorMinigameUI] PlayerCatMovement를 찾을 수 없습니다!");
            }
        }
        
        // 발전기 SpriteRenderer 확인
        if (generatorSpriteRenderer != null)
        {
            originalSortingOrder = generatorSpriteRenderer.sortingOrder;
            Debug.Log($"[GeneratorMinigameUI] 발전기 원본 Sorting Order: {originalSortingOrder}");
        }
        
        // 게임 시작 시 플레이어를 웅크린 상태로 만들고 입력 차단
        if (playerMovement != null)
        {
            Debug.Log("[GeneratorMinigameUI] 게임 시작 - 플레이어 강제 웅크리기 및 입력 차단");
            playerMovement.SetMiniGameInputBlocked(true);
            playerMovement.ForceCrouch = true;
            playerMovement.IsJumpingBlocked = true;
        }
    }
    
    public void StartMinigame(Action<bool> completeCallback)
    {
        Debug.Log("=== GeneratorMinigameUI.StartMinigame 호출됨 ===");
        
        onComplete = completeCallback;
        isActive = true;
        progress = 0f;
        consecutiveFails = 0;
        
        // 플레이어 상태 확인
        if (playerMovement != null)
        {
            playerMovement.SetMiniGameInputBlocked(true);
            playerMovement.ForceCrouch = true;
            playerMovement.IsJumpingBlocked = true;
            Debug.Log("[GeneratorMinigameUI] 플레이어 입력 차단 및 웅크리기 활성화");
        }
        
        // Canvas 활성화
        if (minigameCanvas != null)
        {
            minigameCanvas.SetActive(true);
            Debug.Log($"Canvas 활성화: {minigameCanvas.activeSelf}");
        }
        else
        {
            Debug.LogError("MinigameCanvas가 null입니다!");
        }
        
        // ProgressGauge 초기화
        if (progressGauge != null)
        {
            progressGauge.SetProgress(0f);
            Debug.Log("ProgressGauge 초기화 완료");
        }
        else
        {
            Debug.LogError("ProgressGauge가 null입니다!");
        }
        
        // CircularTimer 숨기기
        if (circularTimer != null)
            circularTimer.gameObject.SetActive(false);
            
        UpdateInstructionText("발전기 수리 중...");
        
        StartCoroutine(MinigameLoop());
    }
    
    IEnumerator MinigameLoop()
    {
        Debug.Log("MinigameLoop 코루틴 시작!");
        
        while (isActive && progress < 100f)
        {
            // 자동 진행 단계
            yield return StartCoroutine(AutoProgressPhase());
            Debug.Log($"자동 진행 완료, 현재 진행도: {progress}%");
            
            if (progress >= 100f) break;
            
            // 타이밍 도전 단계
            if (isActive)
            {
                yield return StartCoroutine(TimingChallengePhase());
            }
        }
        
        Debug.Log($"MinigameLoop 종료, 최종 진행도: {progress}%");
        
        if (isActive)
        {
            CompleteMinigame(progress >= 100f);
        }
    }
    
    IEnumerator AutoProgressPhase()
    {
        float waitTime = UnityEngine.Random.Range(2f, timingChallengeInterval);
        float elapsedTime = 0f;
        
        while (elapsedTime < waitTime && progress < 100f && isActive)
        {
            progress += autoProgressSpeed * Time.deltaTime;
            progress = Mathf.Clamp(progress, 0f, 100f);
            
            if (progressGauge != null)
                progressGauge.SetProgress(progress);
                
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }
    
    IEnumerator TimingChallengePhase()
    {
        isTimingChallenge = true;
        
        // 타이밍 UI 표시
        if (circularTimer != null)
        {
            circularTimer.gameObject.SetActive(true);
            
            var settings = new CircularTimer.TimingSettings();
            settings.rotationSpeed = rotationSpeed;
            
            circularTimer.StartTiming(settings);
        }
        
        PlaySound(timingAppearSound);
        UpdateInstructionText("스페이스바를 눌러 타이밍을 맞추세요!");
        
        bool inputReceived = false;
        float challengeTime = 0f;
        
        while (challengeTime < timingChallengeDuration && !inputReceived && isActive)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                inputReceived = true;
                ProcessTimingInput();
            }
            
            challengeTime += Time.deltaTime;
            yield return null;
        }
        
        // 입력이 없었을 경우
        if (!inputReceived && isActive)
        {
            ProcessTimingResult(TimingResult.NoInput);
        }
        
        // 타이밍 UI 숨기기
        if (circularTimer != null)
        {
            circularTimer.gameObject.SetActive(false);
        }
        
        isTimingChallenge = false;
        
        // 결과 메시지 표시 시간
        yield return new WaitForSeconds(2f);
        
        if (isActive)
        {
            UpdateInstructionText("발전기 수리 중...");
        }
    }
    
    void ProcessTimingInput()
    {
        if (circularTimer != null)
        {
            TimingResult result = circularTimer.GetTimingResult();
            ProcessTimingResult(result);
        }
    }
    
    void ProcessTimingResult(TimingResult result)
    {
        float progressChange = 0f;
        string resultMessage = "";
        
        switch (result)
        {
            case TimingResult.Perfect:
                progressChange = 10f;
                consecutiveFails = 0;
                resultMessage = "퍼펙트!";
                PlaySound(successSound);
                StartCoroutine(ShowFeedbackEffect(Color.green));
                break;
                
            case TimingResult.Good:
                progressChange = 5f;
                consecutiveFails = 0;
                resultMessage = "성공!";
                PlaySound(successSound);
                StartCoroutine(ShowFeedbackEffect(Color.green));
                break;
                
            case TimingResult.Miss:
                progressChange = -15f;
                consecutiveFails++;
                resultMessage = "실패!";
                PlaySound(failSound);
                StartCoroutine(ShowFeedbackEffect(Color.red));
                break;
                
            case TimingResult.NoInput:
                progressChange = -10f;
                consecutiveFails++;
                resultMessage = "입력 없음!";
                PlaySound(failSound);
                StartCoroutine(ShowFeedbackEffect(Color.red));
                break;
        }
        
        UpdateInstructionText($"{resultMessage} (진행도 {progressChange:+0;-0}%)");
        
        progress += progressChange;
        progress = Mathf.Clamp(progress, 0f, 100f);
        
        if (progressGauge != null)
            progressGauge.SetProgress(progress);
            
        // 연속 실패 체크
        if (consecutiveFails >= maxConsecutiveFails)
        {
            CompleteMinigame(false);
        }
    }
    
    IEnumerator ShowFeedbackEffect(Color color)
    {
        if (minigameCanvas != null)
        {
            var canvasGroup = minigameCanvas.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = minigameCanvas.AddComponent<CanvasGroup>();
                
            float originalAlpha = canvasGroup.alpha;
            canvasGroup.alpha = 0.8f;
            yield return new WaitForSeconds(0.1f);
            canvasGroup.alpha = originalAlpha;
        }
    }
    
    void UpdateInstructionText(string text)
    {
        if (instructionText != null)
            instructionText.text = text;
    }
    
    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }
    
    void CompleteMinigame(bool success)
    {
        Debug.Log($"[GeneratorMinigameUI] 미니게임 완료 - 성공 여부: {success}");
        
        isActive = false;
        
        // 성공했을 때만 플레이어 해방 및 발전기 변경
        if (success)
        {
            StartCoroutine(FinishMinigameSequence());
        }
        else
        {
            // 실패 시 미니게임만 종료하고 플레이어는 웅크린 상태 유지
            if (minigameCanvas != null)
                minigameCanvas.SetActive(false);
                
            onComplete?.Invoke(success);
        }
    }
    
    IEnumerator FinishMinigameSequence()
    {
        Debug.Log("[GeneratorMinigameUI] 미니게임 성공 - 마무리 시퀀스 시작");
        
        // 약간의 대기 (성공 연출)
        yield return new WaitForSeconds(0.5f);
        
        // 발전기 수리 완료 효과음 재생
        if (generatorCompleteSound != null)
        {
            PlaySound(generatorCompleteSound);
            Debug.Log("[GeneratorMinigameUI] 발전기 수리 완료 효과음 재생");
        }
        
        // 발전기 스프라이트 변경 및 Sorting Order 변경
        if (generatorSpriteRenderer != null)
        {
            // 스프라이트 변경
            if (generatorRepairedSprite != null)
            {
                generatorSpriteRenderer.sprite = generatorRepairedSprite;
                Debug.Log("[GeneratorMinigameUI] 발전기 스프라이트 변경 완료");
            }
            else
            {
                Debug.LogWarning("[GeneratorMinigameUI] generatorRepairedSprite가 할당되지 않았습니다!");
            }
            
            // Sorting Order 변경 (3 → 1)
            generatorSpriteRenderer.sortingOrder = repairedSortingOrder;
            Debug.Log($"[GeneratorMinigameUI] 발전기 Sorting Order 변경: {originalSortingOrder} → {repairedSortingOrder}");
        }
        else
        {
            Debug.LogWarning("[GeneratorMinigameUI] generatorSpriteRenderer가 할당되지 않았습니다!");
        }
        
        // 효과음이 재생될 시간을 추가로 대기 (선택사항)
        yield return new WaitForSeconds(0.3f);
        
        // UI 비활성화
        if (minigameCanvas != null)
            minigameCanvas.SetActive(false);
        
        // 플레이어 상태 복원 - 자유롭게 움직일 수 있도록
        if (playerMovement != null)
        {
            Debug.Log("[GeneratorMinigameUI] 플레이어 해방 - 웅크리기 및 입력 차단 해제");
            playerMovement.SetMiniGameInputBlocked(false);
            playerMovement.ForceCrouch = false;
            playerMovement.IsJumpingBlocked = false;
            playerMovement.SetCrouchMovingState(false);
        }
        
        Debug.Log("[GeneratorMinigameUI] 플레이어 이제 자유롭게 움직일 수 있음!");
        
        // 콜백 호출
        onComplete?.Invoke(true);
    }
    
    void Update()
    {
        if (!isActive) return;
        
        // ESC로 미니게임 중단
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CompleteMinigame(false);
            return;
        }
    }
    
    // 오브젝트가 비활성화될 때도 안전하게 처리
    void OnDisable()
    {
        if (isActive)
        {
            Debug.Log("[GeneratorMinigameUI] OnDisable - 비정상 종료 감지");
            isActive = false;
            
            // 비정상 종료 시에도 플레이어 해방
            if (playerMovement != null)
            {
                playerMovement.SetMiniGameInputBlocked(false);
                playerMovement.ForceCrouch = false;
                playerMovement.IsJumpingBlocked = false;
            }
        }
    }
}