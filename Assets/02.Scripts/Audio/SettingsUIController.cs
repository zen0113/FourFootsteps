using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingsUIController : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Slider bgmVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    [Header("Volume Settings")]
    [SerializeField] private float defaultBGMVolume = 0.5f;
    [SerializeField] private float defaultSFXVolume = 0.5f;

    [Header("외부 AudioSource 연동")]
    [SerializeField] private AudioSource[] additionalSFXSources;

    // 임시 볼륨 값들 (확인/취소 기능용)
    private float tempBGMVolume;
    private float tempSFXVolume;
    private float originalBGMVolume;
    private float originalSFXVolume;

    private void Start()
    {
        InitializeSettings();
        SetupUIEvents();
    }

    private void InitializeSettings()
    {
        // PlayerPrefs에서 저장된 볼륨 값 불러오기
        float savedBGMVolume = PlayerPrefs.GetFloat("BGMVolume", defaultBGMVolume);
        float savedSFXVolume = PlayerPrefs.GetFloat("SFXVolume", defaultSFXVolume);

        // 슬라이더 초기값 설정
        if (bgmVolumeSlider != null)
        {
            bgmVolumeSlider.value = savedBGMVolume;
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.value = savedSFXVolume;
        }

        // SoundPlayer에 볼륨 적용
        if (SoundPlayer.Instance != null)
        {
            SoundPlayer.Instance.ChangeVolume(savedBGMVolume, savedSFXVolume);
        }

        // 설정 패널 비활성화
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    private void SetupUIEvents()
    {
        // 슬라이더 이벤트 설정
        if (bgmVolumeSlider != null)
        {
            bgmVolumeSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }

        // 버튼 이벤트 설정
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirmButtonClicked);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(OnCancelButtonClicked);
        }
    }

    // 설정 창 열기
    public void OpenSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);

            // 현재 볼륨 값들을 임시 변수에 저장
            originalBGMVolume = bgmVolumeSlider.value;
            originalSFXVolume = sfxVolumeSlider.value;
            tempBGMVolume = originalBGMVolume;
            tempSFXVolume = originalSFXVolume;
        }
    }

    // 설정 창 닫기
    public void CloseSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    // BGM 볼륨 슬라이더 변경 이벤트
    private void OnBGMVolumeChanged(float value)
    {
        tempBGMVolume = value;

        // 실시간으로 볼륨 변경 적용 (미리보기)
        if (SoundPlayer.Instance != null)
        {
            SoundPlayer.Instance.ChangeVolume(tempBGMVolume, -1);
        }
    }

    // 효과음 볼륨 슬라이더 변경 이벤트
    private void OnSFXVolumeChanged(float value)
    {
        tempSFXVolume = value;

        // 실시간으로 볼륨 변경 적용 (미리보기)
        if (SoundPlayer.Instance != null)
        {
            SoundPlayer.Instance.ChangeVolume(-1, tempSFXVolume);
        }

        // 외부 오디오 소스 볼륨도 동기화
        foreach (var source in additionalSFXSources)
        {
            if (source != null)
                source.volume = tempSFXVolume;
        }
    }

    // 확인 버튼 클릭 이벤트
    private void OnConfirmButtonClicked()
    {
        PlayerPrefs.SetFloat("BGMVolume", tempBGMVolume);
        PlayerPrefs.SetFloat("SFXVolume", tempSFXVolume);
        PlayerPrefs.Save();

        if (SoundPlayer.Instance != null)
        {
            SoundPlayer.Instance.ChangeVolume(tempBGMVolume, tempSFXVolume);
        }

        // 외부 AudioSource 볼륨 적용
        foreach (var source in additionalSFXSources)
        {
            if (source != null)
                source.volume = tempSFXVolume;
        }

        CloseSettings();
    }

    // 취소 버튼 클릭 이벤트
    private void OnCancelButtonClicked()
    {
        // 원래 볼륨 값으로 되돌리기
        if (bgmVolumeSlider != null)
        {
            bgmVolumeSlider.value = defaultBGMVolume;
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.value = defaultSFXVolume;
        }

        // SoundPlayer에 원래 볼륨 적용
        if (SoundPlayer.Instance != null)
        {
            SoundPlayer.Instance.ChangeVolume(defaultBGMVolume, defaultSFXVolume);
        }
    }


    // 외부에서 호출할 수 있는 메서드들
    public void SetBGMVolume(float volume)
    {
        if (bgmVolumeSlider != null)
        {
            bgmVolumeSlider.value = volume;
        }
    }

    public void SetSFXVolume(float volume)
    {
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.value = volume;
        }
    }

    public float GetBGMVolume()
    {
        return bgmVolumeSlider != null ? bgmVolumeSlider.value : defaultBGMVolume;
    }

    public float GetSFXVolume()
    {
        return sfxVolumeSlider != null ? sfxVolumeSlider.value : defaultSFXVolume;
    }

    // 설정 리셋
    public void ResetToDefault()
    {
        if (bgmVolumeSlider != null)
        {
            bgmVolumeSlider.value = defaultBGMVolume;
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.value = defaultSFXVolume;
        }
    }

    private void OnDestroy()
    {
        // 이벤트 리스너 제거
        if (bgmVolumeSlider != null)
        {
            bgmVolumeSlider.onValueChanged.RemoveListener(OnBGMVolumeChanged);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.RemoveListener(OnSFXVolumeChanged);
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(OnConfirmButtonClicked);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveListener(OnCancelButtonClicked);
        }

    }
}