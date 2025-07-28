using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class StruggleMiniGame : MonoBehaviour
{
    [Header("UI")]
    public GameObject miniGameUI;
    public CanvasGroup miniGameCanvasGroup; // 페이드 효과를 위한 CanvasGroup
    public Image gaugeFillImage;
    public TextMeshProUGUI keyPromptText;
    public TextMeshProUGUI missTextPrefab; // 헛딧음 텍스트 프리팹
    public RectTransform gaugeRectTransform; // 게이지 UI의 RectTransform

    [Header("페이드 효과 설정")]
    public float fadeInDuration = 0.5f; // 페이드 인 시간
    public float fadeOutDuration = 0.3f; // 페이드 아웃 시간
    public AnimationCurve fadeInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // 페이드 인 커브
    public AnimationCurve fadeOutCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // 페이드 아웃 커브

    [Header("설정")]
    public KeyCode inputKey = KeyCode.D;
    public float gaugePerPress = 10f;
    public float gaugeDecreasePerSecond = 20f;
    public int totalSuccessNeeded = 3;

    [Header("키 입력 타이밍 설정")]
    public float inactivityTime = 3f; // 비활성 시간 (초)
    public int pressCountToShowProgress = 3; // 진행사항 표시까지 필요한 키 입력 횟수

    [Header("헛딧음 확률")]
    private int[] missChances = new int[] { 20, 15, 10 }; //20, 15, 10
    private int currentStage = 0;

    [Header("연출")]
    public CameraShake cameraShake;
    public AudioSource sfxMiss;
    public AudioSource sfxStep;
    public AudioSource sfxFinalStep;

    [Header("UI 연출 설정")]
    public float keyPromptBlinkSpeed = 2f;
    public Color normalGaugeColor = Color.green;
    public Color dangerGaugeColor = Color.red;

    [Header("캐릭터 이동 설정")]
    public Transform playerCharacter; // 플레이어 캐릭터 Transform
    public float stepDistance = 2f; // 한 번에 전진할 거리 (더 크게 설정)
    public float stepDuration = 0.8f; // 전진 애니메이션 시간 (더 길게 설정)
    public AnimationCurve stepCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // 이동 애니메이션 커브

    [Header("플레이어 컨트롤러 설정")]
    public PlayerCatMovement playerMovement; // 플레이어 이동 컨트롤러 참조

    [Header("다이얼로그 설정")]
    public string[] stageDialogueIDs = new string[] {
        "walking_stage1", // 첫 번째 성공 시 다이얼로그
        "walking_stage2", // 두 번째 성공 시 다이얼로그
        "walking_stage3"  // 세 번째 성공 시 다이얼로그
    };
    public string finalDialogueID = "walking_complete"; // 최종 완료 시 다이얼로그

    private float currentGauge = 0f;
    private bool isActive = false;
    private int successCount = 0;
    private Coroutine gaugeCoroutine;
    private Coroutine uiEffectCoroutine;
    private Coroutine inactivityCoroutine;
    private Coroutine fadeCoroutine; // 페이드 효과 코루틴
    private bool isPlayingDialogue = false; // 다이얼로그 재생 중인지 확인

    // 키 입력 추적 변수들
    private float lastKeyPressTime;
    private int keyPressCountSinceInactivity = 0;
    private bool isShowingProgressText = true; // 현재 진행사항을 보여주고 있는지

    private void Start()
    {
        // CanvasGroup 자동 설정
        if (miniGameCanvasGroup == null && miniGameUI != null)
        {
            miniGameCanvasGroup = miniGameUI.GetComponent<CanvasGroup>();
            if (miniGameCanvasGroup == null)
            {
                miniGameCanvasGroup = miniGameUI.AddComponent<CanvasGroup>();
            }
        }

        // 플레이어 컨트롤러 자동 찾기 (할당되지 않은 경우)
        if (playerMovement == null)
        {
            playerMovement = FindObjectOfType<PlayerCatMovement>();
        }

        // 플레이어 캐릭터 Transform 자동 찾기 (할당되지 않은 경우)
        if (playerCharacter == null && playerMovement != null)
        {
            playerCharacter = playerMovement.transform;
        }

        // 초기 상태 설정 (UI는 투명하게)
        if (miniGameCanvasGroup != null)
        {
            miniGameCanvasGroup.alpha = 0f;
        }
        miniGameUI.SetActive(false);
    }

    void Update()
    {
        if (!isActive || isPlayingDialogue) return;

        if (Input.GetKeyDown(inputKey))
        {
            OnKeyPressed();
            TryStep();
        }
    }

    private void OnKeyPressed()
    {
        lastKeyPressTime = Time.time;

        // 진행사항이 표시되지 않는 상태에서 키를 눌렀을 때
        if (!isShowingProgressText)
        {
            keyPressCountSinceInactivity++;

            // 일정 횟수 이상 누르면 진행사항 다시 표시
            if (keyPressCountSinceInactivity >= pressCountToShowProgress)
            {
                ShowProgressText();
            }
        }

        // 비활성 타이머 재시작
        RestartInactivityTimer();
    }

    private void ShowProgressText()
    {
        isShowingProgressText = true;
        keyPressCountSinceInactivity = 0;
        UpdateKeyPromptText();
    }

    private void ShowInstructionText()
    {
        isShowingProgressText = false;
        keyPromptText.text = "[ D ] 키를 연타하세요!";
    }

    private void UpdateKeyPromptText()
    {
        if (isShowingProgressText)
        {
            keyPromptText.text = $"발걸음 연습 중... ({successCount}/{totalSuccessNeeded})";
        }
        else
        {
            keyPromptText.text = "[ D ] 키를 연타하세요!";
        }
    }

    private void RestartInactivityTimer()
    {
        // 기존 비활성 코루틴 정지
        if (inactivityCoroutine != null)
        {
            StopCoroutine(inactivityCoroutine);
        }

        // 새로운 비활성 타이머 시작
        inactivityCoroutine = StartCoroutine(InactivityTimer());
    }

    private IEnumerator InactivityTimer()
    {
        yield return new WaitForSeconds(inactivityTime);

        // 일정 시간 동안 키를 누르지 않았으면 안내 텍스트로 변경
        if (Time.time - lastKeyPressTime >= inactivityTime && !isPlayingDialogue)
        {
            ShowInstructionText();
        }
    }

    public void StartMiniGame()
    {
        isActive = true;
        currentGauge = 0f;
        successCount = 0;
        currentStage = 0;
        isPlayingDialogue = false;
        // isShowingProgressText = true; // StartGameSequence에서 초기 설정하므로 주석 처리
        keyPressCountSinceInactivity = 0;
        lastKeyPressTime = Time.time;

        // 플레이어 상태 설정 (강제 웅크리기 및 입력 차단) - 즉시 적용
        if (playerMovement != null)
        {
            Debug.Log("[StruggleMiniGame] 플레이어 강제 웅크리기 활성화");
            playerMovement.SetMiniGameInputBlocked(true);
            playerMovement.ForceCrouch = true;
        }
        else
        {
            Debug.LogWarning("[StruggleMiniGame] PlayerCatMovement가 할당되지 않았습니다!");
        }

        // UI 활성화 (투명 상태)는 여기서 바로 진행하여 페이드 인 준비
        miniGameUI.SetActive(true);
        if (miniGameCanvasGroup != null)
        {
            miniGameCanvasGroup.alpha = 0f;
        }

        // UI 페이드 인과 게임 시퀀스를 동시에 시작
        StartCoroutine(ShowUIWithFadeInAndGameSequence());

        // 게이지 감소 코루틴 시작
        if (gaugeCoroutine != null) StopCoroutine(gaugeCoroutine);
        gaugeCoroutine = StartCoroutine(DecreaseGaugeOverTime());

        // UI 효과 코루틴 시작
        if (uiEffectCoroutine != null) StopCoroutine(uiEffectCoroutine);
        uiEffectCoroutine = StartCoroutine(BlinkKeyPrompt());

        // 비활성 타이머 시작
        RestartInactivityTimer();
    }

    IEnumerator ShowUIWithFadeInAndGameSequence()
    {
        // UI 페이드 인 효과 먼저 시작
        yield return StartCoroutine(FadeInUI());

        // 페이드 인 완료 후 게임 시작 연출
        yield return StartCoroutine(StartGameSequence());
    }

    IEnumerator FadeInUI()
    {
        if (miniGameCanvasGroup == null) yield break;

        // 기존 페이드 코루틴 정지
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(FadeCoroutine(0f, 1f, fadeInDuration, fadeInCurve));
        yield return fadeCoroutine;
    }

    IEnumerator FadeOutUI()
    {
        if (miniGameCanvasGroup == null) yield break;

        // 기존 페이드 코루틴 정지
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(FadeCoroutine(1f, 0f, fadeOutDuration, fadeOutCurve));
        yield return fadeCoroutine;
    }

    IEnumerator FadeCoroutine(float startAlpha, float endAlpha, float duration, AnimationCurve curve)
    {
        if (miniGameCanvasGroup == null) yield break;

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;
            float curveValue = curve.Evaluate(progress);

            miniGameCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, curveValue);

            yield return null;
        }

        miniGameCanvasGroup.alpha = endAlpha;
    }

    IEnumerator StartGameSequence()
    {
        gaugeFillImage.fillAmount = 0f;
        keyPromptText.text = "다리에 힘이 없어..";

        // 페이드 인 완료 후 1.5초 대기
        yield return new WaitForSeconds(1.5f);

        // D키 UI 표시
        keyPromptText.text = "[ D ] 키를 연타하세요!";
        isShowingProgressText = false; // 초기에는 안내 텍스트 표시
    }

    void TryStep()
    {
        // 현재 단계의 헛딧음 확률 체크
        int currentMissChance = missChances[Mathf.Min(currentStage, missChances.Length - 1)];

        if (Random.Range(0, 100) < currentMissChance)
        {
            MissStep();
        }
        else
        {
            SuccessfulPress();
        }
    }

    void SuccessfulPress()
    {
        currentGauge += gaugePerPress;
        currentGauge = Mathf.Min(currentGauge, 100f); // 100% 초과 방지

        // 게이지 색상 변경 (위험 구간에서는 빨간색)
        if (currentGauge < 30f)
        {
            gaugeFillImage.color = Color.Lerp(dangerGaugeColor, normalGaugeColor, currentGauge / 30f);
        }
        else
        {
            gaugeFillImage.color = normalGaugeColor;
        }

        gaugeFillImage.fillAmount = currentGauge / 100f;

        // 100% 도달 시 성공 처리
        if (currentGauge >= 100f)
        {
            OnStepSuccess();
        }
    }

    void MissStep()
    {
        currentGauge = Mathf.Max(0, currentGauge - 10f);
        gaugeFillImage.fillAmount = currentGauge / 100f;
        gaugeFillImage.color = dangerGaugeColor;

        // 헛딧음 연출
        StartCoroutine(MissStepEffect());

        // 효과음 재생
        if (sfxMiss != null) sfxMiss.Play();

        // 카메라 흔들림과 블러 효과
        if (cameraShake != null)
        {
            cameraShake.Shake(0.4f, 0.25f);
        }
    }

    IEnumerator MissStepEffect()
    {
        TextMeshProUGUI missTextInstance = Instantiate(missTextPrefab, gaugeRectTransform);
        missTextInstance.text = "헛딧음!";
        missTextInstance.color = Color.white;

        float fill = gaugeFillImage.fillAmount; // 0~1 사이의 값

        float angle = -fill * 360f;
        float radians = (angle + 90f) * Mathf.Deg2Rad;

        // 반지름 설정
        float radius = gaugeRectTransform.rect.width * 0.8f;

        // 게이지 중심점
        Vector2 center = gaugeFillImage.rectTransform.localPosition;

        // 정확한 위치 계산
        Vector2 offset = new Vector2(
            Mathf.Cos(radians) * radius,
            Mathf.Sin(radians) * radius
        );
        Vector2 localPos = center + offset;

        missTextInstance.rectTransform.localPosition = localPos;

        // 기존 애니메이션 효과
        float duration = 1.0f;
        Vector3 startPosition = missTextInstance.rectTransform.position;
        Vector3 endPosition = startPosition + new Vector3(0, 50f, 0);
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = timer / duration;
            missTextInstance.rectTransform.position = Vector3.Lerp(startPosition, endPosition, progress);
            Color currentColor = missTextInstance.color;
            currentColor.a = Mathf.Lerp(1f, 0f, progress);
            missTextInstance.color = currentColor;
            yield return null;
        }

        Destroy(missTextInstance.gameObject);
        UpdateKeyPromptText();
    }


    void OnStepSuccess()
    {
        Debug.Log($"[StruggleMiniGame] 단계 성공! 현재 단계: {successCount + 1}/{totalSuccessNeeded}");

        successCount++;
        currentStage++;

        // 성공 효과음
        if (sfxStep != null) sfxStep.Play();

        // 전진 모션과 다이얼로그 재생
        StartCoroutine(HandleStepSuccessSequence());
    }

    IEnumerator HandleStepSuccessSequence()
    {
        isPlayingDialogue = true; // 입력 차단

        // 비활성 타이머 정지 (다이얼로그 중에는 타이머 작동 안함)
        if (inactivityCoroutine != null)
        {
            StopCoroutine(inactivityCoroutine);
        }

        Debug.Log("[StruggleMiniGame] 전진 애니메이션 시작");

        // 전진 모션 실행 (웅크린 상태 유지하며)
        if (playerCharacter != null)
        {
            yield return StartCoroutine(MoveCharacterForwardCrouching());
        }
        else
        {
            Debug.LogWarning("[StruggleMiniGame] playerCharacter가 할당되지 않았습니다!");
        }

        // 단계 완료 이벤트 호출
        InvokeStageComplete(successCount);

        // 최종 성공 체크
        if (successCount >= totalSuccessNeeded)
        {
            Debug.Log("[StruggleMiniGame] 모든 단계 완료!");

            // 최종 완료 다이얼로그 재생
            if (!string.IsNullOrEmpty(finalDialogueID))
            {
                DialogueManager.Instance.StartDialogue(finalDialogueID);
                yield return StartCoroutine(WaitForDialogueEnd());
            }

            FinishMiniGame();
        }
        else
        {
            // 단계별 다이얼로그 재생
            int dialogueIndex = successCount - 1; // successCount는 1부터 시작하므로
            if (dialogueIndex >= 0 && dialogueIndex < stageDialogueIDs.Length &&
                !string.IsNullOrEmpty(stageDialogueIDs[dialogueIndex]))
            {
                DialogueManager.Instance.StartDialogue(stageDialogueIDs[dialogueIndex]);
                yield return StartCoroutine(WaitForDialogueEnd());
            }

            // 다음 단계 준비
            currentGauge = 0f;
            gaugeFillImage.fillAmount = 0f;
            gaugeFillImage.color = normalGaugeColor;

            // 진행 상황 업데이트 (성공 후에는 항상 진행사항 표시)
            isShowingProgressText = true;
            keyPressCountSinceInactivity = 0;
            UpdateKeyPromptText();
        }

        isPlayingDialogue = false; // 입력 재개
        lastKeyPressTime = Time.time; // 시간 갱신
        RestartInactivityTimer(); // 비활성 타이머 재시작
    }

    IEnumerator MoveCharacterForwardCrouching()
    {
        if (playerCharacter == null)
        {
            Debug.LogWarning("[StruggleMiniGame] playerCharacter가 null입니다!");
            yield break;
        }

        Vector3 startPosition = playerCharacter.position;
        Vector3 targetPosition = startPosition + Vector3.right * stepDistance; // X축 방향으로 이동

        // 전진 중 웅크리기 이동 애니메이션 활성화
        if (playerMovement != null)
        {
            playerMovement.SetCrouchMovingState(true);
        }

        float elapsedTime = 0f;

        while (elapsedTime < stepDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / stepDuration;
            float curveValue = stepCurve.Evaluate(progress);

            Vector3 currentPos = Vector3.Lerp(startPosition, targetPosition, curveValue);
            playerCharacter.position = currentPos;

            // 이동 중임을 지속적으로 알림 (애니메이션 유지를 위해)
            if (playerMovement != null)
            {
                playerMovement.SetCrouchMovingState(true);
            }

            yield return null;
        }

        playerCharacter.position = targetPosition;

        // 전진 완료 후 웅크리기 이동 애니메이션 중지
        if (playerMovement != null)
        {
            playerMovement.SetCrouchMovingState(false);
        }

    }

    IEnumerator WaitForDialogueEnd()
    {
        if (DialogueManager.Instance == null)
        {
            Debug.LogWarning("[StruggleMiniGame] DialogueManager.Instance가 null입니다!");
            yield break;
        }

        // 다이얼로그가 시작될 때까지 대기
        yield return new WaitUntil(() => DialogueManager.Instance.isDialogueActive);
        // 다이얼로그가 끝날 때까지 대기
        yield return new WaitUntil(() => !DialogueManager.Instance.isDialogueActive);
    }

    void FinishMiniGame()
    {
        Debug.Log("[StruggleMiniGame] 미니게임 완료");

        isActive = false;
        cameraShake.enabled = false;

        keyPromptText.text = "이제 걸을 수 있을 것 같아";

        // 모든 코루틴 정지
        if (gaugeCoroutine != null) StopCoroutine(gaugeCoroutine);
        if (uiEffectCoroutine != null) StopCoroutine(uiEffectCoroutine);
        if (inactivityCoroutine != null) StopCoroutine(inactivityCoroutine);

        // 최종 성공 연출
        StartCoroutine(FinalSuccessSequence());
    }

    IEnumerator FinalSuccessSequence()
    {
        // 최종 효과음
        if (sfxFinalStep != null) sfxFinalStep.Play();

        yield return new WaitForSeconds(1f);

        // UI 페이드 아웃 효과와 함께 비활성화
        yield return StartCoroutine(HideUIWithFadeOut());

        // 플레이어 상태 복원 (미니게임 완전 종료 후)
        if (playerMovement != null)
        {
            Debug.Log("[StruggleMiniGame] 플레이어 상태 복원 - 웅크리기 해제");
            playerMovement.SetMiniGameInputBlocked(false);
            playerMovement.ForceCrouch = false;
            playerMovement.SetCrouchMovingState(false);
        }

        Debug.Log("걷기 해방 완료!");

        // 미니게임 완료 이벤트 호출
        InvokeMiniGameComplete();
    }

    IEnumerator HideUIWithFadeOut()
    {
        // 페이드 아웃 효과
        yield return StartCoroutine(FadeOutUI());

        // UI 비활성화
        miniGameUI.SetActive(false);
    }

    IEnumerator DecreaseGaugeOverTime()
    {
        float interval = 0.1f; // 0.1초마다 실행
        float decreasePerTick = gaugeDecreasePerSecond * interval;

        while (isActive)
        {
            yield return new WaitForSeconds(interval);

            if (currentGauge > 0 && !isPlayingDialogue) // 다이얼로그 중에는 게이지 감소 정지
            {
                currentGauge = Mathf.Max(0f, currentGauge - decreasePerTick);
                gaugeFillImage.fillAmount = currentGauge / 100f;

                // 게이지가 낮을 때 색상 변경
                if (currentGauge < 30f)
                {
                    gaugeFillImage.color = Color.Lerp(normalGaugeColor, dangerGaugeColor, 1 - (currentGauge / 30f));
                }
            }
        }
    }

    IEnumerator BlinkKeyPrompt()
    {
        while (isActive)
        {
            if (!isPlayingDialogue) // 다이얼로그 중에는 깜빡임 정지
            {
                // 깜빡이는 효과
                float alpha = Mathf.PingPong(Time.time * keyPromptBlinkSpeed, 1f);
                Color color = keyPromptText.color;
                color.a = 0.5f + (alpha * 0.5f); // 0.5 ~ 1.0 사이로 알파값 조정
                keyPromptText.color = color;
            }

            yield return null;
        }

        // 종료 시 원래 색상으로 복원
        Color finalColor = keyPromptText.color;
        finalColor.a = 1f;
        keyPromptText.color = finalColor;
    }

    // 외부에서 미니게임 강제 종료 시 사용
    public void ForceEndMiniGame()
    {
        Debug.Log("[StruggleMiniGame] 미니게임 강제 종료");

        isActive = false;
        isPlayingDialogue = false;

        // 플레이어 상태 복원
        if (playerMovement != null)
        {
            playerMovement.SetMiniGameInputBlocked(false);
            playerMovement.ForceCrouch = false;
            playerMovement.SetCrouchMovingState(false);
        }

        if (gaugeCoroutine != null) StopCoroutine(gaugeCoroutine);
        if (uiEffectCoroutine != null) StopCoroutine(uiEffectCoroutine);
        if (inactivityCoroutine != null) StopCoroutine(inactivityCoroutine);
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);

        // 강제 종료 시에도 페이드 아웃 효과 적용
        StartCoroutine(ForceEndWithFadeOut());
    }

    IEnumerator ForceEndWithFadeOut()
    {
        yield return StartCoroutine(HideUIWithFadeOut());
    }

    // 현재 진행 상황 확인용
    public float GetProgress()
    {
        return (float)successCount / totalSuccessNeeded;
    }

    // 단계별 성공 시 호출되는 이벤트 (필요시 사용)
    public System.Action<int> OnStageComplete;
    public System.Action OnMiniGameComplete;

    // 이벤트 호출 메서드들
    private void InvokeStageComplete(int stage)
    {
        OnStageComplete?.Invoke(stage);
    }

    private void InvokeMiniGameComplete()
    {
        OnMiniGameComplete?.Invoke();
    }
}