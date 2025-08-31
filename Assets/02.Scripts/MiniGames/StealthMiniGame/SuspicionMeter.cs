using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SuspicionMeter : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private StealthSettingsSO settings;

    [Header("UI")]
    public GameObject suspicionGaugeGroup;
    public Image suspicionGauge;
    public TextMeshProUGUI suspicionText;

    public event Action OnReachedMax;

    private void Awake()
    {
        if (!settings)
        {
            Debug.LogWarning("[CatStealthController] StealthSettings가 지정되지 않았습니다. 기본값으로 동작합니다.");
            settings = ScriptableObject.CreateInstance<StealthSettingsSO>();
        }
        OnReachedMax += CaughtByKid;
    }

    void Start() => UpdateUI();

    public void ApplyDelta(float delta)
    {
        float prev = settings.gaugeCurrent;
        settings.gaugeCurrent = Mathf.Clamp(settings.gaugeCurrent + delta, 0f, settings.gaugeMax);
        if (suspicionGauge) suspicionGauge.fillAmount = settings.gaugeCurrent / Mathf.Max(0.0001f, settings.gaugeMax);

        if (suspicionGauge.fillAmount >= 0.5f)
        {
            suspicionText.text = "!";
        }else
            suspicionText.text = "?";

        if (prev < settings.gaugeMax && Mathf.Approximately(settings.gaugeCurrent, settings.gaugeMax))
            OnReachedMax?.Invoke();
    }

    public void ResetMeter(float value = 0f)
    {
        settings.gaugeCurrent = Mathf.Clamp(value, 0f, settings.gaugeMax);
        UpdateUI();
    }

    void UpdateUI()
    {
        if (suspicionGauge) suspicionGauge.fillAmount = settings.gaugeCurrent / Mathf.Max(0.0001f, settings.gaugeMax);
    }

    private void CaughtByKid()
    {
        suspicionGaugeGroup.SetActive(false);
    }
}
