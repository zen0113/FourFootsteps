using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 단계별 이미지 변화가 있는 어둠 걷어내기 연타 미니게임
/// 40, 60, 80, 100번째에 이미지가 변화하며 플레이어 점프 차단
/// </summary>
public class DarknessTapMinigame : MonoBehaviour
{
    [Header("게임 설정")]
    [SerializeField] private float tapGoal = 100f; // 연타 목표치 (100번)
    [SerializeField] private float perTapIncrease = 1f; // 한 번 누를 때 증가량 (정확히 1씩)
    [SerializeField] private KeyCode tapKey = KeyCode.Space; // 연타 키

    [Header("단계별 이미지 설정")]
    [SerializeField] private Image stageImage;// 단계별로 변화할 이미지
    [SerializeField] private Sprite[] stageSprites; // 각 단계별 스프라이트 (4개: 40,60,80,100번용)
    [SerializeField] private int[] stageMilestones = { 20, 30, 40, 50 }; // 이미지 변화 시점

    [Header("UI 요소")]
    [SerializeField] private TextMeshProUGUI instructionText;  // 안내 텍스트
    [SerializeField] private Canvas gameCanvas; // 게임 캔버스

    [Header("텍스트 애니메이션")]
    [SerializeField] private float blinkSpeed = 1f;         // 깜빡임 속도
    [SerializeField] private float minAlpha = 0.3f;         // 최소 투명도
    [SerializeField] private float maxAlpha = 1f;           // 최대 투명도

    [Header("플레이어 제어")]
    [SerializeField] private PlayerCatMovement playerController;  // 플레이어 컨트롤러 참조

    [Header("사운드 (선택사항)")]
    [SerializeField] private AudioSource audioSource;       // 오디오 소스
    [SerializeField] private AudioClip tapSound;            // 연타 사운드
    [SerializeField] private AudioClip gameCompleteSound;   // 게임 완료 사운드 (기존 stageChangeSound 대체)

    [Header("게임 완료 후 상태 설정")]
    [SerializeField] private bool setCrouchAfterComplete = true;
    [SerializeField] private bool keepCrouchForNext = true;

    // 게임 상태
    private float tapProgress = 0f;
    private bool isGameActive = false;
    private bool isGameCompleted = false;
    private int currentStage = -1;
    private bool isPaused = false;

    // 텍스트 애니메이션
    private Coroutine textBlinkCoroutine;

    // 플레이어 입력 차단
    private bool originalCanMoving;

    // 이벤트
    public System.Action OnMinigameComplete;

    private void Start()
    {
        SetupUI();
        StartMinigame();
    }

    private void SetupUI()
    {
        // 안내 텍스트 설정
        if (instructionText != null)
        {
            instructionText.text = "연타 (Space) 하세요!";
        }

        // 첫 번째 단계 이미지 설정 (검은 배경으로 시작)
        if (stageImage != null)
        {
            stageImage.sprite = null;
            stageImage.color = Color.black;
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        if (playerController == null)
        {
            playerController = FindObjectOfType<PlayerCatMovement>();
        }
    }

    /// <summary>
    /// 미니게임 시작
    /// </summary>
    public void StartMinigame()
    {
        isGameActive = true;
        isPaused = false;
        isGameCompleted = false;
        tapProgress = 0f;
        currentStage = -1;

        BlockPlayerInput(true);

        // 텍스트를 다시 활성화하고 깜빡임 시작
        if (instructionText != null)
        {
            instructionText.gameObject.SetActive(true);
        }

        if (textBlinkCoroutine != null)
        {
            StopCoroutine(textBlinkCoroutine);
        }
        textBlinkCoroutine = StartCoroutine(BlinkText());

        Debug.Log("단계별 어둠 걷어내기 미니게임 시작!");
    }

    /// <summary>
    /// 미니게임 일시 정지
    /// </summary>
    public void PauseMinigame()
    {
        isPaused = true;
        if (textBlinkCoroutine != null)
        {
            StopCoroutine(textBlinkCoroutine);
        }
        // 추가: 텍스트 비활성화
        if (instructionText != null)
        {
            instructionText.gameObject.SetActive(false);
        }
        Debug.Log("미니게임 일시 정지됨");
    }

    /// <summary>
    /// 미니게임 재개
    /// </summary>
    public void ResumeMinigame()
    {
        isPaused = false;
        // 추가: 텍스트 활성화
        if (instructionText != null)
        {
            instructionText.gameObject.SetActive(true);
        }

        if (textBlinkCoroutine != null)
        {
            StopCoroutine(textBlinkCoroutine);
        }
        textBlinkCoroutine = StartCoroutine(BlinkText());
        Debug.Log("미니게임 재개됨");
    }

    private void BlockPlayerInput(bool block)
    {
        if (GameManager.Instance != null)
        {
            if (block)
            {
                originalCanMoving = (bool)GameManager.Instance.GetVariable("CanMoving");
                GameManager.Instance.SetVariable("CanMoving", false);
            }
            else
            {
                GameManager.Instance.SetVariable("CanMoving", originalCanMoving);
            }
        }
    }

    private void Update()
    {
        if (!isGameActive || isGameCompleted || isPaused) return;

        if (Input.GetKeyDown(tapKey))
        {
            ProcessTap();
        }
    }

    private void ProcessTap()
    {
        tapProgress += perTapIncrease;

        PlayTapSound();

        CheckStageChange();

        if (tapProgress >= 90f)
        {
            UpdateTextFadeOut();
        }

        if (tapProgress >= tapGoal)
        {
            CompleteMinigame();
        }

        Debug.Log($"연타 진행도: {tapProgress:F0}/{tapGoal} - 현재 단계: {currentStage}");
    }

    private void CheckStageChange()
    {
        for (int i = 0; i < stageMilestones.Length; i++)
        {
            if (tapProgress >= stageMilestones[i] && currentStage < i)
            {
                Debug.Log($"단계 변화 감지: {tapProgress}번째, 목표: {stageMilestones[i]}번째");
                ChangeToStage(i);
                break;
            }
        }
    }

    private void ChangeToStage(int stage)
    {
        if (stage >= stageSprites.Length)
        {
            Debug.LogError($"단계 {stage}이 배열 범위({stageSprites.Length})를 벗어났습니다!");
            return;
        }

        Debug.Log($"단계 변경: {currentStage} → {stage}");
        currentStage = stage;

        if (stageImage != null && stageSprites[stage] != null)
        {
            stageImage.sprite = stageSprites[stage];
            stageImage.color = Color.white;

            Debug.Log($"✅ 단계 {stage + 1} 이미지로 변경 성공! ({stageMilestones[stage]}번째 연타) - 스프라이트: {stageSprites[stage].name}");
        }
        else
        {
            Debug.LogError($"❌ 단계 변경 실패! StageImage={stageImage != null}, Sprite[{stage}]={stageSprites[stage] != null}");
            if (stageImage == null) Debug.LogError("StageImage가 null입니다!");
            if (stageSprites[stage] == null) Debug.LogError($"StageSprites[{stage}]이 null입니다!");
        }
    }

    private void UpdateTextFadeOut()
    {
        if (instructionText == null) return;

        float fadeProgress = (tapProgress - 90f) / 10f;
        float textAlpha = Mathf.Lerp(1f, 0f, fadeProgress);

        Color textColor = instructionText.color;
        textColor.a = textAlpha;
        instructionText.color = textColor;
    }

    private void CompleteMinigame()
    {
        isGameCompleted = true;
        isGameActive = false;

        if (textBlinkCoroutine != null)
        {
            StopCoroutine(textBlinkCoroutine);
        }

        if (instructionText != null)
        {
            instructionText.gameObject.SetActive(false);
        }

        // 게임 완료 사운드 재생
        PlayGameCompleteSound();

        Debug.Log("미니게임 완료!");

        OnMinigameComplete?.Invoke();

        StartCoroutine(ShowFinalImageAndCleanup());
    }

    private IEnumerator ShowFinalImageAndCleanup()
    {
        yield return new WaitForSeconds(2f);

        if (setCrouchAfterComplete)
        {
            SetPlayerCrouchState(true);
        }

        if (!keepCrouchForNext)
        {
            BlockPlayerInput(false);
        }
        else
        {
            RestoreInputWithCrouch();
        }

        if (gameCanvas != null)
        {
            gameCanvas.gameObject.SetActive(false);
        }

        Debug.Log("미니게임 UI 정리 완료! 웅크리기 상태 유지됨");
    }

    private void SetPlayerCrouchState(bool crouch)
    {
        if (playerController != null && crouch)
        {
            Debug.Log("[DarknessTapMinigame] 플레이어 강제 웅크리기 활성화");
            playerController.ForceCrouch = true;
            playerController.SetMiniGameInputBlocked(false);
        }
        else if (playerController != null && !crouch)
        {
            Debug.Log("[DarknessTapMinigame] 플레이어 웅크리기 해제");
            playerController.ForceCrouch = false;
            playerController.SetMiniGameInputBlocked(false);
        }
    }

    private void RestoreInputWithCrouch()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetVariable("CanMoving", originalCanMoving);
        }

        if (playerController != null)
        {
            playerController.SetMiniGameInputBlocked(false);
            playerController.ForceCrouch = true;

            bool isMovingOnInput = Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.01f;
            playerController.SetCrouchMovingState(isMovingOnInput);

            Debug.Log("[DarknessTapMinigame] 입력 복구 완료, 웅크리기 상태 유지");
        }
    }

    private IEnumerator BlinkText()
    {
        if (instructionText == null) yield break;

        Color originalColor = instructionText.color;

        while (isGameActive && !isGameCompleted && tapProgress < 90f && !isPaused)
        {
            float elapsedTime = 0f;
            while (elapsedTime < blinkSpeed && tapProgress < 90f && !isPaused)
            {
                elapsedTime += Time.deltaTime;
                float alpha = Mathf.Lerp(maxAlpha, minAlpha, elapsedTime / blinkSpeed);

                Color newColor = originalColor;
                newColor.a = alpha;
                instructionText.color = newColor;

                yield return null;
            }

            elapsedTime = 0f;
            while (elapsedTime < blinkSpeed && tapProgress < 90f && !isPaused)
            {
                elapsedTime += Time.deltaTime;
                float alpha = Mathf.Lerp(minAlpha, maxAlpha, elapsedTime / blinkSpeed);

                Color newColor = originalColor;
                newColor.a = alpha;
                instructionText.color = newColor;

                yield return null;
            }
        }
    }

    private void PlayTapSound()
    {
        if (audioSource != null && tapSound != null)
        {
            audioSource.PlayOneShot(tapSound, 0.3f);
        }
    }

    private void PlayGameCompleteSound()
    {
        if (audioSource != null && gameCompleteSound != null)
        {
            audioSource.PlayOneShot(gameCompleteSound, 0.7f);
        }
    }

    /// <summary>
    /// 미니게임 강제 정지
    /// </summary>
    public void StopMinigame()
    {
        isGameActive = false;
        isPaused = true;

        if (textBlinkCoroutine != null)
        {
            StopCoroutine(textBlinkCoroutine);
        }

        // 텍스트 비활성화
        if (instructionText != null)
        {
            instructionText.gameObject.SetActive(false);
        }

        BlockPlayerInput(false);
    }

    public void ResetProgress()
    {
        tapProgress = 0f;
        currentStage = -1;
        isGameCompleted = false;
        isPaused = false;

        if (stageImage != null)
        {
            stageImage.sprite = null;
            stageImage.color = Color.black;
        }

        if (instructionText != null)
        {
            instructionText.gameObject.SetActive(true);
            Color textColor = instructionText.color;
            textColor.a = 1f;
            instructionText.color = textColor;
        }
    }

    public float GetProgress()
    {
        return tapProgress / tapGoal;
    }

    public int GetCurrentStage()
    {
        return currentStage;
    }

    public bool IsGameActive()
    {
        return isGameActive && !isPaused;
    }

    public bool IsGameCompleted()
    {
        return isGameCompleted;
    }
}