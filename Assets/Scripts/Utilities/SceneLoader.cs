using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 씬 전환 시 로딩 화면을 보여주는 싱글턴 클래스
public class SceneLoader : MonoBehaviour
{
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
    [SerializeField] private Image progressBar;

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
        gameObject.SetActive(true);
        loadSceneName = sceneName;
        SceneManager.sceneLoaded += OnSceneLoaded;
        StartCoroutine(Load(sceneName));
    }

    // 씬 비동기 로드 및 진행률 표시
    private IEnumerator Load(string sceneName)
    {
        progressBar.fillAmount = 0f;
        yield return StartCoroutine(Fade(true));

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        float timer = 0f;

        while (!op.isDone)
        {
            yield return null;
            timer += Time.unscaledDeltaTime;

            if (op.progress < 0.9f)
            {
                progressBar.fillAmount = Mathf.Lerp(progressBar.fillAmount, op.progress, timer);
                if (progressBar.fillAmount >= op.progress) timer = 0f;
            }
            else
            {
                progressBar.fillAmount = Mathf.Lerp(progressBar.fillAmount, 1f, timer);
                if (progressBar.fillAmount >= 1.0f)
                {
                    op.allowSceneActivation = true;
                    yield break;
                }
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
        }
    }

    // 페이드 인/아웃 애니메이션
    private IEnumerator Fade(bool isFadeIn)
    {
        float timer = 0f;
        float startAlpha = isFadeIn ? 0f : 1f;
        float endAlpha = isFadeIn ? 1f : 0f;

        while (timer <= 1f)
        {
            yield return null;
            //timer += Time.unscaledDeltaTime * 2f;
            timer += Time.unscaledDeltaTime * 1.5f;
            sceneLoaderCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, timer);
        }

        if (!isFadeIn)
        {
            gameObject.SetActive(false);
        }
    }
}