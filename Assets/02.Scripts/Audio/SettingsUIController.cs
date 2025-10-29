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
    [SerializeField] private Button resetButton;

    [Header("Volume Settings")]
    [Tooltip("기본 BGM 볼륨 (0.0 ~ 1.0)")]
    [Range(0f, 1f)] // 인스펙터에서 슬라이더 범위를 0~1로 제한
    [SerializeField] private float defaultBGMVolume = 0.3f;
    [Tooltip("기본 SFX 볼륨 (0.0 ~ 1.0)")]
    [Range(0f, 1f)] // 인스펙터에서 슬라이더 범위를 0~1로 제한
    [SerializeField] private float defaultSFXVolume = 0.3f;

    // 임시 볼륨 값들 (확인/취소 기능용)
    private float tempBGMVolume;
    private float tempSFXVolume;
    private float originalBGMVolume;
    private float originalSFXVolume;

    // --- OnEnable / OnDisable / HandleMasterVolumeChanged 부분은 그대로 유지 ---
    private void OnEnable()
    {
        AudioEventSystem.OnMasterVolumeChanged += HandleMasterVolumeChanged;

        // 설정 패널이 활성화될 때마다 현재 볼륨을 기준으로
        // original (취소용) 볼륨과 temp (확인용) 볼륨을 *모두* 초기화
        if (SoundPlayer.Instance != null)
        {
            // 1. SoundPlayer에서 현재 볼륨을 가져와 originalBGMVolume에 저장
            originalBGMVolume = SoundPlayer.Instance.GetBGMVolume();
            originalSFXVolume = SoundPlayer.Instance.GetSFXVolume();

            // 2. 슬라이더 값을 SoundPlayer의 현재 값으로 (알림 없이) 업데이트
            bgmVolumeSlider?.SetValueWithoutNotify(originalBGMVolume);
            sfxVolumeSlider?.SetValueWithoutNotify(originalSFXVolume);

            // 3. temp 볼륨을 현재 볼륨 (original)으로 초기화
            tempBGMVolume = originalBGMVolume;
            tempSFXVolume = originalSFXVolume;
        }
        else
        {
            // SoundPlayer가 없는 비상 상황 처리
            originalBGMVolume = bgmVolumeSlider != null ? bgmVolumeSlider.value : defaultBGMVolume;
            originalSFXVolume = sfxVolumeSlider != null ? sfxVolumeSlider.value : defaultSFXVolume;

            // temp 볼륨도 동일하게 초기화
            tempBGMVolume = originalBGMVolume;
            tempSFXVolume = originalSFXVolume;

            Debug.LogWarning("SoundPlayer.Instance not found. Initializing temp/original volumes from slider.", this);
        }
    }

    private void OnDisable()
    {
        AudioEventSystem.OnMasterVolumeChanged -= HandleMasterVolumeChanged;
    }

    private void HandleMasterVolumeChanged(float bgmVol, float sfxVol)
    {
        if (bgmVolumeSlider != null && bgmVol >= 0)
        {
            bgmVolumeSlider.SetValueWithoutNotify(bgmVol);
        }
        if (sfxVolumeSlider != null && sfxVol >= 0)
        {
            sfxVolumeSlider.SetValueWithoutNotify(sfxVol);
        }
    }

    private void Start()
    {
        InitializeSettings();
        SetupUIEvents();
    }

    private void InitializeSettings()
    {
        // PlayerPrefs에서 저장된 볼륨 값 불러오기
        // PlayerPrefs.GetFloat의 기본값으로 0이 아닌 defaultVolume 변수를 직접 사용.
        // 이 defaultVolume 변수가 인스펙터에서 0으로 설정되어 있는지 다시 한번 확인.
        float savedBGMVolume = PlayerPrefs.GetFloat("BGMVolume", defaultBGMVolume);
        float savedSFXVolume = PlayerPrefs.GetFloat("SFXVolume", defaultSFXVolume);

        // 슬라이더 초기값 설정 (null 체크 강화)
        if (bgmVolumeSlider != null)
        {
            bgmVolumeSlider.value = savedBGMVolume;
            // 슬라이더의 Min/Max Value가 0.0 ~ 1.0으로 설정되어 있는지 Unity Inspector에서 다시 확인하세요.
            // 필요하다면 코드에서 강제로 설정:
            // bgmVolumeSlider.minValue = 0f;
            // bgmVolumeSlider.maxValue = 1f;
        }
        else
        {
            Debug.LogWarning("BGM Volume Slider is not assigned in Inspector for SettingsUIController.", this);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.value = savedSFXVolume;
            // sfxVolumeSlider.minValue = 0f;
            // sfxVolumeSlider.maxValue = 1f;
        }
        else
        {
            Debug.LogWarning("SFX Volume Slider is not assigned in Inspector for SettingsUIController.", this);
        }

        // SoundPlayer에 볼륨 적용 (초기 설정 시 한 번만)
        if (SoundPlayer.Instance != null)
        {
            SoundPlayer.Instance.ChangeVolume(savedBGMVolume, savedSFXVolume);
        }
        else
        {
            Debug.LogWarning("SoundPlayer.Instance not found. Volume settings might not be applied correctly on start.", this);
        }

        // 설정 패널 비활성화 (null 체크 강화)
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
        else
        {
            Debug.LogWarning("Settings Panel is not assigned in Inspector for SettingsUIController.", this);
        }
    }

    private void SetupUIEvents()
    {
        // 슬라이더 이벤트 설정 (null 체크 강화)
        if (bgmVolumeSlider != null)
        {
            bgmVolumeSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
        }
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }

        // 버튼 이벤트 설정 (null 체크 강화)
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirmButtonClicked);
        }
        if (resetButton != null)
        {
            resetButton.onClick.AddListener(OnResetButtonClicked);
        }
    }

    /// <summary>
    /// 설정 창을 열고 현재 볼륨 값을 임시 저장합니다.
    /// </summary>
    /// <summary>
    /// 설정 창을 엽니다. (주로 PauseManager에서 SetActive(true)로 호출됨)
    /// </summary>
    public void OpenSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Settings Panel is not assigned in Inspector. Cannot open settings.", this);
        }
    }

    /// <summary>
    /// 설정 창을 닫습니다.
    /// </summary>
    public void CloseSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    /// <summary>
    /// BGM 볼륨 슬라이더 변경 이벤트 핸들러.
    /// </summary>
    private void OnBGMVolumeChanged(float value)
    {
        // Ensure value is clamped within 0 to 1, though slider should handle this.
        tempBGMVolume = Mathf.Clamp01(value);
        if (SoundPlayer.Instance != null)
        {
            SoundPlayer.Instance.ChangeVolume(tempBGMVolume, -1f);
        }
    }

    /// <summary>
    /// 효과음 볼륨 슬라이더 변경 이벤트 핸들러.
    /// </summary>
    private void OnSFXVolumeChanged(float value)
    {
        // Ensure value is clamped within 0 to 1.
        tempSFXVolume = Mathf.Clamp01(value);
        if (SoundPlayer.Instance != null)
        {
            SoundPlayer.Instance.ChangeVolume(-1f, tempSFXVolume);
        }
    }

    /// <summary>
    /// 확인 버튼 클릭 시 호출됩니다. 변경된 볼륨을 저장하고 적용합니다.
    /// </summary>
    private void OnConfirmButtonClicked()
    {
        // PlayerPrefs에 저장하기 전에 Mathf.Clamp01로 한 번 더 범위 제한
        PlayerPrefs.SetFloat("BGMVolume", Mathf.Clamp01(tempBGMVolume));
        PlayerPrefs.SetFloat("SFXVolume", Mathf.Clamp01(tempSFXVolume));
        PlayerPrefs.Save();

        // SoundPlayer에 최종 볼륨 적용 (null 체크)
        if (SoundPlayer.Instance != null)
        {
            SoundPlayer.Instance.ChangeVolume(tempBGMVolume, tempSFXVolume);
        }
        else
        {
            Debug.LogWarning("SoundPlayer.Instance not found. Cannot apply confirmed volume settings.", this);
        }

        CloseSettings();
    }

    /// <summary>
    /// 취소 버튼 클릭 시 호출됩니다. 원래 볼륨 값으로 되돌리고 설정 창을 닫습니다.
    /// </summary>
    private void OnCancelButtonClicked()
    {
        // 원래 볼륨 값으로 슬라이더 되돌리기 (null 체크 및 SetValueWithoutNotify 사용)
        if (bgmVolumeSlider != null)
        {
            bgmVolumeSlider.SetValueWithoutNotify(originalBGMVolume);
        }
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.SetValueWithoutNotify(originalSFXVolume);
        }

        // SoundPlayer에 원래 볼륨 적용 (null 체크)
        if (SoundPlayer.Instance != null)
        {
            SoundPlayer.Instance.ChangeVolume(originalBGMVolume, originalSFXVolume);
        }
        else
        {
            Debug.LogWarning("SoundPlayer.Instance not found. Cannot revert volume settings on cancel.", this);
        }

        CloseSettings();
    }

    /// <summary>
    /// 리셋 버튼 클릭 시 호출됩니다. 볼륨을 기본값으로 되돌리고 적용합니다.
    /// </summary>
    private void OnResetButtonClicked()
    {
        // 슬라이더를 기본값으로 설정 (null 체크 및 SetValueWithoutNotify 사용)
        if (bgmVolumeSlider != null)
        {
            bgmVolumeSlider.SetValueWithoutNotify(defaultBGMVolume);
        }
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.SetValueWithoutNotify(defaultSFXVolume);
        }

        // PlayerPrefs도 기본값으로 저장 (Mathf.Clamp01로 한 번 더 범위 제한)
        PlayerPrefs.SetFloat("BGMVolume", Mathf.Clamp01(defaultBGMVolume));
        PlayerPrefs.SetFloat("SFXVolume", Mathf.Clamp01(defaultSFXVolume));
        PlayerPrefs.Save();

        // SoundPlayer에 기본 볼륨 적용 (null 체크)
        if (SoundPlayer.Instance != null)
        {
            SoundPlayer.Instance.ChangeVolume(defaultBGMVolume, defaultSFXVolume);
        }
        else
        {
            Debug.LogWarning("SoundPlayer.Instance not found. Cannot apply default volume settings.", this);
        }
        // 리셋 후에도 temp 볼륨 값은 슬라이더 값과 동기화되어 있어야 함
        tempBGMVolume = defaultBGMVolume;
        tempSFXVolume = defaultSFXVolume;
    }

    // ResetVolume()은 OnResetButtonClicked()를 호출하므로 그대로 유지
    public void ResetVolume()
    {
        OnResetButtonClicked();
    }

    // --- 외부에서 호출할 수 있는 메서드들 (UI 슬라이더 값 설정용) ---
    public void SetBGMVolume(float volume)
    {
        if (bgmVolumeSlider != null)
        {
            bgmVolumeSlider.value = Mathf.Clamp01(volume); // 값 범위 제한
        }
    }

    public void SetSFXVolume(float volume)
    {
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.value = Mathf.Clamp01(volume); // 값 범위 제한
        }
    }

    public float GetBGMVolume()
    {
        // SoundPlayer에서 직접 현재 BGM 볼륨을 가져오는 것이 더 정확할 수 있습니다.
        // 슬라이더 값이 변경 중일 수 있기 때문입니다.
        if (SoundPlayer.Instance != null)
        {
            return SoundPlayer.Instance.GetBGMVolume();
        }
        return bgmVolumeSlider != null ? bgmVolumeSlider.value : defaultBGMVolume;
    }

    public float GetSFXVolume()
    {
        // SoundPlayer에서 직접 현재 SFX 볼륨을 가져오는 것이 더 정확할 수 있습니다.
        if (SoundPlayer.Instance != null)
        {
            return SoundPlayer.Instance.GetSFXVolume();
        }
        return sfxVolumeSlider != null ? sfxVolumeSlider.value : defaultSFXVolume;
    }
    // --- End of public getters/setters ---

    private void OnDestroy()
    {
        // 이벤트 리스너 제거는 그대로 유지 (null 체크 강화)
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

        if (resetButton != null)
        {
            resetButton.onClick.RemoveListener(OnResetButtonClicked);
        }
    }
}