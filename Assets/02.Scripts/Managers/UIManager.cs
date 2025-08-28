using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum eUIGameObjectName
{
    WarningVignette,
    HidingVignette,
    CatVersionUIGroup,
    HumanVersionUIGroup,
    ResponsibilityGroup,
    ResponsibilityGauge,
    PlaceUI,
    PuzzleBagButton
}

public class UIManager : MonoBehaviour
{
    [Header("Screen Effect")]
    public Image coverPanel;
    public Image dialogueCoverPanel;
    //[SerializeField] private TextMeshProUGUI coverText;

    [Header("UI Game Objects")]
    public GameObject warningVignette;
    public GameObject hidingVignette;
    public GameObject heartParent;
    public GameObject responsibilityGroup;
    public GameObject responsibilityGauge;
    public GameObject placeUI;
    public GameObject puzzleBagButton;

    [HideInInspector] public Slider responsibilitySlider;

    [Header("Warning Vignette Settings")]
    [SerializeField] protected float warningTime;

    [Header("Player Version Object Group")]
    public GameObject catVersionUIGroup;
    public GameObject humanVersionUIGroup;

    private readonly Dictionary<eUIGameObjectName, GameObject> uiGameObjects = new();
    private Q_Vignette_Single warningVignetteQVignetteSingle;
    private Q_Vignette_Single hidingVignetteQVignetteSingle;

    public static UIManager Instance { get; private set; }

    // 은신 비네팅 깜빡임 효과 활성화 변수
    private bool isBlinkHidingActive = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
            Destroy(gameObject);

        AddUIGameObjects();
        SetAllUI(false);

    }

    private void AddUIGameObjects()
    {
        uiGameObjects.Add(eUIGameObjectName.WarningVignette, warningVignette);
        uiGameObjects.Add(eUIGameObjectName.HidingVignette, hidingVignette);

        uiGameObjects.Add(eUIGameObjectName.CatVersionUIGroup, catVersionUIGroup);
        uiGameObjects.Add(eUIGameObjectName.HumanVersionUIGroup, humanVersionUIGroup);

        uiGameObjects.Add(eUIGameObjectName.ResponsibilityGroup, responsibilityGroup);
        uiGameObjects.Add(eUIGameObjectName.ResponsibilityGauge, responsibilityGauge);
        uiGameObjects.Add(eUIGameObjectName.PlaceUI, placeUI);
        uiGameObjects.Add(eUIGameObjectName.PuzzleBagButton, puzzleBagButton);

        warningVignetteQVignetteSingle = warningVignette.GetComponent<Q_Vignette_Single>();
        hidingVignetteQVignetteSingle = hidingVignette.GetComponent<Q_Vignette_Single>();
        responsibilitySlider = responsibilityGauge.GetComponent<Slider>();
    }

    private void SetAllUI(bool isActive)
    {
        foreach (var ui in uiGameObjects)
            SetUI(ui.Key, isActive);
    }

    public void SetUI(eUIGameObjectName uiName, bool isActive)
    {
        GameObject targetUI = uiGameObjects[uiName];

        targetUI.SetActive(isActive);
    }

    public GameObject GetUI(eUIGameObjectName uiName)
    {
        return uiGameObjects[uiName];
    }


    // <summary> 변수 설명
    // fadeObject는 fade 효과를 적용할 물체 (null을 주면 화면 전체)
    // start = 1, end = 0 이면 밝아짐 start = 0, end = 1이면 어두워짐
    // fadeTime은 밝아짐(또는 어두워짐)에 걸리는 시간
    // blink가 true이면 어두워졌다가 밝아짐
    // waitingTime은 blink가 true일 때 어두워져 있는 시간
    // changeFadeTime은 다시 밝아질 때 걸리는 시간을 조정하고 싶으면 쓰는 변수
    // </summary>
    public IEnumerator OnFade(Image fadeObject, float start, float end, float fadeTime, bool blink = false, float waitingTime = 0f, float changeFadeTime = 0f)
    {
        if (!fadeObject)
            fadeObject = coverPanel;

        if (!fadeObject.gameObject.activeSelf)
            fadeObject.gameObject.SetActive(true);
        Color newColor = fadeObject.color;
        newColor.a = start;
        fadeObject.color = newColor;

        float current = 0, percent = 0;

        while (percent < 1 && fadeTime != 0)
        {
            current += Time.deltaTime;
            percent = current / fadeTime;

            newColor.a = Mathf.Lerp(start, end, percent);
            fadeObject.color = newColor;

            yield return null;
        }
        newColor.a = end;
        fadeObject.color = newColor;

        // 곧바로 다시 어두워지거나 밝아지게 하고 싶을 때
        if (blink)
        {
            yield return new WaitForSeconds(waitingTime);
            StartCoroutine(OnFade(fadeObject, end, start, fadeTime + changeFadeTime, false, 0, 0));
        }

        // 투명해졌으면 끈다
        if (end == 0)
        {
            fadeObject.gameObject.SetActive(false);
        }
    }

    /*
 * startAlpha: 경고 표시 시작 시 투명도
 * endAlpha: 경고 표시 종료 시 투명도
 * fadeInDuration: 경고 표시 페이드 인 소요 시간
 * fadeOutDuration: 경고 표시 페이드 아웃 소요 시간
 */
    public IEnumerator WarningCoroutine(float startAlpha = 0f,
        float endAlpha = 1f,
        float fadeInDuration = 0.5f,
        float fadeOutDuration = 0.5f)
    {
        warningVignette.SetActive(true); // 경고 표시 활성화

        float timeAccumulated = 0;
        while (timeAccumulated < fadeInDuration)
        {
            timeAccumulated += Time.deltaTime;
            warningVignetteQVignetteSingle.mainColor.a = Mathf.Lerp(startAlpha,
                endAlpha,
                timeAccumulated / fadeInDuration); // WarningVignette 투명도를 0에서 1로 선형 보간(Lerp)

            yield return null;
        }

        yield return new WaitForSeconds(warningTime); // warningTime 동안 경고 상태 유지

        timeAccumulated = 0; // 경고 종료: WarningVignette.mainColor.a를 다시 0으로 페이드 아웃
        while (timeAccumulated < fadeOutDuration)
        {
            timeAccumulated += Time.deltaTime * 2; // 페이드 아웃 속도를 더 빠르게 설정
            warningVignetteQVignetteSingle.mainColor.a = Mathf.Lerp(endAlpha,
                startAlpha,
                timeAccumulated / fadeOutDuration); // WarningVignette 투명도를 1에서 0으로 선형 보간(Lerp)

            yield return null;
        }

        warningVignette.SetActive(false); // 경고 표시 비활성화
    }

    // 스테이지3 은신 미니게임에서 은신했을 경우 VFX 비네팅 효과
    public IEnumerator HidingCoroutine( bool isHiding, float fadeInDuration = 1f)
    {
        float startAlpha, endAlpha;
        if (isHiding)
        {
            startAlpha = 0f; 
            endAlpha = 1f;
        }
        else
        {
            startAlpha = 1f;
            endAlpha = 0f;
        }

        hidingVignette.SetActive(true); // 은신 비네팅 활성화

        float timeAccumulated = 0;
        while (timeAccumulated < fadeInDuration)
        {
            timeAccumulated += Time.deltaTime;
            hidingVignetteQVignetteSingle.mainColor.a = Mathf.Lerp(startAlpha,
                endAlpha,
                timeAccumulated / fadeInDuration); // WarningVignette 투명도를 0에서 1로 선형 보간(Lerp)

            yield return null;
        }

        if(!isHiding)
            hidingVignette.SetActive(false); // 은신 비네팅 비활성화
    }

    // 은신 비네팅 깜빡임 효과 코루틴
    private IEnumerator BlinkHidingCoroutine(float blinkHidingSpeed =2f)
    {
        while (isBlinkHidingActive)
        {
            // 깜빡이는 효과
            float alpha = Mathf.PingPong(Time.time * blinkHidingSpeed, 1f); // 알파값 깜빡임 효과
            hidingVignetteQVignetteSingle.mainColor.a = 0.5f + (alpha * 0.5f); // 0.5 ~ 1.0 사이로 알파값 조정

            yield return null;
        }

        // 종료 시 원래 색상으로 복원
        Color finalColor = hidingVignetteQVignetteSingle.mainColor;
        finalColor.a = 1f;
        hidingVignetteQVignetteSingle.mainColor = finalColor;
    }

    public void SetBlinkHidingCoroutine(bool isActive)
    {
        if (isActive)
        {
            isBlinkHidingActive = true;
            StartCoroutine(BlinkHidingCoroutine());
        }
        else
        {
            StopCoroutine(BlinkHidingCoroutine());
            isBlinkHidingActive = false;
        }
    }
}
