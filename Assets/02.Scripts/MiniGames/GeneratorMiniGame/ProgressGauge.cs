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
        progress = Mathf.Clamp01(progress / 100f);
        
        if (progressSlider != null)
            progressSlider.value = progress;
            
        if (fillImage != null)
            fillImage.color = progressColors.Evaluate(progress);
            
        if (progressText != null)
            progressText.text = $"{Mathf.RoundToInt(progress * 100)}%";
    }
}