using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EndingCredit : MonoBehaviour
{
    private float fadeInOutTime = 2f;
    private float endingCreditTime = 3f;

    // 싱글턴 인스턴스
    protected static EndingCredit instance;
    public static EndingCredit Instance
    {
        get
        {
            if (instance == null)
            {
                var obj = FindObjectOfType<EndingCredit>();
                instance = obj != null ? obj : Create();
            }
            return instance;
        }
        private set => instance = value;
    }

    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Animator animator;
    [SerializeField] private TextMeshProUGUI creditText;
    [SerializeField] private Image backgroundImage;
    // backgroundSprites[0]은 해피엔딩때 크레딧 배경
    // backgroundSprites[1]은 배드엔딩때 크레딧 배경
    [SerializeField] private Sprite[] backgroundSprites;

    string animationName = "EndingCredit_Moving";
    string happyEndingSceneName = "Ending_Happy";
    bool isHappyEnding = false;
    const int happyBG = 0;
    const int badBG = 1;

    // Resources 폴더에서 EndingCredit 프리팹을 생성
    public static EndingCredit Create()
    {
        return Instantiate(Resources.Load<EndingCredit>("Prefabs/EndingCredit Canvas"));
    }

    private void Awake()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (animator == null) animator = GameObject.Find("Credit Panel").GetComponent<Animator>();
        if(creditText==null) creditText = GameObject.Find("credit Text").GetComponent<TextMeshProUGUI>();
    }

    void Start()
    {
        string creditString = creditText.text;
        creditString = ProcessPlaceholders(creditString);
        creditText.text = creditString;

        canvasGroup.alpha = 0f;
        animator.speed = 0f;

        backgroundImage.sprite = (isHappyEnding) ? backgroundSprites[happyBG] : backgroundSprites[badBG];

        //// 게임 데이터 초기화
        ////SaveManager.Instance.LoadInitGameData();
        //SaveManager.Instance.CreateNewGameData();
    }

    // 엔딩크레딧 연출 시작
    public IEnumerator StartEndingCredit()
    {
        if (SceneManager.GetActiveScene().name == happyEndingSceneName) isHappyEnding = true;

        if (isHappyEnding)
            yield return new WaitForSeconds(endingCreditTime);

        yield return StartCoroutine(Fade(true));

        // 엔딩 크레딧 스크롤 애니메이션 재생
        animator.speed = 1f;

        // 애니메이션이 재생 중일 동안 대기
        while (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f &&
               animator.GetCurrentAnimatorStateInfo(0).IsName(animationName))
        {
            yield return null;
        }

        // 해피엔딩에선 일러스트 끄고 밝은 배경으로 미리 준비
        if (isHappyEnding)
            TutorialController.Instance.SetNextTutorial();

        // 크레딧 다 스크롤링 된 후 3초 대기
        yield return new WaitForSeconds(endingCreditTime);

        // 페이드 아웃
        yield return StartCoroutine(Fade(false));

        // 게임 데이터 초기화
        //SaveManager.Instance.LoadInitGameData();
        SaveManager.Instance.CreateNewGameData();

        // 타이틀씬으로 이동
        GameManager.Instance.LoadTitleScene();
    }


    // 페이드 인/아웃 애니메이션
    private IEnumerator Fade(bool isFadeIn)
    {
        float timer = 0f;
        float startAlpha = isFadeIn ? 0f : 1f;
        float endAlpha = isFadeIn ? 1f : 0f;

        while (timer <= fadeInOutTime)
        {
            yield return null;
            timer += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, timer);
        }

        if (!isFadeIn)
        {
            gameObject.SetActive(false);
        }
    }

    private string ProcessPlaceholders(string originalString)
    {
        string playerName = (GameManager.Instance.GetVariable("PlayerName") as string) ?? "";

        string modifiedString = originalString.Replace("`", ",")
                       .Replace("", "")
                       .Replace("\u0008", "");

        // 한 번에 모든 패턴(괄호/슬래시/단일조사/단독)을 처리
        modifiedString = KoreanJosa.Apply(
            modifiedString,
            ("PlayerName", playerName)
        );

        return modifiedString;
    }
}
