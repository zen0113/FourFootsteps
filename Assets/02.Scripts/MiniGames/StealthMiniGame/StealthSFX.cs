using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class StealthSFX : MonoBehaviour
{
    public static StealthSFX Instance { get; private set; }

    [Header("Refs")]
    [SerializeField] private StealthSettingsSO settings;
    [SerializeField] private Camera mainCamera;

    [Header("UI")]
    public TextMeshProUGUI guideText;
    public float textPromptBlinkSpeed = 4f; // 텍스트 프롬프트 깜빡임 속도

    [Header("Audio")]
    AudioSource audioSource;
    [SerializeField] private AudioClip heartBeatSound; // 심장 소리

    [SerializeField] private bool isShowedGuide = false;
    private bool isActive = false;
    private CatStealthController sc;
    private Coroutine sfxCo, camCo, uiCo; // 각 연출 코루틴 핸들

    private void Awake()
    {
        if (Instance == null) Instance = this;

        mainCamera = Camera.main;
        sc = FindObjectOfType<CatStealthController>();
        guideText.gameObject.SetActive(false);

        sc.OnEnterArea += HandleEnterArea;
        sc.OnExitArea += HandleExitArea;
        sc.OnHideStart += HandleHideStart;
        sc.OnHideEnd += HandleHideEnd;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        if (!settings)
        {
            Debug.LogWarning("[CatStealthController] StealthSettings가 지정되지 않았습니다. 기본값으로 동작합니다.");
            settings = ScriptableObject.CreateInstance<StealthSettingsSO>();
        }
    }

    public void DisconnectEvent()
    {
        sc.OnEnterArea -= HandleEnterArea;
        sc.OnExitArea -= HandleExitArea;
        sc.OnHideStart -= HandleHideStart;
        sc.OnHideEnd -= HandleHideEnd;
    }

    void HandleEnterArea(HideObject ho)
    {
        // 맨 처음에만 화면 중앙에 "E 키로 숨기" 토스트 띄우기(텍스트 깜빡깜빡)
        if (!isShowedGuide)
        {
            isActive = true;
            guideText?.gameObject.SetActive(true);
            guideText.text = "E 키로 숨기";
            StartCoroutine(BlinkTextPrompt());
        }
    }

    void HandleExitArea(HideObject ho)
    {
        if (!isShowedGuide)
        {
            isActive = false;
            guideText?.gameObject.SetActive(false);
        }
    }

    void HandleHideStart(HideObject ho)
    {
        if (!isShowedGuide)
        {
            guideText.text = "E 키로 나오기";
        }
        // 카메라 플레이어에게 확대 줌 인 및 화면이 비네팅으로 살짝 어두워짐
        // 심장 박동소리 효과음 무한 루프 및 주변 환경음 및 다른 효과음 소리 점점 줄어듬

        // 사운드 페이드 (기존 코루틴 정리 후 시작)
        if (sfxCo != null) StopCoroutine(sfxCo);
        sfxCo = StartCoroutine(FadeSFXCoroutine(0f, 0.8f, 1f, 0f));

        // 카메라 줌 (기존 코루틴 정리 후 시작)
        if (camCo != null) StopCoroutine(camCo);
        camCo = StartCoroutine(ChangeCameraSize(settings.enterHideSize));

        // UI 연출 (하나의 통로로 관리)
        if (uiCo != null) StopCoroutine(uiCo);
        uiCo = StartCoroutine(UIManager.Instance.HidingCoroutine(true)); // UIManager가 코루틴 내부에서 깜빡임/비네팅 등 처리
        UIManager.Instance.SetBlinkHidingCoroutine(true);
    }

    void HandleHideEnd(HideObject ho)
    {
        if (!isShowedGuide)
        {
            isActive = false;
            guideText.gameObject.SetActive(false);
            isShowedGuide = true;
        }
        // 카메라가 플레이어에게서 확대 줌 아웃 및 화면이 원래대로 밝아짐
        // 심장 박동소리 줄어들고 주변 환경음 및 다른 효과음 소리 점점 커짐

        // 사운드 페이드 (기존 코루틴 정리 후 시작)
        if (sfxCo != null) StopCoroutine(sfxCo);
        sfxCo = StartCoroutine(FadeSFXCoroutine(0.8f, 0f, 1f, 0f));

        // 카메라 줌 (기존 코루틴 정리 후 시작)
        if (camCo != null) StopCoroutine(camCo);
        camCo = StartCoroutine(ChangeCameraSize(settings.exitHideSize));

        if (uiCo != null) StopCoroutine(uiCo);
        UIManager.Instance.SetBlinkHidingCoroutine(false);
        uiCo = StartCoroutine(UIManager.Instance.HidingCoroutine(false));
    }
    
    public void CaughtByKidFX()
    {
        StartCoroutine(FadeSFXCoroutine(0.8f, 0f, 1f, 0f));
        //StartCoroutine(ChangeCameraSize(settings.exitHideSize));
        UIManager.Instance.SetBlinkHidingCoroutine(false);
        StartCoroutine(UIManager.Instance.HidingCoroutine(false));
    }


    // 가이드 텍스트 깜빡이는 효과 코루틴
    IEnumerator BlinkTextPrompt()
    {
        while (isActive)
        {
            // 깜빡이는 효과
            float alpha = Mathf.PingPong(Time.time * textPromptBlinkSpeed, 1f); // 알파값 깜빡임 효과
            Color color = guideText.color;
            color.a = 0.5f + (alpha * 0.5f); // 0.5 ~ 1.0 사이로 알파값 조정
            guideText.color = color;

            yield return null;
        }

        // 종료 시 원래 색상으로 복원
        Color finalColor = guideText.color;
        finalColor.a = 1f;
        guideText.color = finalColor;
    }

    // 숨거나 나왔을 경우 카메라 줌인/아웃 변경 효과 코루틴
    IEnumerator ChangeCameraSize(float finalValue)
    {
        float elapsedTime = 0f;
        float duration = settings.sizeDuration;
        float startValue = mainCamera.orthographicSize;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            mainCamera.orthographicSize = Mathf.Lerp(startValue, finalValue, (elapsedTime / duration));
            mainCamera.GetComponent<FollowCamera>().UpdateCameraHalfSize();
            yield return null;
        }

        mainCamera.orthographicSize = finalValue;
        mainCamera.GetComponent<FollowCamera>().UpdateCameraHalfSize();
    }

    // 심장 효과음 페이드 재생 코루틴
    private IEnumerator FadeSFXCoroutine(float startVolume,float targetVolume, float duration, float delay)
    {
        if (delay > 0f)
        {
            float d = 0f;
            while (d < delay)
            {
                d += Time.deltaTime;
                yield return null; // 도중에 StopCoroutine 되면 자연 종료
            }
        }

        // 전용 루프 설정
        if (audioSource.clip != heartBeatSound)
            audioSource.clip = heartBeatSound;
        audioSource.loop = true;

        // "현재값 → 목표값"으로 보간 (연속 토글에도 이어짐)
        float initial = audioSource.volume;   // 현재 볼륨에서 시작
        float timeElapsed = 0f;

        // 타겟 볼륨이 0보다 크면 재생 보장
        if (targetVolume > 0f && !audioSource.isPlaying)
            audioSource.Play();

        duration = Mathf.Max(0.0001f, duration);
        while (timeElapsed < duration)
        {
            timeElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(timeElapsed / duration);
            // SmoothStep로 부드럽게
            audioSource.volume = Mathf.Lerp(initial, targetVolume, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        audioSource.volume = targetVolume; // 최종 보정

        // 완전 무음이면 정지 (다음에 다시 Play로 시작)
        if (Mathf.Approximately(targetVolume, 0f))
            audioSource.Stop();
    }

    public void PlayEnterSFX(float camSize, float targetVolume = 0.8f, float fadeDur = 1f, float delay = 0f)
    {
        if (sfxCo != null) StopCoroutine(sfxCo);
        sfxCo = StartCoroutine(FadeSFXCoroutine(0f, targetVolume, fadeDur, delay));

        if (camCo != null) StopCoroutine(camCo);
        camCo = StartCoroutine(ChangeCameraSize(camSize));

        if (uiCo != null) StopCoroutine(uiCo);
        if (UIManager.Instance != null)
        {
            uiCo = StartCoroutine(UIManager.Instance.HidingCoroutine(true));
            UIManager.Instance.SetBlinkHidingCoroutine(true);
        }
    }

    public void StopEnterSFX()
    {
        if (sfxCo != null) StopCoroutine(sfxCo);
        sfxCo = StartCoroutine(FadeSFXCoroutine(0.8f, 0f, 1f, 0f));

        if (uiCo != null) StopCoroutine(uiCo);
        UIManager.Instance.SetBlinkHidingCoroutine(false);
        uiCo = StartCoroutine(UIManager.Instance.HidingCoroutine(false));
    }
}
