using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// showcase 타이틀화면에서 엔딩 버튼 숨기기 용도
[RequireComponent(typeof(RectTransform))]
public class Showcase_ButtonAnimator : MonoBehaviour
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
    private RectTransform rectTransform;

    [SerializeField] private float fadeDuration = 0.2f;

    private CanvasGroup canvasGroup;
    private Coroutine fadeCoroutine;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        // CanvasGroup 확보 (없으면 자동 추가)
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // 초기 alpha는 0으로 (필요에 따라 1로 변경 가능)
        canvasGroup.alpha = 0f;

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
            trigger.triggers.Clear();
        }
#endif
    }

#if UNITY_EDITOR || UNITY_STANDALONE
    private void OnPointerEnter(BaseEventData eventData)
    {
        StartFade(1f); // alpha 0 → 1
    }

    private void OnPointerExit(BaseEventData eventData)
    {
        StartFade(0f); // alpha 1 → 0
    }

    private void StartFade(float targetAlpha)
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeCanvasGroup(targetAlpha));
    }

    private IEnumerator FadeCanvasGroup(float targetAlpha)
    {
        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            float t = elapsed / fadeDuration;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
    }
#endif

    private void AddEventTriggerListener(EventTrigger trigger,
        EventTriggerType eventType,
        System.Action<BaseEventData> callback)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = eventType;
        entry.callback.AddListener((data) => callback(data));
        trigger.triggers.Add(entry);
    }
}