using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum eUIGameObjectName
{
    WarningVignette,
    CatVersionUIGroup,
    HumanVersionUIGroup,
    HeartParent,
    ResponsibilityGroup,
    ResponsibilityGauge
}

public class UIManager : MonoBehaviour
{
    //[Header("Screen Effect")]
    //public Image coverPanel;
    //[SerializeField] private TextMeshProUGUI coverText;

    [Header("UI Game Objects")]
    public GameObject warningVignette;
    public GameObject heartParent;
    public GameObject responsibilityGroup;
    public GameObject responsibilityGauge;

    [HideInInspector] public Slider responsibilitySlider;

    [Header("Warning Vignette Settings")]
    [SerializeField] protected float warningTime;

    [Header("Player Version Object Group")]
    public GameObject catVersionUIGroup;
    public GameObject humanVersionUIGroup;

    private readonly Dictionary<eUIGameObjectName, GameObject> uiGameObjects = new();
    private Q_Vignette_Single warningVignetteQVignetteSingle;

    public static UIManager Instance { get; private set; }

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
    }


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void AddUIGameObjects()
    {
        uiGameObjects.Add(eUIGameObjectName.WarningVignette, warningVignette);

        uiGameObjects.Add(eUIGameObjectName.CatVersionUIGroup, catVersionUIGroup);
        uiGameObjects.Add(eUIGameObjectName.HumanVersionUIGroup, humanVersionUIGroup);

        uiGameObjects.Add(eUIGameObjectName.HeartParent, heartParent);
        uiGameObjects.Add(eUIGameObjectName.ResponsibilityGroup, responsibilityGroup);
        uiGameObjects.Add(eUIGameObjectName.ResponsibilityGauge, responsibilityGauge);

        warningVignetteQVignetteSingle = warningVignette.GetComponent<Q_Vignette_Single>();
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
}
