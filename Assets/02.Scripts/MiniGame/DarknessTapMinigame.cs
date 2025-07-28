using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 어둠을 걷어내는 연타 미니게임 컨트롤러
/// Space를 반복해서 누르면 화면이 점점 밝아짐
/// </summary>
public class DarknessTapMinigame : MonoBehaviour
{
    [Header("게임 설정")]
    [SerializeField] private float tapGoal = 100f;           // 연타 목표치 (100번)
    [SerializeField] private float perTapIncrease = 1f;      // 한 번 누를 때 증가량 (정확히 1씩)
    [SerializeField] private KeyCode tapKey = KeyCode.Space; // 연타 키
    
    [Header("UI 요소")]
    [SerializeField] private Image darknessOverlay;          // 어둠 오버레이
    [SerializeField] private TextMeshProUGUI instructionText; // 안내 텍스트
    [SerializeField] private Canvas gameCanvas;              // 게임 캔버스
    
    [Header("텍스트 애니메이션")]
    [SerializeField] private float blinkSpeed = 1f;         // 깜빡임 속도
    [SerializeField] private float minAlpha = 0.3f;         // 최소 투명도
    [SerializeField] private float maxAlpha = 1f;           // 최대 투명도
    
    [Header("사운드 (선택사항)")]
    [SerializeField] private AudioSource audioSource;       // 오디오 소스
    [SerializeField] private AudioClip tapSound;            // 연타 사운드
    [SerializeField] private AudioClip completeSound;       // 완료 사운드
    
    // 게임 상태
    private float tapProgress = 0f;
    private bool isGameActive = false;
    private bool isGameCompleted = false;
    
    // 텍스트 애니메이션
    private Coroutine textBlinkCoroutine;
    
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
        // 어둠 오버레이 초기화 (완전히 어두운 상태)
        if (darknessOverlay != null)
        {
            Color darkColor = Color.black;
            darkColor.a = 1f;
            darknessOverlay.color = darkColor;
        }
        
        // 안내 텍스트 설정
        if (instructionText != null)
        {
            instructionText.text = "연타 (Space) 하세요!";
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
    }
    
    /// <summary>
    /// 미니게임 시작
    /// </summary>
    public void StartMinigame()
    {
        isGameActive = true;
        isGameCompleted = false;
        tapProgress = 0f;
        
        // 텍스트 깜빡임 시작
        if (textBlinkCoroutine != null)
        {
            StopCoroutine(textBlinkCoroutine);
        }
        textBlinkCoroutine = StartCoroutine(BlinkText());
        
        Debug.Log("어둠 걷어내기 미니게임 시작!");
    }
    
    private void Update()
    {
        if (!isGameActive || isGameCompleted) return;
        
        // Space 키 입력 처리
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
        
        // 어둠 알파값 업데이트
        UpdateDarknessAlpha();
        
        // 목표 달성 확인
        if (tapProgress >= tapGoal)
        {
            CompleteMinigame();
        }
        
        Debug.Log($"연타 진행도: {tapProgress:F1}/{tapGoal}");
    }
    
    /// <summary>
    /// 어둠 오버레이 알파값 업데이트 + 텍스트도 함께 페이드아웃
    /// </summary>
    private void UpdateDarknessAlpha()
    {
        if (darknessOverlay == null) return;
        
        // 진행도에 따라 알파값 계산 (1 -> 0)
        float progress = tapProgress / tapGoal;
        float darknessAlpha = Mathf.Lerp(1f, 0f, progress);
        
        // 어둠 오버레이 알파값 적용
        Color currentColor = darknessOverlay.color;
        currentColor.a = darknessAlpha;
        darknessOverlay.color = currentColor;
        
        // 텍스트도 함께 서서히 사라지게 (진행도 50% 이후부터 사라지기 시작)
        if (instructionText != null && progress > 0.5f)
        {
            float textProgress = (progress - 0.5f) / 0.5f; // 0.5~1을 0~1로 정규화
            float textAlpha = Mathf.Lerp(1f, 0f, textProgress);
            
            Color textColor = instructionText.color;
            textColor.a = textAlpha;
            instructionText.color = textColor;
        }
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
        
        // 완료 사운드 재생
        PlayCompleteSound();
        
        // 텍스트 변경
        if (instructionText != null)
        {
            instructionText.text = "완료!";
            Color textColor = instructionText.color;
            textColor.a = 1f;
            instructionText.color = textColor;
        }
        
        Debug.Log("미니게임 완료!");
        
        // 완료 이벤트 호출
        OnMinigameComplete?.Invoke();
        
        // 잠시 후 UI 정리
        StartCoroutine(CleanupAfterCompletion());
    }
    
    /// <summary>
    /// 완료 후 정리 작업
    /// </summary>
    private IEnumerator CleanupAfterCompletion()
    {
        yield return new WaitForSeconds(2f);
        
        // UI 숨기기 또는 다음 단계로 진행
        if (gameCanvas != null)
        {
            gameCanvas.gameObject.SetActive(false);
        }
        
        // 또는 씬 전환, 다른 게임 로직 등...
    }
    
    /// <summary>
    /// 텍스트 깜빡임 코루틴 (밝아질수록 깜빡임도 서서히 중지)
    /// </summary>
    private IEnumerator BlinkText()
    {
        if (instructionText == null) yield break;
        
        Color originalColor = instructionText.color;
        
        while (isGameActive && !isGameCompleted)
        {
            // 진행도에 따라 깜빡임 강도 조절 (50% 이후부터는 깜빡임 약해짐)
            float progress = tapProgress / tapGoal;
            float blinkIntensity = progress < 0.5f ? 1f : Mathf.Lerp(1f, 0f, (progress - 0.5f) / 0.5f);
            
            float currentMinAlpha = Mathf.Lerp(minAlpha, maxAlpha, 1f - blinkIntensity);
            
            // 어두워지기
            float elapsedTime = 0f;
            while (elapsedTime < blinkSpeed)
            {
                elapsedTime += Time.deltaTime;
                float alpha = Mathf.Lerp(maxAlpha, currentMinAlpha, elapsedTime / blinkSpeed);
                
                Color newColor = originalColor;
                newColor.a = alpha;
                instructionText.color = newColor;
                
                yield return null;
            }
            
            // 밝아지기
            elapsedTime = 0f;
            while (elapsedTime < blinkSpeed)
            {
                elapsedTime += Time.deltaTime;
                float alpha = Mathf.Lerp(currentMinAlpha, maxAlpha, elapsedTime / blinkSpeed);
                
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
            audioSource.PlayOneShot(tapSound, 0.5f);
        }
    }
    
    /// <summary>
    /// 완료 사운드 재생
    /// </summary>
    private void PlayCompleteSound()
    {
        if (audioSource != null && completeSound != null)
        {
            audioSource.PlayOneShot(completeSound, 0.8f);
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
    }
    
    /// <summary>
    /// 진행도 리셋
    /// </summary>
    public void ResetProgress()
    {
        tapProgress = 0f;
        isGameCompleted = false;
        UpdateDarknessAlpha();
    }
    
    /// <summary>
    /// 현재 진행도 반환 (0~1)
    /// </summary>
    public float GetProgress()
    {
        return tapProgress / tapGoal;
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