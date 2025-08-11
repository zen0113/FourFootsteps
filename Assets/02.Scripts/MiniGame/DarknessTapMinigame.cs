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
    [SerializeField] private float tapGoal = 100f;           // 연타 목표치 (100번)
    [SerializeField] private float perTapIncrease = 1f;      // 한 번 누를 때 증가량 (정확히 1씩)
    [SerializeField] private KeyCode tapKey = KeyCode.Space; // 연타 키
    
    [Header("단계별 이미지 설정")]
    [SerializeField] private Image stageImage;               // 단계별로 변화할 이미지
    [SerializeField] private Sprite[] stageSprites;          // 각 단계별 스프라이트 (4개: 40,60,80,100번용)
    [SerializeField] private int[] stageMilestones = {40, 60, 80, 100}; // 이미지 변화 시점
    
    [Header("UI 요소")]
    [SerializeField] private TextMeshProUGUI instructionText; // 안내 텍스트
    [SerializeField] private Canvas gameCanvas;              // 게임 캔버스
    
    [Header("텍스트 애니메이션")]
    [SerializeField] private float blinkSpeed = 1f;         // 깜빡임 속도
    [SerializeField] private float minAlpha = 0.3f;         // 최소 투명도
    [SerializeField] private float maxAlpha = 1f;           // 최대 투명도
    
    [Header("플레이어 제어")]
    [SerializeField] private PlayerCatMovement playerController; // 플레이어 컨트롤러 참조
    
    [Header("사운드 (선택사항)")]
    [SerializeField] private AudioSource audioSource;       // 오디오 소스
    [SerializeField] private AudioClip tapSound;            // 연타 사운드
    [SerializeField] private AudioClip stageChangeSound;    // 단계 변화 사운드
    
    // 게임 상태
    private float tapProgress = 0f;
    private bool isGameActive = false;
    private bool isGameCompleted = false;
    private int currentStage = -1; // -1로 시작해서 첫 번째 단계 변경이 가능하도록
    
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
    
    /// <summary>
    /// UI 초기 설정
    /// </summary>
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
            stageImage.sprite = null; // 처음에는 이미지 없음
            stageImage.color = Color.black; // 검은색 배경으로 시작
        }
        
        // 오디오 소스 자동 생성
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        
        // 플레이어 컨트롤러 자동 찾기
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
        isGameCompleted = false;
        tapProgress = 0f;
        currentStage = -1; // -1로 시작
        
        // 플레이어 이동 차단
        BlockPlayerInput(true);
        
        // 텍스트 깜빡임 시작
        if (textBlinkCoroutine != null)
        {
            StopCoroutine(textBlinkCoroutine);
        }
        textBlinkCoroutine = StartCoroutine(BlinkText());
        
        Debug.Log("단계별 어둠 걷어내기 미니게임 시작!");
    }
    
    /// <summary>
    /// 플레이어 입력 차단/해제
    /// </summary>
    private void BlockPlayerInput(bool block)
    {
        if (GameManager.Instance != null)
        {
            if (block)
            {
                // 현재 CanMoving 상태 저장 후 차단
                originalCanMoving = (bool)GameManager.Instance.GetVariable("CanMoving");
                GameManager.Instance.SetVariable("CanMoving", false);
            }
            else
            {
                // 원래 상태로 복구
                GameManager.Instance.SetVariable("CanMoving", originalCanMoving);
            }
        }
    }
    
    private void Update()
    {
        if (!isGameActive || isGameCompleted) return;
        
        // Space 키 입력 처리 (플레이어 점프는 차단됨)
        if (Input.GetKeyDown(tapKey))
        {
            ProcessTap();
        }
    }
    
    /// <summary>
    /// 연타 입력 처리
    /// </summary>
    private void ProcessTap()
    {
        tapProgress += perTapIncrease;
        
        // 사운드 재생
        PlayTapSound();
        
        // 단계 체크 및 이미지 변경
        CheckStageChange();
        
        // 90번째부터 텍스트 페이드아웃 시작
        if (tapProgress >= 90f)
        {
            UpdateTextFadeOut();
        }
        
        // 목표 달성 확인
        if (tapProgress >= tapGoal)
        {
            CompleteMinigame();
        }
        
        Debug.Log($"연타 진행도: {tapProgress:F0}/{tapGoal} - 현재 단계: {currentStage}");
    }
    
    /// <summary>
    /// 단계 변화 체크 및 이미지 변경
    /// </summary>
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
    
    /// <summary>
    /// 특정 단계로 변경
    /// </summary>
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
            stageImage.color = Color.white; // 이미지 보이게 함
            
            // 단계 변화 사운드 재생
            PlayStageChangeSound();
            
            Debug.Log($"✅ 단계 {stage + 1} 이미지로 변경 성공! ({stageMilestones[stage]}번째 연타) - 스프라이트: {stageSprites[stage].name}");
        }
        else
        {
            Debug.LogError($"❌ 단계 변경 실패! StageImage={stageImage != null}, Sprite[{stage}]={stageSprites[stage] != null}");
            if (stageImage == null) Debug.LogError("StageImage가 null입니다!");
            if (stageSprites[stage] == null) Debug.LogError($"StageSprites[{stage}]이 null입니다!");
        }
    }
    

    
    /// <summary>
    /// 텍스트 페이드아웃 (90번째부터)
    /// </summary>
    private void UpdateTextFadeOut()
    {
        if (instructionText == null) return;
        
        // 90~100 구간을 0~1로 정규화
        float fadeProgress = (tapProgress - 90f) / 10f;
        float textAlpha = Mathf.Lerp(1f, 0f, fadeProgress);
        
        Color textColor = instructionText.color;
        textColor.a = textAlpha;
        instructionText.color = textColor;
    }
    
    /// <summary>
    /// 미니게임 완료 처리
    /// </summary>
    private void CompleteMinigame()
    {
        isGameCompleted = true;
        isGameActive = false;
        
        // 텍스트 깜빡임 중지
        if (textBlinkCoroutine != null)
        {
            StopCoroutine(textBlinkCoroutine);
        }
        
        // 텍스트 완전히 숨기기 (완료 메시지 없음)
        if (instructionText != null)
        {
            instructionText.gameObject.SetActive(false);
        }
        
        Debug.Log("미니게임 완료!");
        
        // 완료 이벤트 호출
        OnMinigameComplete?.Invoke();
        
        // 마지막 이미지 2초간 보여주고 정리
        StartCoroutine(ShowFinalImageAndCleanup());
    }
    
    /// <summary>
    /// 마지막 이미지 2초간 보여주고 정리
    /// </summary>
    private IEnumerator ShowFinalImageAndCleanup()
    {
        // 2초 대기
        yield return new WaitForSeconds(2f);
        
        // 플레이어 입력 복구
        BlockPlayerInput(false);
        
        // UI 완전히 숨기기
        if (gameCanvas != null)
        {
            gameCanvas.gameObject.SetActive(false);
        }
        
        Debug.Log("미니게임 UI 정리 완료!");
    }
    
    /// <summary>
    /// 텍스트 깜빡임 코루틴 (90번 이전까지만)
    /// </summary>
    private IEnumerator BlinkText()
    {
        if (instructionText == null) yield break;
        
        Color originalColor = instructionText.color;
        
        while (isGameActive && !isGameCompleted && tapProgress < 90f)
        {
            // 어두워지기
            float elapsedTime = 0f;
            while (elapsedTime < blinkSpeed && tapProgress < 90f)
            {
                elapsedTime += Time.deltaTime;
                float alpha = Mathf.Lerp(maxAlpha, minAlpha, elapsedTime / blinkSpeed);
                
                Color newColor = originalColor;
                newColor.a = alpha;
                instructionText.color = newColor;
                
                yield return null;
            }
            
            // 밝아지기
            elapsedTime = 0f;
            while (elapsedTime < blinkSpeed && tapProgress < 90f)
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
    
    /// <summary>
    /// 연타 사운드 재생
    /// </summary>
    private void PlayTapSound()
    {
        if (audioSource != null && tapSound != null)
        {
            audioSource.PlayOneShot(tapSound, 0.3f);
        }
    }
    
    /// <summary>
    /// 단계 변화 사운드 재생
    /// </summary>
    private void PlayStageChangeSound()
    {
        if (audioSource != null && stageChangeSound != null)
        {
            audioSource.PlayOneShot(stageChangeSound, 0.7f);
        }
    }
    
    /// <summary>
    /// 미니게임 강제 정지
    /// </summary>
    public void StopMinigame()
    {
        isGameActive = false;
        
        if (textBlinkCoroutine != null)
        {
            StopCoroutine(textBlinkCoroutine);
        }
        
        // 플레이어 입력 복구
        BlockPlayerInput(false);
    }
    
    /// <summary>
    /// 진행도 리셋
    /// </summary>
    public void ResetProgress()
    {
        tapProgress = 0f;
        currentStage = -1; // -1로 리셋
        isGameCompleted = false;
        
        if (stageImage != null)
        {
            stageImage.sprite = null;
            stageImage.color = Color.black; // 검은색 배경으로 리셋
        }
        
        if (instructionText != null)
        {
            instructionText.gameObject.SetActive(true);
            Color textColor = instructionText.color;
            textColor.a = 1f;
            instructionText.color = textColor;
        }
    }
    
    /// <summary>
    /// 현재 진행도 반환 (0~1)
    /// </summary>
    public float GetProgress()
    {
        return tapProgress / tapGoal;
    }
    
    /// <summary>
    /// 현재 단계 반환
    /// </summary>
    public int GetCurrentStage()
    {
        return currentStage;
    }
    
    /// <summary>
    /// 게임 활성 상태 확인
    /// </summary>
    public bool IsGameActive()
    {
        return isGameActive;
    }
    
    /// <summary>
    /// 게임 완료 상태 확인
    /// </summary>
    public bool IsGameCompleted()
    {
        return isGameCompleted;
    }
}