using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;       // Image
using TMPro;               // TextMeshProUGUI

public class ToastTextSpawner : MonoBehaviour
{
    public static ToastTextSpawner Instance { get; private set; }

    public enum ToastMood { Positive, Negative }

    [Header("Prefab & Parent")]
    [SerializeField] private GameObject toastPrefab;
    [Tooltip("PlayerUI 캔버스의 ToastLayer 할당")]
    [SerializeField] private RectTransform toastParent;

    [Header("Common Anim")]
    [SerializeField] private float duration = 3f;
    [SerializeField] private Vector2 jitterX = new Vector2(-12f, 12f);

    [Header("Style: Positive (+1)")]
    [SerializeField] private Color positiveTextColor = Color.black;
    [SerializeField] private Color positiveBgColor = new Color(1f, 1f, 1f, 0.85f);
    [SerializeField] private Vector2 riseOffsetUp = new Vector2(0f, 60f);
    [SerializeField]
    private AnimationCurve alphaUp = new AnimationCurve(
        new Keyframe(0.00f, 0.0f, 0f, 8f),
        new Keyframe(0.12f, 1.0f, 0f, 0f),
        new Keyframe(0.80f, 1.0f, 0f, 0f),
        new Keyframe(1.00f, 0.0f, -8f, 0f)
    );
    [SerializeField]
    private AnimationCurve scaleUp = new AnimationCurve(
        new Keyframe(0.00f, 0.85f),
        new Keyframe(0.15f, 1.10f),
        new Keyframe(1.00f, 1.00f)
    );

    [Header("Style: Negative (+0)")]
    [SerializeField] private Color negativeTextColor = Color.red;
    [SerializeField] private Color negativeBgColor = new Color(1f, 1f, 1f, 0.85f);
    [SerializeField] private Vector2 riseOffsetDown = new Vector2(0f, -60f);
    [SerializeField]
    private AnimationCurve alphaDown = new AnimationCurve(
        new Keyframe(0.00f, 0.0f, 0f, 9f),
        new Keyframe(0.10f, 1.0f, 0f, 0f),
        new Keyframe(0.70f, 1.0f, 0f, 0f),
        new Keyframe(0.95f, 0.0f, -9f, 0f)
    );
    [SerializeField]
    private AnimationCurve scaleDown = new AnimationCurve(
        new Keyframe(0.00f, 1.05f),
        new Keyframe(0.20f, 0.95f),
        new Keyframe(1.00f, 0.95f)
    );

    [Header("Layer Auto Toggle")]
    [SerializeField] private bool autoToggleLayer = true;     // 계속 켜둘거면 false
    [SerializeField] private float layerOffDelay = 0.25f;

    [Header("Pooling")]
    [SerializeField] private int initialPool = 2;

    // ---------- Internal ----------
    private readonly Queue<ToastItem> pool = new Queue<ToastItem>();
    private int liveToasts = 0;
    private Coroutine layerOffCo;

    // 프리팹에서 찾아올 구성 요소 묶음
    private class ToastItem
    {
        public GameObject go;
        public RectTransform rt;
        public CanvasGroup cg;              // 전체 알파/페이드용
        public TextMeshProUGUI tmp;         // 텍스트
        public Image bg;                    // 배경(옵션)
    }

    public static (string msg, ToastMood mood) GetToastForDelta(int delta)
    {
        if (delta >= 1) return ($"책임지수 +{delta}", ToastMood.Positive);
        else return ("책임지수 상승 실패", ToastMood.Negative);
    }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (toastParent == null)
            toastParent = GetComponentInParent<Canvas>()?.transform as RectTransform;

        for (int i = 0; i < Mathf.Max(1, initialPool); i++)
            pool.Enqueue(CreateInstance());

        // 항상 켜둘 거면 아래는 건드릴 필요 X
        if (autoToggleLayer) toastParent.gameObject.SetActive(false);
    }

    // ----------------- Pooling -----------------
    private ToastItem CreateInstance()
    {
        var go = Instantiate(toastPrefab, toastParent);
        var item = new ToastItem
        {
            go = go,
            rt = go.GetComponent<RectTransform>(),
            cg = go.GetComponent<CanvasGroup>() ?? go.AddComponent<CanvasGroup>(),
            tmp = go.GetComponentInChildren<TextMeshProUGUI>(true),
            bg = go.GetComponentInChildren<Image>(true)
        };

        // 중앙 앵커/피벗 고정 (캔버스 중앙 기준)
        item.rt.anchorMin = item.rt.anchorMax = item.rt.pivot = new Vector2(0.5f, 0.5f);

        // 입력 막지 않도록
        item.cg.interactable = false;
        item.cg.blocksRaycasts = false;
        item.cg.ignoreParentGroups = true;

        // 텍스트/배경 Raycast 끄기 권장
        if (item.tmp) item.tmp.raycastTarget = false;
        if (item.bg) item.bg.raycastTarget = false;

        go.SetActive(false);
        return item;
    }

    private ToastItem GetFromPool() => pool.Count > 0 ? pool.Dequeue() : CreateInstance();

    private void ReturnToPool(ToastItem t)
    {
        t.go.SetActive(false);
        pool.Enqueue(t);
    }

    // ----------------- Layer Toggle -----------------
    private void EnsureLayerOn()
    {
        if (!autoToggleLayer || toastParent == null) return;
        if (!toastParent.gameObject.activeSelf)
            toastParent.gameObject.SetActive(true);
        if (layerOffCo != null) { StopCoroutine(layerOffCo); layerOffCo = null; }
    }

    private void TryScheduleLayerOff()
    {
        if (!autoToggleLayer || toastParent == null) return;
        if (liveToasts > 0) return;
        if (layerOffCo != null) { StopCoroutine(layerOffCo); layerOffCo = null; }
        layerOffCo = StartCoroutine(CoLayerOffAfterDelay());
    }

    private IEnumerator CoLayerOffAfterDelay()
    {
        float t = 0f;
        while (t < layerOffDelay)
        {
            t += Time.unscaledDeltaTime;
            if (liveToasts > 0) yield break;
            yield return null;
        }
        if (liveToasts == 0) toastParent.gameObject.SetActive(false);
        layerOffCo = null;
    }

    public void SetLayerActive(bool on)
    {
        if (toastParent == null) return;
        toastParent.gameObject.SetActive(on);
        if (!on)
        {
            liveToasts = 0;
            if (layerOffCo != null) { StopCoroutine(layerOffCo); layerOffCo = null; }
        }
    }

    // ----------------- Public API -----------------
    public void ShowToastCentered(string message, ToastMood mood, float yOffset = 0f)
    {
        ShowToast(message, new Vector2(0f, yOffset), mood);
    }

    public void ShowToast(string message, Vector2 anchoredPosition, ToastMood mood)
    {
        EnsureLayerOn();

        var item = GetFromPool();
        PrepareItem(item, message, mood);

        var xJitter = Random.Range(jitterX.x, jitterX.y);
        item.rt.localScale = Vector3.one;
        item.rt.anchoredPosition = anchoredPosition + new Vector2(xJitter, 0f);

        item.go.SetActive(true);
        liveToasts++;
        StartCoroutine(AnimateToast(item, mood));
    }

    // ----------------- Internals -----------------
    private void PrepareItem(ToastItem item, string message, ToastMood mood)
    {
        if (item.tmp)
        {
            item.tmp.text = message;
            item.tmp.enableWordWrapping = false;
            item.tmp.enableAutoSizing = false;
            item.tmp.color = (mood == ToastMood.Positive) ? positiveTextColor : negativeTextColor;
        }
        if (item.bg)
        {
            item.bg.color = (mood == ToastMood.Positive) ? positiveBgColor : negativeBgColor;
        }
        // 알파는 CanvasGroup으로 제어 (텍스트/배경 동시에 페이드)
        item.cg.alpha = 0f;
    }

    private IEnumerator AnimateToast(ToastItem item, ToastMood mood)
    {
        float elapsed = 0f;
        Vector2 start = item.rt.anchoredPosition;
        Vector2 end = start + ((mood == ToastMood.Positive) ? riseOffsetUp : riseOffsetDown);

        AnimationCurve aCurve = (mood == ToastMood.Positive) ? alphaUp : alphaDown;
        AnimationCurve sCurve = (mood == ToastMood.Positive) ? scaleUp : scaleDown;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;               // 타임스케일 0에서도 동작
            float p = Mathf.Clamp01(elapsed / duration);

            item.rt.anchoredPosition = Vector2.LerpUnclamped(start, end, p);
            float s = sCurve.Evaluate(p);
            item.rt.localScale = new Vector3(s, s, 1f);

            item.cg.alpha = Mathf.Clamp01(aCurve.Evaluate(p)); // ★ 텍스트+배경 동시 페이드

            yield return null;
        }

        ReturnToPool(item);
        liveToasts = Mathf.Max(0, liveToasts - 1);
        TryScheduleLayerOff();
    }
}
