using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class HoverTextEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private TextMeshProUGUI tmpText;
    private string originalText;

    void Awake()
    {
        tmpText = GetComponent<TextMeshProUGUI>();
        originalText = tmpText.text;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // ♤ ♧ † £ ¢
        tmpText.text = "† " + originalText;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        tmpText.text = originalText;
    }
}
