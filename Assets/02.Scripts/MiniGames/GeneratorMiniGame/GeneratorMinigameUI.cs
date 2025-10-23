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
    public float timingChallengeProgressSpeed = 1f;
    public float progressAnimationSpeed = 30f;
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
    public AudioClip generatorCompleteSound;

    [Header("플레이어 컨트롤러 설정")]
    public PlayerCatMovement playerMovement;

    [Header("발전기 오브젝트 설정")]
    public SpriteRenderer generatorSpriteRenderer;
    public Sprite generatorRepairedSprite;
    private int originalSortingOrder = 3;
    private int repairedSortingOrder = 1;

    private Action<bool> onComplete;
    private bool isActive = false;
    private bool isTimingChallenge = false;
    private float progress = 0f;
    private int consecutiveFails = 0;
    private const int maxConsecutiveFails = 3;
    private Coroutine progressAnimationCoroutine;

    void Start()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (playerMovement == null)
        {
            playerMovement = FindObjectOfType<PlayerCatMovement>();
            if (playerMovement == null)
            {
                Debug.LogError("[GeneratorMinigameUI] PlayerCatMovement를 찾을 수 없습니다!");
            }
        }

        if (generatorSpriteRenderer != null)
        {
            originalSortingOrder = generatorSpriteRenderer.sortingOrder;
        }

        if (progressGauge != null)
        {
            progressGauge.gameObject.SetActive(false);
        }

        if (circularTimer != null)
        {
            circularTimer.gameObject.SetActive(false);
        }

        if (minigameCanvas != null)
        {
            minigameCanvas.SetActive(false);
        }

    }

    public void StartMinigame(Action<bool> completeCallback)
    {
        Debug.Log("=== GeneratorMinigameUI.StartMinigame 호출됨 ===");

        onComplete = completeCallback;
        isActive = true;
        progress = 0f;
        consecutiveFails = 0;

        if (playerMovement != null)
        {
            playerMovement.SetMiniGameInputBlocked(true);
            playerMovement.ForceCrouch = true;
            playerMovement.IsJumpingBlocked = true;
            Debug.Log("[GeneratorMinigameUI] 플레이어 입력 차단 및 웅크리기 활성화");
        }

        if (minigameCanvas != null)
        {
            minigameCanvas.SetActive(true);
            Debug.Log($"Canvas 활성화: {minigameCanvas.activeSelf}");
        }
        else
        {
            Debug.LogError("MinigameCanvas가 null입니다!");
        }

        if (progressGauge != null)
        {
            progressGauge.gameObject.SetActive(true);
            progressGauge.SetProgress(0f);
            Debug.Log("ProgressGauge 활성화 및 초기화 완료");
        }
        else
        {
            Debug.LogError("ProgressGauge가 null입니다!");
        }

        if (circularTimer != null)
        {
            circularTimer.gameObject.SetActive(false);
            Debug.Log("CircularTimer 비활성화 유지");
        }

        UpdateInstructionText("잠금장치 푸는 중...");

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

                if (progressAnimationCoroutine != null)
                {
                    yield return progressAnimationCoroutine;
                }
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
        // 진행중인 애니메이션이 있다면 정지 (안전 장치)
        if (progressAnimationCoroutine != null)
        {
            StopCoroutine(progressAnimationCoroutine);
            progressAnimationCoroutine = null;
        }

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

        if (circularTimer != null)
        {
            circularTimer.gameObject.SetActive(true);
            Debug.Log("[GeneratorMinigameUI] CircularTimer 활성화 (타이밍 도전 시작)");

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
            progress += timingChallengeProgressSpeed * Time.deltaTime;
            progress = Mathf.Clamp(progress, 0f, 100f);
            if (progressGauge != null)
            {
                progressGauge.SetProgress(progress);
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                inputReceived = true;
                ProcessTimingInput();
            }

            challengeTime += Time.deltaTime;
            yield return null;
        }

        if (!inputReceived && isActive)
        {
            ProcessTimingResult(TimingResult.NoInput);
        }

        if (circularTimer != null)
        {
            circularTimer.gameObject.SetActive(false);
            Debug.Log("[GeneratorMinigameUI] CircularTimer 비활성화 (타이밍 도전 종료)");
        }

        isTimingChallenge = false;
        yield return new WaitForSeconds(2f);

        if (isActive)
        {
            UpdateInstructionText("잠금장치 푸는 중...");
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

        float targetProgress = Mathf.Clamp(progress + progressChange, 0f, 100f);
        if (progressAnimationCoroutine != null)
        {
            StopCoroutine(progressAnimationCoroutine);
        }
        progressAnimationCoroutine = StartCoroutine(AnimateProgress(targetProgress));

        if (consecutiveFails >= maxConsecutiveFails)
        {
            CompleteMinigame(false);
        }
    }

    IEnumerator AnimateProgress(float targetProgress)
    {
        while (!Mathf.Approximately(progress, targetProgress))
        {
            progress = Mathf.MoveTowards(progress, targetProgress, progressAnimationSpeed * Time.deltaTime);
            if (progressGauge != null)
            {
                progressGauge.SetProgress(progress);
            }
            yield return null;
        }

        progress = targetProgress;
        if (progressGauge != null)
        {
            progressGauge.SetProgress(progress);
        }

        progressAnimationCoroutine = null;
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

        if (success)
        {
            StartCoroutine(FinishMinigameSequence());
        }
        else
        {
            if (playerMovement != null)
            {
                playerMovement.SetMiniGameInputBlocked(false);
                playerMovement.ForceCrouch = false;
                playerMovement.IsJumpingBlocked = false;
            }

            if (progressGauge != null)
                progressGauge.gameObject.SetActive(false);

            if (circularTimer != null)
                circularTimer.gameObject.SetActive(false);

            if (minigameCanvas != null)
                minigameCanvas.SetActive(false);

            onComplete?.Invoke(success);
        }
    }

    IEnumerator FinishMinigameSequence()
    {
        Debug.Log("[GeneratorMinigameUI] 미니게임 성공 - 마무리 시퀀스 시작");

        yield return new WaitForSeconds(0.5f);

        if (generatorCompleteSound != null)
        {
            PlaySound(generatorCompleteSound);
            //Debug.Log("[GeneratorMinigameUI] 잠금장치 해제제 효과음 재생");
        }

        if (generatorSpriteRenderer != null)
        {
            if (generatorRepairedSprite != null)
            {
                generatorSpriteRenderer.sprite = generatorRepairedSprite;
                //Debug.Log("[GeneratorMinigameUI] 철장장 스프라이트 변경 완료");
            }
            else
            {
                Debug.LogWarning("[GeneratorMinigameUI] generatorRepairedSprite가 할당되지 않았습니다!");
            }

            generatorSpriteRenderer.sortingOrder = repairedSortingOrder;
           // Debug.Log($"[GeneratorMinigameUI] 발전기 Sorting Order 변경: {originalSortingOrder} → {repairedSortingOrder}");
        }
        else
        {
            Debug.LogWarning("[GeneratorMinigameUI] generatorSpriteRenderer가 할당되지 않았습니다!");
        }

        yield return new WaitForSeconds(0.3f);

        if (progressGauge != null)
            progressGauge.gameObject.SetActive(false);

        if (circularTimer != null)
            circularTimer.gameObject.SetActive(false);

        if (minigameCanvas != null)
            minigameCanvas.SetActive(false);

        if (playerMovement != null)
        {
            Debug.Log("[GeneratorMinigameUI] 플레이어 해방 - 웅크리기 및 입력 차단 해제");
            playerMovement.SetMiniGameInputBlocked(false);
            playerMovement.ForceCrouch = false;
            playerMovement.IsJumpingBlocked = false;
            playerMovement.SetCrouchMovingState(false);
        }

        Debug.Log("[GeneratorMinigameUI] 플레이어 이제 자유롭게 움직일 수 있음!");

        onComplete?.Invoke(true);
    }

    void Update()
    {
        if (!isActive) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CompleteMinigame(false);
            return;
        }
    }

    void OnDisable()
    {
        if (isActive)
        {
            Debug.Log("[GeneratorMinigameUI] OnDisable - 비정상 종료 감지");
            isActive = false;

            if (playerMovement != null)
            {
                playerMovement.SetMiniGameInputBlocked(false);
                playerMovement.ForceCrouch = false;
                playerMovement.IsJumpingBlocked = false;
            }
        }
    }
}