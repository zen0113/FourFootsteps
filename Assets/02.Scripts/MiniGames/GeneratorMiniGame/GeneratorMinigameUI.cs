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
    
    private Action<bool> onComplete;
    private bool isActive = false;
    private bool isTimingChallenge = false;
    private float progress = 0f;
    private int consecutiveFails = 0;
    private const int maxConsecutiveFails = 3;
    
    // 플레이어 참조 추가
    private PlayerCatMovement player;
    
    void Start()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
            
        // 플레이어 찾기
        player = FindObjectOfType<PlayerCatMovement>();
        if (player == null)
        {
            Debug.LogError("PlayerCatMovement를 찾을 수 없습니다!");
        }
    }
    
    public void StartMinigame(Action<bool> completeCallback)
    {
        Debug.Log("=== GeneratorMinigameUI.StartMinigame 호출됨 ===");
        
        onComplete = completeCallback;
        isActive = true;
        progress = 0f;
        consecutiveFails = 0;
        
        // 플레이어 입력 차단
        if (player != null)
        {
            player.SetMiniGameInputBlocked(true);
            Debug.Log("플레이어 입력 차단 완료");
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
        isActive = false;
        
        // 플레이어 입력 차단 해제
        if (player != null)
        {
            player.SetMiniGameInputBlocked(false);
            Debug.Log("플레이어 입력 차단 해제 완료");
        }
        
        if (minigameCanvas != null)
            minigameCanvas.SetActive(false);
            
        onComplete?.Invoke(success);
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
    
    // 오브젝트가 비활성화될 때도 입력 차단 해제
    void OnDisable()
    {
        if (player != null && isActive)
        {
            player.SetMiniGameInputBlocked(false);
            Debug.Log("OnDisable: 플레이어 입력 차단 해제");
        }
    }
}