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

    private Coroutine fadeCoroutine;
    private RectTransform rectTransform;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        rectTransform = GetComponent<RectTransform>();
    }

    public void ShowBubble(string message, Sprite faceSprite)
    {
        // 말풍선 위치를 원래 위치(앵커 기준)로 설정합니다.
        rectTransform.anchoredPosition = Vector2.zero;
        InternalShow(message, faceSprite);
    }


    /// <param name="screenPosition">말풍선이 표시될 화면상의 픽셀 좌표 (x, y)</param>
    public void ShowBubble(string message, Sprite faceSprite, Vector2 screenPosition)
    {
        rectTransform.position = screenPosition; // 전달받은 스크린 좌표로 위치 설정
        InternalShow(message, faceSprite);
    }

    private void InternalShow(string message, Sprite faceSprite)
    {
        messageText.text = message;
        if (faceSprite != null)
        {
            faceImage.gameObject.SetActive(true);
            faceImage.sprite = faceSprite;
        }
        else
        {
            faceImage.gameObject.SetActive(false);
        }

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeIn());
    }

    public void HideBubbleInstant()
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        bubbleCanvasGroup.alpha = 0f;
        bubbleCanvasGroup.gameObject.SetActive(false);
    }

    public void FadeOutBubble()
    {
        if (!bubbleCanvasGroup.gameObject.activeSelf || bubbleCanvasGroup.alpha == 0) return;

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeOut());
    }

    public void FadeOutBubbleAfterDelay(float delay)
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeOutWithDelay(delay));
    }

    private IEnumerator FadeIn()
    {
        bubbleCanvasGroup.alpha = 0f;
        bubbleCanvasGroup.gameObject.SetActive(true);
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

    private IEnumerator FadeOutWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        yield return StartCoroutine(FadeOut());
    }
}