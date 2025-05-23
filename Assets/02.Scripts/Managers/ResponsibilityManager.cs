using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

// 책임감 (지수) 매니저
public class ResponsibilityManager : MonoBehaviour
{
    public static ResponsibilityManager Instance { get; private set; }

    // 책임감 게이지 
    [SerializeField] private GameObject responsibilityGaugeGroup;
    [SerializeField] private Slider responsibilitySlider;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
            Destroy(gameObject);
    }


    void Start()
    {
        responsibilityGaugeGroup = UIManager.Instance.GetUI(eUIGameObjectName.ResponsibilityGroup);
        responsibilitySlider = UIManager.Instance.responsibilitySlider;
    }

    // ResultManager에서 Result_IncrementResponsibilityScore이면
    // case문에서 ResponsibilityScore 증가시키고 아래의 메소드를 호출함.
    public void ChangeResponsibilityGauge()
    {
        if (responsibilityGaugeGroup == null)
            return;

        int maxResponsibilityScore = (int)GameManager.Instance.GetVariable("MaxResponsibilityScore");
        int currentResponsibilityScore = (int)GameManager.Instance.GetVariable("ResponsibilityScore");

        //responsibilitySlider.value = Mathf.Clamp((float)currentResponsibilityScore / maxResponsibilityScore, 0, maxResponsibilityScore);
        float targetValue = Mathf.Clamp01((float)currentResponsibilityScore / maxResponsibilityScore);

        // 게이지 증가시키는 코루틴 실행
        StartCoroutine(AnimateGaugeChanging(targetValue));
    }

    // 게이지 증가 애니메이션 코루틴
    private IEnumerator AnimateGaugeChanging(float targetValue)
    {
        float duration = 0.5f; // 애니메이션 지속 시간 (초)
        float elapsed = 0f;
        float startValue = responsibilitySlider.value;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            responsibilitySlider.value = Mathf.Lerp(startValue, targetValue, elapsed / duration);
            yield return null;
        }

        responsibilitySlider.value = targetValue; // 마지막에 정확히 맞춰줌
    }

}
