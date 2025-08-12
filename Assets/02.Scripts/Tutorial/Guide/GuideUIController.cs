using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class GuideUIController : MonoBehaviour
{
    public static GuideUIController Instance;

    [Header("UI 요소")]
    public CanvasGroup guideCanvasGroup;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;

    [Header("Fade 설정")]
    public float fadeDuration = 0.5f;

    private Coroutine fadeCoroutine;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    /// <summary>
    /// 타이틀과 설명을 보여주며 페이드인
    /// </summary>
    public void ShowGuide(string title, string description)
    {
        titleText.text = title;
        descriptionText.text = description;

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeIn());
    }

    /// <summary>
    /// UI를 즉시 사라지게 함 (페이드 없음)
    /// </summary>
    public void HideGuideInstant()
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        guideCanvasGroup.alpha = 0f;
        guideCanvasGroup.gameObject.SetActive(false);
    }

    /// <summary>
    /// UI를 페이드아웃 후 사라지게 함
    /// </summary>
    public void FadeOutGuide()
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeOut());
    }

    private IEnumerator FadeIn()
    {
        guideCanvasGroup.alpha = 0f;
        guideCanvasGroup.gameObject.SetActive(true);
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            guideCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        guideCanvasGroup.alpha = 1f;
    }

    private IEnumerator FadeOut()
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            guideCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        guideCanvasGroup.alpha = 0f;
        guideCanvasGroup.gameObject.SetActive(false);
    }

    /// <summary>
    /// 지정된 시간 후 페이드아웃
    /// </summary>
    public void FadeOutGuideAfterDelay(float delay)
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeOutWithDelay(delay));
    }

    private IEnumerator FadeOutWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        yield return FadeOut();
    }
}
