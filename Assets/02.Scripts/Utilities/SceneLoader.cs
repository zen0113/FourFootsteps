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
    //[SerializeField] private Image progressBar;

    private string loadSceneName;

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

        gameObject.SetActive(true);
        loadSceneName = sceneName;
        SceneManager.sceneLoaded += OnSceneLoaded;
        StartCoroutine(Load(sceneName));
    }

    // 씬 비동기 로드 및 진행률 표시
    private IEnumerator Load(string sceneName)
    {
        yield return StartCoroutine(Fade(true));

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
            StartCoroutine(Fade(false));
            SceneManager.sceneLoaded -= OnSceneLoaded;

            GameManager.Instance.FinishSceneLoad();

            if (GameManager.Instance != null)
            {
                // 씬이 로드된 후, 자동으로 GameManager 업데이트
                GameManager.Instance.UpdateSceneProgress(scene.name);
            }
        }
    }

    // CanvasGroup도 알파값 조정
    //private IEnumerator Fade(bool isFadeIn)
    //{
    //    // 0~(퍼즐개수-1)까지 리스트 만들고 셔플
    //    List<int> indices = Enumerable.Range(0, loadingPuzzlePieces.Count).ToList();
    //    System.Random rand = new System.Random();
    //    for (int i = indices.Count - 1; i > 0; i--)
    //    {
    //        int j = rand.Next(i + 1);
    //        (indices[i], indices[j]) = (indices[j], indices[i]);
    //    }

    //    // 한 퍼즐당 대략 실행 간격 계산
    //    float interval = fadeInOutTime / loadingPuzzlePieces.Count;
    //    float timer = 0f;

    //    // 알파 시작/끝 값 설정
    //    float startAlpha = isFadeIn ? 0f : 1f;
    //    float endAlpha = isFadeIn ? 1f : 0f;
    //    sceneLoaderCanvasGroup.alpha = startAlpha;

    //    while (timer < fadeInOutTime)
    //    {
    //        yield return null;
    //        timer += Time.unscaledDeltaTime;

    //        // 알파 값 보간 (부드럽게 전환)
    //        sceneLoaderCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, timer / fadeInOutTime);

    //        // 특정 간격마다 랜덤 순서의 퍼즐 조각 켜기/끄기
    //        int pieceIndex = Mathf.FloorToInt(timer / interval);
    //        if (pieceIndex < indices.Count)
    //        {
    //            int idx = indices[pieceIndex];
    //            if (loadingPuzzlePieces[idx] != null)
    //                loadingPuzzlePieces[idx].SetActive(isFadeIn);
    //        }
    //    }

    //    // 알파 최종값으로 설정
    //    sceneLoaderCanvasGroup.alpha = endAlpha;

    //    if (!isFadeIn)
    //    {
    //        gameObject.SetActive(false);
    //    }
    //}

    // 퍼즐 피스만
    private IEnumerator Fade(bool isFadeIn)
    {
        // 0~(퍼즐개수-1)까지 리스트 만들고 셔플
        List<int> indices = Enumerable.Range(0, loadingPuzzlePieces.Count).ToList();
        System.Random rand = new System.Random();
        for (int i = indices.Count - 1; i > 0; i--)
        {
            int j = rand.Next(i + 1);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }

        // 한 퍼즐당 대략 실행 간격 계산
        float interval = fadeInOutTime / loadingPuzzlePieces.Count;
        float timer = 0f;

        foreach (int idx in indices)
        {
            // 한 스텝마다 interval만큼 기다리며 순차적으로 랜덤 순서 실행
            if (loadingPuzzlePieces[idx] != null)
            {
                loadingPuzzlePieces[idx].SetActive(isFadeIn);
            }

            timer += interval;
            yield return new WaitForSecondsRealtime(interval);
        }

        // 마지막 처리
        if (!isFadeIn)
        {
            gameObject.SetActive(false);
        }
    }


    //    // -------------------------------에디터에서 오브젝트 70개 자동할당-----------------------------------
    //    // 실행 시 쓰이진 않음! 에디터에서 편의용으로 둔것
    //    [Header("검색 규칙")]
    //    [SerializeField] private string layerPrefix = "레이어";
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
    //        loadingPuzzlePieces.Clear();
    //        loadingPuzzlePieces.AddRange(found);

    //        // 프리팹/씬 변경사항 저장 표시
    //#if UNITY_EDITOR
    //        UnityEditor.Undo.RecordObject(this, "Auto Assign Loading Puzzle Pieces");
    //        UnityEditor.EditorUtility.SetDirty(this);
    //#endif

    //        Debug.Log($"[SceneLoader] 자동 할당 완료: {found.Count}개");
    //    }

}