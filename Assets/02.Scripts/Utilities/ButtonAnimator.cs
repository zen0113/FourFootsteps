using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class ButtonAnimator : MonoBehaviour
{
    [Header("Hover Effect")]
    [SerializeField] private bool enableHoverEffect = true;
    public bool EnableHoverEffect
    {
        get => enableHoverEffect;
        set
        {
            if (enableHoverEffect == value) return;
            enableHoverEffect = value;

            if (enableHoverEffect)
                AddHoverEvents();
            else
                RemoveHoverEvents();
        }
    }

    private EventTrigger trigger;

    [SerializeField] private float hoverScaleAmount = 1.05f;

    private RectTransform rectTransform;
    private Vector3 originalScale;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Coroutine animationCoroutine;
    private bool isHovering = false;

    [SerializeField] private bool alphaChangeEffect = false;
    [SerializeField] private Image image;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalScale = rectTransform.localScale;
        originalPosition = rectTransform.localPosition;
        originalRotation = rectTransform.localRotation;
        image = GetComponent<Image>();

        // Add hover detection if needed
        if (enableHoverEffect)
            AddHoverEvents();
    }

    private void AddHoverEvents()
    {
        Button button = GetComponent<Button>();
        if (button == null) return;

#if UNITY_EDITOR || UNITY_STANDALONE
        if (trigger == null)
            trigger = gameObject.AddComponent<EventTrigger>();

        AddEventTriggerListener(trigger, EventTriggerType.PointerEnter, OnPointerEnter);
        AddEventTriggerListener(trigger, EventTriggerType.PointerExit, OnPointerExit);
#endif
    }

    private void RemoveHoverEvents()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        if (trigger != null)
        {
            trigger.triggers.Clear(); // 이벤트 전부 제거
        }
#endif
    }

    private void OnDisable()
    {
        // Reset to original state
        rectTransform.localScale = originalScale;
        rectTransform.localPosition = originalPosition;
        rectTransform.localRotation = originalRotation;
    }

#if UNITY_EDITOR || UNITY_STANDALONE
    private void OnPointerEnter(BaseEventData eventData)
    {
        isHovering = true;
        StopAllCoroutines();
        if(!alphaChangeEffect)
            StartCoroutine(HoverAnimation(true));
        else
            StartCoroutine(AlphaAnimation(true));
    }

    private void OnPointerExit(BaseEventData eventData)
    {
        isHovering = false;
        StopAllCoroutines();
        if (!alphaChangeEffect)
            StartCoroutine(HoverAnimation(false));
        else
            StartCoroutine(AlphaAnimation(false));
    }

    private IEnumerator HoverAnimation(bool hovering)
    {
        Vector3 targetScale = hovering ? originalScale * hoverScaleAmount : originalScale;
        Vector3 currentScale = rectTransform.localScale;

        float elapsed = 0f;
        float hoverDuration = 0.2f;

        while (elapsed < hoverDuration)
        {
            float t = elapsed / hoverDuration;
            rectTransform.localScale = Vector3.Lerp(currentScale, targetScale, t);

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        rectTransform.localScale = targetScale;
    }

    private IEnumerator AlphaAnimation(bool hovering)
    {

        Color color = image.color;
        float targetAlpha = hovering ? 0f : 1f;
        float currentAlpha = color.a;

        float elapsed = 0f;
        float hoverDuration = 0.2f;

        while (elapsed < hoverDuration)
        {
            float t = elapsed / hoverDuration;

            color.a = Mathf.Lerp(currentAlpha, targetAlpha, t);
            image.color = color;

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        color.a = targetAlpha;
        image.color = color;
    }

    private void AddEventTriggerListener(EventTrigger trigger,
        EventTriggerType eventType,
        System.Action<BaseEventData> callback)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = eventType;
        entry.callback.AddListener((data) => callback(data));
        trigger.triggers.Add(entry);
    }
#endif
}
