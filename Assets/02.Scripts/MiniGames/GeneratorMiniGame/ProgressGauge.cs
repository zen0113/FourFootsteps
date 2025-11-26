using UnityEngine;
using UnityEngine.UI;

public class ProgressGauge : MonoBehaviour
{
    [Header("UI 컴포넌트")]
    public Slider progressSlider;
    public Image fillImage;
    public Text progressText;
    
    [Header("색상 설정")]
    public Gradient progressColors;
    
    [Header("텍스트 색상 변경 설정")]
    [Tooltip("텍스트 색상이 변경될 진행도 (%)")]
    public float textColorChangeThreshold = 48f;
    [Tooltip("임계값 이전 텍스트 색상")]
    public Color textColorBefore = Color.white;
    [Tooltip("임계값 이후 텍스트 색상")]
    public Color textColorAfter = Color.black;
    
    void Start()
    {
        if (progressSlider == null)
            progressSlider = GetComponent<Slider>();
            
        if (fillImage == null && progressSlider != null)
            fillImage = progressSlider.fillRect.GetComponent<Image>();
            
        SetProgress(0f);
    }
    
    public void SetProgress(float progress)
    {
        float normalizedProgress = Mathf.Clamp01(progress / 100f);
        
        if (progressSlider != null)
            progressSlider.value = normalizedProgress;
            
        if (fillImage != null)
            fillImage.color = progressColors.Evaluate(normalizedProgress);
            
        if (progressText != null)
        {
            progressText.text = $"{Mathf.RoundToInt(progress)}%";
            
            // 진행도에 따른 텍스트 색상 변경
            if (progress >= textColorChangeThreshold)
            {
                progressText.color = textColorAfter;
            }
            else
            {
                progressText.color = textColorBefore;
            }
        }
    }
}