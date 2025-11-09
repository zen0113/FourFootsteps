using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

// 씬 전환 시 로딩 화면을 보여주는 싱글턴 클래스
public class SceneLoader : MonoBehaviour
{
    private float fadeInOutTime = 2f;

    // 싱글턴 인스턴스
    protected static SceneLoader instance;
    public static SceneLoader Instance
    {
        get
        {
            if (instance == null)
            {
                var obj = FindObjectOfType<SceneLoader>();
                instance = obj != null ? obj : Create();
            }
            return instance;
        }
        private set => instance = value;
    }

    [SerializeField] private CanvasGroup sceneLoaderCanvasGroup;
    [SerializeField] private List<GameObject> loadingPuzzlePieces = new List<GameObject>();
    // 회상에서 스테이지로 넘어갈 때 하얀 퍼즐 트랜지션 사용
    [SerializeField] private List<GameObject> loadingPuzzlePiecesWhite = new List<GameObject>();
    [SerializeField] private GameObject CatAnimUI;
    //[SerializeField] private Image progressBar;

    private string loadSceneName;
    private string previousSceneName;

    private const string STAGE_SCENE_NAME = "StageScene";
    private const string RECALL_SCENE_NAME = "RecallScene";
    private const string ENDING_SCENE_NAME = "Ending";
    private bool isFading = false;

    // Resources 폴더에서 SceneLoader 프리팹을 생성
    public static SceneLoader Create()
    {
        return Instantiate(Resources.Load<SceneLoader>("Prefabs/Loading Canvas"));
    }

    private void Awake()
    {
        if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
    }

    // 씬 로드를 시작
    public void LoadScene(string sceneName)
    {
        GameManager.Instance.StartSceneLoad();

        previousSceneName = SceneManager.GetActiveScene().name;
        gameObject.SetActive(true);
        loadSceneName = sceneName;
        SceneManager.sceneLoaded += OnSceneLoaded;
        StartCoroutine(Load(sceneName));
    }

    // 씬 비동기 로드 및 진행률 표시
    private IEnumerator Load(string sceneName)
    {
        StartFadeCoroutine(true);
        yield return new WaitWhile(()=>isFading);

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        while (!op.isDone)
        {
            yield return null;

            if (!(op.progress < 0.9f))
            {
                op.allowSceneActivation = true;
                yield break;
            }
        }
    }

    // 로딩된 씬이 목표 씬인지 확인 후 페이드아웃
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == loadSceneName)
        {
            StartFadeCoroutine(false);

            SceneManager.sceneLoaded -= OnSceneLoaded;

            GameManager.Instance.FinishSceneLoad();

            if (GameManager.Instance != null)
            {
                // 씬이 로드된 후, 자동으로 GameManager 업데이트
                GameManager.Instance.UpdateSceneProgress(scene.name);
            }
        }
    }


    // --------------단순 Fade인 경우--------------
    // previousSceneName이 TitleScene||Prologue 일 때
    // previousSceneName과 loadSceneName 둘다 string에 “StageScene” 문자열 포함일때
    // --------------퍼즐 피스 Fade인 경우--------------
    // previousSceneName이 TitleScene||Prologue 아닐 때
    // (previousSceneName에 “StageScene” 문자열 포함 AND loadSceneName에 “RecallScene” 문자열 포함) OR 
    // (previousSceneName에 “RecallScene” 문자열 포함 AND loadSceneName에 “StageScene” 문자열 포함)
    // loadSceneName.Contains("Ending") 일 때
    private void StartFadeCoroutine(bool isFadein)
    {
        bool isFromTitleOrPrologue = previousSceneName == "TitleScene" || previousSceneName == "Prologue";
        bool isStageToStage = previousSceneName.Contains(STAGE_SCENE_NAME) && loadSceneName.Contains(STAGE_SCENE_NAME);
        bool isStageRecallTransition = (previousSceneName.Contains(STAGE_SCENE_NAME) && loadSceneName.Contains(RECALL_SCENE_NAME))
                                       || (previousSceneName.Contains(RECALL_SCENE_NAME) && loadSceneName.Contains(STAGE_SCENE_NAME));
        bool isToEnding = loadSceneName.Contains(ENDING_SCENE_NAME);

        if (!isFromTitleOrPrologue && (isStageRecallTransition || isToEnding))
        {
            // 퍼즐 피스 Fade
            StartCoroutine(PuzzleFade(isFadein));
            Debug.Log($"퍼즐 피스 Fade 실행 : {loadSceneName}");
        }
        else if (isFromTitleOrPrologue || isStageToStage)
        {
            // 단순 고양이 애니 Fade
            StartCoroutine(CatAnimFade(isFadein));
            Debug.Log($"단순 고양이 애니 Fade 실행 : {loadSceneName}");
        }
        else
        {
            Debug.LogWarning($"Unexpected scene transition: {previousSceneName} -> {loadSceneName}");
            StartCoroutine(CatAnimFade(isFadein)); // 기본 동작
        }
    }

    // 단순 고양이 애니메이션 Fade
    private IEnumerator CatAnimFade(bool isFadeIn)
    {
        isFading = true;
        float timer = 0f;
        float startAlpha = isFadeIn ? 0f : 1f;
        float endAlpha = isFadeIn ? 1f : 0f;
        if (isFadeIn) CatAnimUI.SetActive(true);

        while (timer <= fadeInOutTime)
        {
            yield return null;
            //timer += Time.unscaledDeltaTime * 2f;
            timer += Time.unscaledDeltaTime;
            sceneLoaderCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, timer);
        }

        if (!isFadeIn)
        {
            CatAnimUI.SetActive(false);
            gameObject.SetActive(false);
        }
        isFading = false;
    }

    // 퍼즐 피스 Fade
    private IEnumerator PuzzleFade(bool isFadeIn)
    {
        isFading = true;
        bool isRecallToStage = previousSceneName.Contains(RECALL_SCENE_NAME);
        sceneLoaderCanvasGroup.alpha = 1f;
        // 사용할 퍼즐 리스트 선택
        List<GameObject> targetPuzzles = isRecallToStage ? loadingPuzzlePiecesWhite : loadingPuzzlePieces;

        // 0~(퍼즐개수-1)까지 리스트 만들고 셔플
        List<int> indices = Enumerable.Range(0, targetPuzzles.Count).ToList();
        ShuffleList(indices);

        // 한 퍼즐당 대략 실행 간격 계산
        float interval = fadeInOutTime / targetPuzzles.Count;

        foreach (int idx in indices)
        {
            // null 체크 후 활성화
            if (targetPuzzles[idx] != null)
            {
                targetPuzzles[idx].SetActive(isFadeIn);
            }

            yield return new WaitForSecondsRealtime(interval);
        }

        // 마지막 처리
        if (!isFadeIn)
        {
            gameObject.SetActive(false);
        }
        isFading = false;
    }

    // 셔플 로직 분리
    private void ShuffleList<T>(List<T> list)
    {
        System.Random rand = new System.Random();
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rand.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    

    //    // -------------------------------에디터에서 오브젝트 70개 자동할당-----------------------------------
    //    // 실행 시 쓰이진 않음! 에디터에서 편의용으로 둔것
    //    [Header("검색 규칙")]
    //    [SerializeField] private string layerPrefix = "layer";
    //    [SerializeField] private int minIndex = 0;
    //    [SerializeField] private int maxIndex = 69;

    //    // 컴포넌트 우클릭 → "Auto Assign Layer0~69 (grandchildren)"
    //    [ContextMenu("Auto Assign Layer0~69 (grandchildren)")]
    //    private void AutoAssign()
    //    {
    //        // 비활성 포함, 모든 하위 트랜스폼 수집
    //        Transform[] descendants = GetComponentsInChildren<Transform>(true);

    //        var found = new List<GameObject>(maxIndex - minIndex + 1);

    //        foreach (var t in descendants)
    //        {
    //            if (t == transform) continue;

    //            // 깊이 계산: 이 컴포넌트 기준으로 부모를 거슬러 올라가며 2단계인지 확인
    //            int depth = 0;
    //            var cur = t;
    //            while (cur != null && cur != transform)
    //            {
    //                depth++;
    //                cur = cur.parent;
    //            }
    //            if (depth != 2) continue; // 자식의 자식만

    //            // 이름이 LayerN 형태인지 검사
    //            var name = t.name;
    //            if (!name.StartsWith(layerPrefix)) continue;

    //            if (int.TryParse(name.Substring(layerPrefix.Length), out int num))
    //            {
    //                if (num >= minIndex && num <= maxIndex)
    //                    found.Add(t.gameObject);
    //            }
    //        }

    //        // 숫자 순 정렬
    //        found.Sort((a, b) =>
    //        {
    //            int ia = int.Parse(a.name.Substring(layerPrefix.Length));
    //            int ib = int.Parse(b.name.Substring(layerPrefix.Length));
    //            return ia.CompareTo(ib);
    //        });

    //        // 결과 반영
    //        loadingPuzzlePiecesWhite.Clear();
    //        loadingPuzzlePiecesWhite.AddRange(found);

    //        // 프리팹/씬 변경사항 저장 표시
    //#if UNITY_EDITOR
    //        UnityEditor.Undo.RecordObject(this, "Auto Assign Loading Puzzle Pieces");
    //        UnityEditor.EditorUtility.SetDirty(this);
    //#endif

    //        Debug.Log($"[SceneLoader] 자동 할당 완료: {found.Count}개");
    //    }

}