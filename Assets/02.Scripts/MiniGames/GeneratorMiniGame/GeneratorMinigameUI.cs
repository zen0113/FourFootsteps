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
    public float autoProgressSpeed = 12f; // %/초
    public float timingChallengeInterval = 4f; // 타이밍 도전 간격
    public float timingChallengeDuration = 3.5f;
    public float successZoneSize = 15f; // 성공 구역 크기
    public float rotationSpeed = 300f; // 바늘 회전 속도
    
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
    
    void Start()
    {
        // Canvas는 Unity에서 이미 비활성화 상태로 설정해두므로
        // 여기서는 아무것도 하지 않음
        
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }
    
    public void StartMinigame(Action<bool> completeCallback)
    {
        Debug.Log("=== GeneratorMinigameUI.StartMinigame 호출됨 ===");
        
        onComplete = completeCallback;
        isActive = true;
        progress = 0f;
        consecutiveFails = 0;
        
        // Canvas 상태 확인
        Debug.Log($"minigameCanvas null? {minigameCanvas == null}");
        if (minigameCanvas != null)
        {
            Debug.Log($"Canvas 현재 상태: {minigameCanvas.activeSelf}");
            minigameCanvas.SetActive(true);
            Debug.Log($"Canvas 활성화 후 상태: {minigameCanvas.activeSelf}");
            Debug.Log($"Canvas activeInHierarchy: {minigameCanvas.activeInHierarchy}");
        }
        else
        {
            Debug.LogError("MinigameCanvas가 null입니다!");
        }
        
        // ProgressGauge 상태 확인
        Debug.Log($"progressGauge null? {progressGauge == null}");
        if (progressGauge != null)
        {
            progressGauge.SetProgress(0f);
            Debug.Log("ProgressGauge 초기화 완료");
        }
        else
        {
            Debug.LogError("ProgressGauge가 null입니다!");
        }
        
        // CircularTimer 상태 확인
        Debug.Log($"circularTimer null? {circularTimer == null}");
        if (circularTimer != null)
            circularTimer.gameObject.SetActive(false);
            
        UpdateInstructionText("발전기 수리 중...");
        Debug.Log("텍스트 업데이트 완료");
        
        Debug.Log("MinigameLoop 코루틴 시작...");
        StartCoroutine(MinigameLoop());
    }
    
    IEnumerator MinigameLoop()
    {
        Debug.Log("MinigameLoop 코루틴 시작!");
        
        while (isActive && progress < 100f)
        {
            Debug.Log("자동 진행 단계 시작");
            // 자동 진행 단계
            yield return StartCoroutine(AutoProgressPhase());
            Debug.Log($"자동 진행 완료, 현재 진행도: {progress}%");
            
            if (progress >= 100f) break;
            
            Debug.Log("타이밍 도전 단계 시작");
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
        
        // 결과 메시지가 표시될 시간을 기다림 (2초)
        yield return new WaitForSeconds(2f);
        
        // 그 다음에 기본 메시지로 복원
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
        
        // 결과 메시지 표시
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
        // 간단한 화면 효과
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
        
        if (minigameCanvas != null)
            minigameCanvas.SetActive(false);
            
        onComplete?.Invoke(success);
    }
    
    void Update()
    {
        // ESC로 미니게임 중단 (옵션)
        if (isActive && Input.GetKeyDown(KeyCode.Escape))
        {
            CompleteMinigame(false);
        }
    }
}