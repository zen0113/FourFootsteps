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
    [Tooltip("Dialogue Canvas의 Dialogue Cover Panel 할당")]
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

    [Header("FootprintsOfMemory Canvas 프리팹")]
    [SerializeField] private GameObject footprintsCanvasPrefab;
    private const string footprintsCanvasName = "FootprintsOfMemory Canvas";

    private readonly Dictionary<eUIGameObjectName, GameObject> uiGameObjects = new();
    private Q_Vignette_Single warningVignetteQVignetteSingle;
    private Q_Vignette_Single hidingVignetteQVignetteSingle;

    public static UIManager Instance { get; private set; }

    // 은신 비네팅 깜빡임 효과 활성화 변수
    private bool isBlinkHidingActive = false;

    private Coroutine fadeOutRoutine;

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

        RegisterPuzzleBagButtonEvent();
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

    public void SetAllUI(bool isActive)
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

    private void RegisterPuzzleBagButtonEvent()
    {
        var button = puzzleBagButton.GetComponentInChildren<Button>(true);
        // 씬에 FootprintsOfMemory Canvas 존재 확인, 없으면 프리팹 생성
        EnsureFootprintsCanvasExists();
        // 버튼에 아무 것도 등록되지 않았다면 OnClickPuzzleBag 등록
        if (button.onClick.GetPersistentEventCount() == 0)
        {
            button.onClick.AddListener(() =>
            {
                if (PuzzleMemoryManager.Instance != null)
                    PuzzleMemoryManager.Instance.OnClickPuzzleBag();
                else
                    Debug.LogWarning("[PuzzleBagButtonBinder] PuzzleMemoryManager.Instance가 null입니다.");
            });
        }
    }

    private void EnsureFootprintsCanvasExists()
    {
        var existing = GameObject.Find(footprintsCanvasName);
        if (existing != null) return; // 이미 있음

        // 프리팹 레퍼런스가 할당되지 않았다면 에디터 환경에서 경로로 로드 시도
        if (footprintsCanvasPrefab == null)
        {
#if UNITY_EDITOR
            // 사용자 제공 경로: Assets\03.Prefabs\Canvas\FootprintsOfMemory Canvas.prefab
            string assetPath = "Assets/03.Prefabs/Canvas/FootprintsOfMemory Canvas.prefab";
            footprintsCanvasPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (footprintsCanvasPrefab == null)
            {
                Debug.LogWarning($"[PuzzleBagButtonBinder] 프리팹을 경로에서 찾지 못했습니다: {assetPath}. " +
                                 $"인스펙터에 프리팹을 직접 할당해 주세요.");
                return;
            }
#else
            Debug.LogWarning("[PuzzleBagButtonBinder] footprintsCanvasPrefab이 비어 있고 런타임에서는 경로 로드를 할 수 없습니다. " +
                             "인스펙터에 프리팹을 할당해 주세요.");
            return;
#endif
        }

        var spawned = Instantiate(footprintsCanvasPrefab);
        // 씬에서 이름으로 탐색하기 쉽게 동일 이름 보장
        spawned.name = footprintsCanvasName;
        // Debug.Log("[PuzzleBagButtonBinder] FootprintsOfMemory Canvas 프리팹을 씬에 생성했습니다.");
    }

    public void StartWhiteOut(bool isEnding)
    {
        // 중복 실행 방지
        if (fadeOutRoutine != null) StopCoroutine(fadeOutRoutine);

        fadeOutRoutine = StartCoroutine(WhiteOutFlow(false, isEnding));
    }
    public void StartBlackOut(bool isEnding)
    {
        // 중복 실행 방지
        if (fadeOutRoutine != null) StopCoroutine(fadeOutRoutine);

        fadeOutRoutine = StartCoroutine(WhiteOutFlow(true, isEnding));
    }

    private IEnumerator WhiteOutFlow(bool isBlack,bool isEnding)
    {
        if(isBlack)
            yield return StartCoroutine(OnFade(dialogueCoverPanel, 0, 1, 2f, true, 2f, 0f));
        else
            yield return StartCoroutine(OnFade(null, 0, 1, 2f, true, 2f, 0f, false));

        if(isEnding)
            TutorialController.Instance.SetNextTutorial();
        fadeOutRoutine = null;
    }

    /// <summary> 변수 설명
    /// <param name="fadeObject"> fade 효과를 적용할 물체 (null을 주면 화면 전체)
    /// <param name="start"><param name="end"> start = 1, end = 0 이면 밝아짐 start = 0, end = 1이면 어두워짐
    /// <param name="fadeTime"> 밝아짐(또는 어두워짐)에 걸리는 시간
    /// <param name="blink"> true이면 어두워졌다가 밝아짐
    /// <param name="waitingTime"> blink가 true일 때 어두워져 있는 시간
    /// <param name="changeFadeTime"> 다시 밝아질 때 걸리는 시간을 조정하고 싶으면 쓰는 변수
    /// </summary>
    public IEnumerator OnFade(Image fadeObject, float start, float end, float fadeTime, bool blink = false, float waitingTime = 0f, float changeFadeTime = 0f, bool Black = true)
    {
        if (!fadeObject)
            fadeObject = coverPanel;

        if (!fadeObject.gameObject.activeSelf)
            fadeObject.gameObject.SetActive(true);

        Color newColor = fadeObject.color;
        if (!Black)
            newColor = Color.white;
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

    /// <summary>
    /// CanvasGroup 알파를 duration 동안 0↔1로 보간합니다.
    /// </summary>
    /// <param name="cg">대상 CanvasGroup</param>
    /// <param name="toVisible">true면 0→1, false면 1→0</param>
    /// <param name="duration">지속 시간(초). 0이하이면 즉시 적용</param>
    /// <param name="setInteractableAndBlocksRaycasts">
    /// 완료 시 cg.interactable / cg.blocksRaycasts를 표시 상태에 맞게 설정할지 여부
    /// </param>
    /// <param name="useUnscaledTime">Time.timeScale 무시 여부(보통 UI는 true 권장)</param>
    public IEnumerator FadeCanvasGroup(
        CanvasGroup cg,
        bool toVisible,
        float duration,
        bool setInteractableAndBlocksRaycasts = true,
        bool useUnscaledTime = true)
    {
        if (!cg)
            yield break;

        float start = cg.alpha;
        float target = toVisible ? 1f : 0f;

        if (duration <= 0f || Mathf.Approximately(start, target))
        {
            cg.alpha = target;

            if (setInteractableAndBlocksRaycasts)
            {
                cg.interactable = toVisible;
                cg.blocksRaycasts = toVisible;
            }
            yield break;
        }

        // 애니 중엔 입력 막기(옵션)
        bool origInteractable = cg.interactable;
        bool origBlocksRaycasts = cg.blocksRaycasts;

        if (setInteractableAndBlocksRaycasts)
        {
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }

        float t = 0f;
        while (t < duration)
        {
            t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            cg.alpha = Mathf.Lerp(start, target, p);
            yield return null;
        }

        // 스냅 & 최종 상태
        cg.alpha = target;

        if (setInteractableAndBlocksRaycasts)
        {
            cg.interactable = toVisible;
            cg.blocksRaycasts = toVisible;
        }
        else
        {
            // 입력 상태 복원
            cg.interactable = origInteractable;
            cg.blocksRaycasts = origBlocksRaycasts;
        }
    }


}
