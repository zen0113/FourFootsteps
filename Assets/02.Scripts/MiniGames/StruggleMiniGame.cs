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
    public AnimationCurve fadeInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); 
    public AnimationCurve fadeOutCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("설정")] 
    public KeyCode inputKey = KeyCode.D; // 입력 키
    public float gaugePerPress = 10f; // 키 입력당 게이지 증가량
    public float gaugeDecreasePerSecond = 20f; // 초당 게이지 감소량
    public int totalSuccessNeeded = 3; // 필요한 총 성공 횟수

    [Header("키 입력 타이밍 설정")]
    public float inactivityTime = 3f; // 비활성 시간 (초)
    public int pressCountToShowProgress = 3; // 진행사항 표시까지 필요한 키 입력 횟수

    [Header("헛딧음 확률")]
    private int[] missChances = new int[] { 20, 15, 10 }; //20, 15, 10
    private int currentStage = 0; // 현재 단계

    [Header("연출")] 
    public CameraShake cameraShake; // 카메라 흔들림
    public AudioSource sfxMiss; // 헛딧음 효과음
    public AudioSource sfxStep; // 발걸음 성공 효과음
    public AudioSource sfxFinalStep; // 최종 성공 효과음

    [Header("UI 연출 설정")] 
    public float keyPromptBlinkSpeed = 2f; // 키 프롬프트 깜빡임 속도
    public Color normalGaugeColor = Color.green; // 일반 게이지 색상
    public Color dangerGaugeColor = Color.red; // 위험 게이지 색상

    [Header("캐릭터 이동 설정")]
    public Transform playerCharacter; 
    public float stepDistance = 2f; 
    public float stepDuration = 0.8f; 
    public AnimationCurve stepCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); 

    [Header("플레이어 컨트롤러 설정")]
    public PlayerCatMovement playerMovement; 

    [Header("다이얼로그 설정")]
    public string[] stageDialogueIDs = new string[] {
        "walking_stage1", // 첫 번째 성공 시 다이얼로그
        "walking_stage2", // 두 번째 성공 시 다이얼로그
        "walking_stage3"  // 세 번째 성공 시 다이얼로그
    };
    public string finalDialogueID = "walking_complete"; // 최종 완료 시 다이얼로그

    private float currentGauge = 0f; // 현재 게이지 값
    private bool isActive = false; // 미니게임 활성화 여부
    private int successCount = 0; // 성공 횟수
    private Coroutine gaugeCoroutine; // 게이지 감소 코루틴
    private Coroutine uiEffectCoroutine; // UI 효과 코루틴 (깜빡임 등)
    private Coroutine inactivityCoroutine; // 비활성 타이머 코루틴
    private Coroutine fadeCoroutine; // 페이드 효과 코루틴
    private bool isPlayingDialogue = false; // 다이얼로그 재생 중인지 확인

    // 키 입력 추적 변수들
    private float lastKeyPressTime; // 마지막 키 입력 시간
    private int keyPressCountSinceInactivity = 0; // 비활성 상태 이후 키 입력 횟수
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
        // 미니게임이 활성화되지 않았거나 다이얼로그 재생 중이면 업데이트 중단
        if (!isActive || isPlayingDialogue) return;

        // 입력 키가 눌렸을 때
        if (Input.GetKeyDown(inputKey))
        {
            OnKeyPressed();
            TryStep();
        }
    }

    private void OnKeyPressed()
    {
        lastKeyPressTime = Time.time; // 마지막 키 입력 시간 갱신

        // 진행사항이 표시되지 않는 상태에서 키를 눌렀을 때
        if (!isShowingProgressText)
        {
            keyPressCountSinceInactivity++; // 비활성 후 키 입력 횟수 증가

            // 일정 횟수 이상 누르면 진행사항 다시 표시
            if (keyPressCountSinceInactivity >= pressCountToShowProgress)
            {
                ShowProgressText(); // 진행사항 텍스트 표시
            }
        }

        // 비활성 타이머 재시작
        RestartInactivityTimer();
    }

    private void ShowProgressText()
    {
        isShowingProgressText = true; // 진행사항 텍스트 표시 활성화
        keyPressCountSinceInactivity = 0; // 키 입력 횟수 초기화
        UpdateKeyPromptText(); // 키 프롬프트 텍스트 업데이트
    }

    private void ShowInstructionText()
    {
        isShowingProgressText = false; // 진행사항 텍스트 표시 비활성화
        keyPromptText.text = "[ D ] 키를 연타하세요!"; // 안내 텍스트 설정
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
        isActive = true; // 미니게임 활성화
        currentGauge = 0f; // 게이지 초기화
        successCount = 0; // 성공 횟수 초기화
        currentStage = 0; // 현재 단계 초기화
        isPlayingDialogue = false; // 다이얼로그 재생 상태 초기화
        keyPressCountSinceInactivity = 0; // 키 입력 횟수 초기화
        lastKeyPressTime = Time.time; // 마지막 키 입력 시간 초기화

        // 플레이어 상태 설정 (강제 웅크리기 및 입력 차단) - 즉시 적용
        if (playerMovement != null)
        {
            Debug.Log("[StruggleMiniGame] 플레이어 강제 웅크리기 및 점프 차단 활성화");
            playerMovement.SetMiniGameInputBlocked(true); // 미니게임 중 플레이어 입력 차단
            playerMovement.ForceCrouch = true; // 플레이어 강제 웅크리기

            // 미니게임이 진행되는 동안 Space 키(점프) 입력을 확실하게 차단
            playerMovement.IsJumpingBlocked = true;
        }
        else
        {
            Debug.LogWarning("[StruggleMiniGame] PlayerCatMovement가 할당되지 않았습니다!");
        }

        

        // UI 활성화 (투명 상태)는 여기서 바로 진행하여 페이드 인 준비
        miniGameUI.SetActive(true);
        if (miniGameCanvasGroup != null)
        {
            miniGameCanvasGroup.alpha = 0f; // UI를 완전히 투명하게 설정
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
        gaugeFillImage.fillAmount = 0f; // 게이지를 0으로 설정
        keyPromptText.text = "다리에 힘이 없어.."; // 초기 안내 텍스트

        // 페이드 인 완료 후 1.5초 대기
        yield return new WaitForSeconds(1.5f);

        // D키 UI 표시
        keyPromptText.text = "[ D ] 키를 연타하세요!"; // 키 입력 안내 텍스트
        isShowingProgressText = false; // 초기에는 안내 텍스트 표시
    }

    void TryStep()
    {
        // 현재 단계의 헛딧음 확률 체크
        int currentMissChance = missChances[Mathf.Min(currentStage, missChances.Length - 1)];

        if (Random.Range(0, 100) < currentMissChance) // 랜덤 확률로 헛딧음 발생 여부 결정
        {
            MissStep(); // 헛딧음 처리
        }
        else
        {
            SuccessfulPress(); // 성공적인 키 입력 처리
        }
    }

    void SuccessfulPress()
    {
        currentGauge += gaugePerPress; // 게이지 증가
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

        gaugeFillImage.fillAmount = currentGauge / 100f; // 게이지 UI 업데이트

        // 100% 도달 시 성공 처리
        if (currentGauge >= 100f)
        {
            OnStepSuccess(); // 발걸음 성공 처리
        }
    }

    void MissStep()
    {
        currentGauge = Mathf.Max(0, currentGauge - 10f); // 게이지 감소
        gaugeFillImage.fillAmount = currentGauge / 100f; // 게이지 UI 업데이트
        gaugeFillImage.color = dangerGaugeColor; // 게이지 색상을 위험 색상으로 변경

        // 헛딧음 연출
        StartCoroutine(MissStepEffect());

        // 효과음 재생
        if (sfxMiss != null) sfxMiss.Play();

        // 카메라 흔들림 효과
        if (cameraShake != null)
        {
            // 쉐이크가 작동하려면 enabled가 true여야 합니다.
            if (!cameraShake.enabled)
            {
                cameraShake.enabled = true;
            }
            cameraShake.Shake(0.4f, 0.25f);
        }
    }

    IEnumerator MissStepEffect()
    {
        // "헛딧음!" 텍스트 프리팹 생성
        TextMeshProUGUI missTextInstance = Instantiate(missTextPrefab, gaugeRectTransform);
        missTextInstance.text = "헛딧음!";
        missTextInstance.color = Color.white;

        float fill = gaugeFillImage.fillAmount; // 0~1 사이의 값 (게이지 채움 비율)

        float angle = -fill * 360f; // 게이지 채움 비율에 따른 각도 계산
        float radians = (angle + 90f) * Mathf.Deg2Rad; // 라디안 값으로 변환

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

        missTextInstance.rectTransform.localPosition = localPos; // 텍스트 위치 설정

        // 기존 애니메이션 효과 (텍스트 위로 이동 및 투명도 감소)
        float duration = 1.0f; // 애니메이션 지속 시간
        Vector3 startPosition = missTextInstance.rectTransform.position;
        Vector3 endPosition = startPosition + new Vector3(0, 50f, 0); // 위로 50f 이동
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = timer / duration;
            missTextInstance.rectTransform.position = Vector3.Lerp(startPosition, endPosition, progress); // 위치 보간
            Color currentColor = missTextInstance.color;
            currentColor.a = Mathf.Lerp(1f, 0f, progress); // 투명도 보간
            missTextInstance.color = currentColor;
            yield return null;
        }

        Destroy(missTextInstance.gameObject); // 텍스트 게임 오브젝트 파괴
        UpdateKeyPromptText(); // 키 프롬프트 텍스트 업데이트
    }


    void OnStepSuccess()
    {
        Debug.Log($"[StruggleMiniGame] 단계 성공! 현재 단계: {successCount + 1}/{totalSuccessNeeded}");

        successCount++; // 성공 횟수 증가
        currentStage++; // 현재 단계 증가

        // 성공 효과음
        if (sfxStep != null) sfxStep.Play();

        // 전진 모션과 다이얼로그 재생
        StartCoroutine(HandleStepSuccessSequence());
    }

    IEnumerator HandleStepSuccessSequence()
    {
        isPlayingDialogue = true; // 다이얼로그 재생 중으로 설정하여 입력 차단

        if (playerMovement != null)
        {
            playerMovement.SetMiniGameInputBlocked(true);
        }

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

            StartCoroutine(HideUIWithFadeOut());

            // 최종 완료 다이얼로그를 먼저 재생합니다.
            if (!string.IsNullOrEmpty(finalDialogueID))
            {
                DialogueManager.Instance.StartDialogue(finalDialogueID);
                yield return StartCoroutine(WaitForDialogueEnd());
            }

            // 다이얼로그가 끝난 후 미니게임을 종료합니다.
            FinishMiniGame();
        }
        else
        {
            // 단계별 다이얼로그 재생
            int dialogueIndex = successCount - 1;
            if (dialogueIndex >= 0 && dialogueIndex < stageDialogueIDs.Length &&
                !string.IsNullOrEmpty(stageDialogueIDs[dialogueIndex]))
            {
                string currentDialogueID = stageDialogueIDs[dialogueIndex];

                if (currentDialogueID == "walking_stage3")
                {
                    if (gaugeFillImage != null) gaugeFillImage.gameObject.SetActive(false);
                    if (keyPromptText != null) keyPromptText.gameObject.SetActive(false);
                }

                DialogueManager.Instance.StartDialogue(currentDialogueID);
                yield return StartCoroutine(WaitForDialogueEnd());
            }

            if (playerMovement != null)
            {
                playerMovement.SetMiniGameInputBlocked(true);
            }

            // 다음 단계 준비
            currentGauge = 0f;
            gaugeFillImage.fillAmount = 0f;
            gaugeFillImage.color = normalGaugeColor;

            isShowingProgressText = true;
            keyPressCountSinceInactivity = 0;
            UpdateKeyPromptText();
        }

        isPlayingDialogue = false;
        lastKeyPressTime = Time.time;
        RestartInactivityTimer();
    }

    IEnumerator MoveCharacterForwardCrouching()
    {
        if (playerCharacter == null)
        {
            Debug.LogWarning("[StruggleMiniGame] playerCharacter가 null입니다!");
            yield break;
        }

        Vector3 startPosition = playerCharacter.position; // 시작 위치
        Vector3 targetPosition = startPosition + Vector3.right * stepDistance; // 목표 위치 (X축 방향으로 이동)

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
            float curveValue = stepCurve.Evaluate(progress); // 애니메이션 커브 적용

            Vector3 currentPos = Vector3.Lerp(startPosition, targetPosition, curveValue); // 위치 보간
            playerCharacter.position = currentPos;

            // 이동 중임을 지속적으로 알림 (애니메이션 유지를 위해)
            if (playerMovement != null)
            {
                playerMovement.SetCrouchMovingState(true);
            }

            yield return null;
        }

        playerCharacter.position = targetPosition; // 최종 위치 설정

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

        isActive = false; // 미니게임 비활성화

        // 모든 코루틴 정지
        if (gaugeCoroutine != null) StopCoroutine(gaugeCoroutine);
        if (uiEffectCoroutine != null) StopCoroutine(uiEffectCoroutine);
        if (inactivityCoroutine != null) StopCoroutine(inactivityCoroutine);

        // 최종 성공 연출
        StartCoroutine(FinalSuccessSequence());
    }

    IEnumerator FinalSuccessSequence()
    {
        // 이 시점은 마지막 다이얼로그가 끝난 직후이므로, 여기서 비활성화합니다.
        if (cameraShake != null)
        {
            Debug.Log("[StruggleMiniGame] 최종 다이얼로그 종료, 카메라 쉐이크를 비활성화합니다.");
            cameraShake.enabled = false;
        }

        // 최종 효과음
        if (sfxFinalStep != null) sfxFinalStep.Play();

        yield return new WaitForSeconds(0.2f); // 1초 대기


        // 플레이어 상태 복원 (미니게임 완전 종료 후)
        if (playerMovement != null)
        {
            Debug.Log("[StruggleMiniGame] 플레이어 상태 복원 - 웅크리기 및 점프 차단 해제");
            playerMovement.SetMiniGameInputBlocked(false); // 플레이어 입력 차단 해제
            playerMovement.ForceCrouch = false; // 강제 웅크리기 해제
            playerMovement.SetCrouchMovingState(false); // 웅크리기 이동 상태 해제

            // [수정] 미니게임이 끝났으므로 Space 키(점프) 입력을 다시 허용합니다.
            playerMovement.IsJumpingBlocked = false;
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
        //miniGameUI.SetActive(false);
    }

    IEnumerator DecreaseGaugeOverTime()
    {
        float interval = 0.1f; // 0.1초마다 실행
        float decreasePerTick = gaugeDecreasePerSecond * interval; // 틱당 감소량

        while (isActive) // 미니게임이 활성화된 동안 계속 실행
        {
            yield return new WaitForSeconds(interval);

            if (currentGauge > 0 && !isPlayingDialogue) // 다이얼로그 중에는 게이지 감소 정지
            {
                currentGauge = Mathf.Max(0f, currentGauge - decreasePerTick); // 게이지 감소 (0 미만으로 내려가지 않도록)
                gaugeFillImage.fillAmount = currentGauge / 100f; // 게이지 UI 업데이트

                // 게이지가 낮을 때 색상 변경 (위험 구간)
                if (currentGauge < 30f)
                {
                    gaugeFillImage.color = Color.Lerp(normalGaugeColor, dangerGaugeColor, 1 - (currentGauge / 30f));
                }
            }
        }
    }

    IEnumerator BlinkKeyPrompt()
    {
        while (isActive) // 미니게임이 활성화된 동안 계속 실행
        {
            if (!isPlayingDialogue) // 다이얼로그 중에는 깜빡임 정지
            {
                // 깜빡이는 효과
                float alpha = Mathf.PingPong(Time.time * keyPromptBlinkSpeed, 1f); // 알파값 깜빡임 효과
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

        isActive = false; // 미니게임 비활성화
        isPlayingDialogue = false; // 다이얼로그 재생 상태 초기화

        // 플레이어 상태 복원
        if (playerMovement != null)
        {
            playerMovement.SetMiniGameInputBlocked(false); // 플레이어 입력 차단 해제
            playerMovement.ForceCrouch = false; // 강제 웅크리기 해제
            playerMovement.SetCrouchMovingState(false); // 웅크리기 이동 상태 해제

            playerMovement.IsJumpingBlocked = false;
        }

        // 모든 코루틴 정지
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

    // 단계별 성공 시 호출되는 이벤트 
    public System.Action<int> OnStageComplete; // 각 단계 완료 시 호출되는 이벤트
    public System.Action OnMiniGameComplete; // 미니게임 전체 완료 시 호출되는 이벤트

    // 이벤트 호출 메서드들
    private void InvokeStageComplete(int stage)
    {
        OnStageComplete?.Invoke(stage); // OnStageComplete 이벤트 호출
    }

    private void InvokeMiniGameComplete()
    {
        OnMiniGameComplete?.Invoke(); // OnMiniGameComplete 이벤트 호출
    }
}