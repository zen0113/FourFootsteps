using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class HoverTextEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // ♤ ♧ †
    [SerializeField] private string emoticon = "♤";
    [SerializeField] private Color hoverColor = Color.black; // 마우스 올렸을 때 색상
    private TextMeshProUGUI tmpText;
    private string originalText;
    private Color originalColor;

    void Awake()
    {
        tmpText = GetComponent<TextMeshProUGUI>();
        originalText = tmpText.text;
        originalColor = tmpText.color;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        tmpText.text = $"{emoticon} {originalText}";
        tmpText.color = hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        tmpText.text = originalText;
        tmpText.color = originalColor;
    }
}
