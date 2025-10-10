using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 얼굴 이미지와 텍스트가 포함된 말풍선 UI를 제어하는 싱글턴 클래스입니다.
/// </summary>
public class SpeechBubbleController : MonoBehaviour
{
    public static SpeechBubbleController Instance;

    [Header("UI 요소")]
    [SerializeField] private CanvasGroup bubbleCanvasGroup;
    [SerializeField] private Image faceImage;
    [SerializeField] private TextMeshProUGUI messageText;

    [Header("설정")]
    [SerializeField] private float fadeDuration = 0.3f;

    private Coroutine displayCoroutine;
    private RectTransform rectTransform;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        rectTransform = GetComponent<RectTransform>();
        bubbleCanvasGroup.alpha = 0f;
        bubbleCanvasGroup.gameObject.SetActive(false);
    }

    // --- 💡 [추가] 말풍선을 즉시 숨기는 함수 ---
    public void HideBubble()
    {
        // 진행 중인 모든 애니메이션(코루틴)을 즉시 중단
        if (displayCoroutine != null)
        {
            StopCoroutine(displayCoroutine);
            displayCoroutine = null;
        }
        // 알파 값을 0으로 만들고 게임 오브젝트를 비활성화
        bubbleCanvasGroup.alpha = 0f;
        bubbleCanvasGroup.gameObject.SetActive(false);
    }

    public void ShowBubble(string message, Sprite faceSprite)
    {
        rectTransform.anchoredPosition = Vector2.zero;
        StartDisplayCoroutine(FadeIn(message, faceSprite));
    }

    public void ShowBubbleForDuration(string message, Sprite faceSprite, float duration)
    {
        rectTransform.anchoredPosition = Vector2.zero;
        StartDisplayCoroutine(ShowAndFadeOut(message, faceSprite, duration));
    }

    public void FadeOutBubble()
    {
        StartDisplayCoroutine(FadeOut());
    }

    private void StartDisplayCoroutine(IEnumerator routine)
    {
        if (displayCoroutine != null)
        {
            StopCoroutine(displayCoroutine);
        }
        displayCoroutine = StartCoroutine(routine);
    }

    private IEnumerator FadeIn(string message, Sprite faceSprite)
    {
        bubbleCanvasGroup.gameObject.SetActive(true);
        messageText.text = message;
        faceImage.sprite = faceSprite;
        faceImage.gameObject.SetActive(faceSprite != null);

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            bubbleCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        bubbleCanvasGroup.alpha = 1f;
    }

    private IEnumerator FadeOut()
    {
        if (!bubbleCanvasGroup.gameObject.activeSelf || bubbleCanvasGroup.alpha == 0) yield break;

        float startAlpha = bubbleCanvasGroup.alpha;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            bubbleCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        bubbleCanvasGroup.alpha = 0f;
        bubbleCanvasGroup.gameObject.SetActive(false);
    }

    private IEnumerator ShowAndFadeOut(string message, Sprite faceSprite, float duration)
    {
        yield return StartCoroutine(FadeIn(message, faceSprite));
        yield return new WaitForSeconds(duration);
        yield return StartCoroutine(FadeOut());
    }
}