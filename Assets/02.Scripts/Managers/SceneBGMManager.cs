using UnityEngine;
using UnityEngine.SceneManagement;
using static Constants;

public class SceneBGMManager : MonoBehaviour
{
    [Header("씬 BGM 설정")]
    [SerializeField] private SceneBGMSetting[] sceneBGMSettings;

    [System.Serializable]
    public class SceneBGMSetting
    {
        public string sceneName;
        [Header("BGM 설정 (최대 2개)")]
        public int bgmIndex1 = -1;
        public int bgmIndex2 = -1;
        [Range(0f, 1f)] public float bgm1Volume = 1f;
        [Range(0f, 1f)] public float bgm2Volume = 1f;
        public bool autoPlay = true;

        [Header("동기화 설정")]
        public bool syncStart = true; // 동시에 시작할지 여부
        public float bgm2Delay = 0f; // BGM2의 시작 지연 시간
    }

    private void Start()
    {
        // 현재 씬의 BGM 설정 적용
        string currentScene = SceneManager.GetActiveScene().name;
        ApplySceneBGM(currentScene);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplySceneBGM(scene.name);
    }

    // 씬에 맞는 BGM 설정 적용
    public void ApplySceneBGM(string sceneName)
    {
        SceneBGMSetting setting = GetSceneBGMSetting(sceneName);
        if (setting != null && setting.autoPlay)
        {
            // 현재 재생 중인 BGM과 다른 경우에만 변경
            bool needChange = false;
            var currentBGMs = SoundPlayer.Instance.GetCurrentBGMs();
            
            if (setting.bgmIndex1 != -1 && !currentBGMs.Contains(setting.bgmIndex1))
                needChange = true;
            if (setting.bgmIndex2 != -1 && !currentBGMs.Contains(setting.bgmIndex2))
                needChange = true;

            if (needChange || currentBGMs.Count == 0)
            {
                SoundPlayer.Instance.ChangeDualBGM(
                    setting.bgmIndex1,
                    setting.bgmIndex2,
                    setting.bgm1Volume,
                    setting.bgm2Volume,
                    setting.syncStart,
                    setting.bgm2Delay
                );
            }
        }
    }

    // 씬 BGM 설정 가져오기
    private SceneBGMSetting GetSceneBGMSetting(string sceneName)
    {
        foreach (var setting in sceneBGMSettings)
        {
            if (setting.sceneName == sceneName)
            {
                return setting;
            }
        }
        return null;
    }

    // 특정 씬의 BGM 설정 수동 적용
    public void ApplySpecificSceneBGM(string sceneName)
    {
        SceneBGMSetting setting = GetSceneBGMSetting(sceneName);
        if (setting != null)
        {
            SoundPlayer.Instance.ChangeDualBGM(
                setting.bgmIndex1,
                setting.bgmIndex2,
                setting.bgm1Volume,
                setting.bgm2Volume,
                setting.syncStart,
                setting.bgm2Delay
            );
        }
    }
}