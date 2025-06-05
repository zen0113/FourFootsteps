using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Constants;

public class SoundPlayer : MonoBehaviour
{
    public static SoundPlayer Instance { get; private set; }

    [Header("Audio Source")]
    [SerializeField] private AudioSource[] bgmPlayers = new AudioSource[4]; // 4개로 증가 (동시 재생용)
    [SerializeField] private AudioSource typingSoundPlayer;
    [SerializeField] private AudioSource[] UISoundLoopPlayer;
    [SerializeField] private AudioSource[] UISoundPlayer;

    [Header("AudioClip")]
    [SerializeField] private AudioClip[] bgmClip;
    [SerializeField] private AudioClip[] UISoundClip;
    [SerializeField] private AudioClip[] UISoundClip_LOOP;

    [Header("Scene BGM Settings")]
    [SerializeField] private SceneBGMSetting[] sceneBGMSettings;

    private float bgmVolume = 0.5f;
    private float soundEffectVolume = 0.5f;
    private List<Coroutine> bgmCoroutines = new List<Coroutine>(); // 여러 코루틴 관리
    private int UISoundPlayerCursor;
    private List<int> currentBGMs = new List<int>(); // 현재 재생 중인 BGM들

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

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        CheckAndPlaySceneBGM(SceneManager.GetActiveScene().name);
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
            UISoundPlay(Sound_Click);
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CheckAndPlaySceneBGM(scene.name);
    }

    private void CheckAndPlaySceneBGM(string sceneName)
    {
        foreach (var setting in sceneBGMSettings)
        {
            if (setting.sceneName == sceneName && setting.autoPlay)
            {
                // 현재 재생 중인 BGM과 다른 경우에만 변경
                bool needChange = false;
                if (setting.bgmIndex1 != -1 && !currentBGMs.Contains(setting.bgmIndex1))
                    needChange = true;
                if (setting.bgmIndex2 != -1 && !currentBGMs.Contains(setting.bgmIndex2))
                    needChange = true;

                if (needChange || currentBGMs.Count == 0)
                {
                    ChangeDualBGM(setting.bgmIndex1, setting.bgmIndex2,
                                setting.bgm1Volume, setting.bgm2Volume,
                                setting.syncStart, setting.bgm2Delay);
                }
                return;
            }
        }
    }

    public void ChangeVolume(float bgmVolume, float soundEffectVolume)
    {
        if (bgmVolume != -1)
        {
            this.bgmVolume = bgmVolume;
            foreach (AudioSource bgm in bgmPlayers)
            {
                if (bgm.isPlaying)
                    bgm.volume = bgmVolume;
            }
        }

        if (soundEffectVolume != -1)
        {
            this.soundEffectVolume = soundEffectVolume;
            typingSoundPlayer.volume = soundEffectVolume;
            foreach (AudioSource audio in UISoundLoopPlayer)
                audio.volume = soundEffectVolume;
            foreach (AudioSource audio in UISoundPlayer)
                audio.volume = soundEffectVolume;
        }
    }

    // 단일 BGM 변경 (기존 호환성)
    public void ChangeBGM(int bgm)
    {
        ChangeDualBGM(bgm, -1, 1f, 1f, true, 0f);
    }

    // 단일 BGM 즉시 변경 (페이드 없음)
    public void ChangeBGMImmediate(int bgm)
    {
        if (bgm != -1 && bgm != BGM_STOP && bgm < bgmClip.Length)
        {
            StopAllBGMCoroutines();
            StopAllBGM();

            currentBGMs.Clear();
            currentBGMs.Add(bgm);

            AudioSource player = bgmPlayers[0];
            player.clip = bgmClip[bgm];
            player.volume = bgmVolume;
            player.Play();
        }
        else if (bgm == -1 || bgm == BGM_STOP)
        {
            StopAllBGM();
        }
    }

    // 2개 BGM 동시 재생
    public void ChangeDualBGM(int bgm1, int bgm2 = -1, float volume1 = 1f, float volume2 = 1f, bool syncStart = true, float bgm2Delay = 0f)
    {
        // 기존 BGM 코루틴들 정지
        StopAllBGMCoroutines();

        // 현재 BGM 리스트 업데이트
        currentBGMs.Clear();
        if (bgm1 != -1) currentBGMs.Add(bgm1);
        if (bgm2 != -1) currentBGMs.Add(bgm2);

        // BGM 재생 시작
        if (bgm1 != -1 && bgm1 != BGM_STOP)
        {
            Coroutine coroutine = StartCoroutine(ChangeBGMFade(bgm1, 0, volume1, 0f));
            bgmCoroutines.Add(coroutine);
        }

        if (bgm2 != -1 && bgm2 != BGM_STOP)
        {
            float delay = syncStart ? 0f : bgm2Delay;
            Coroutine coroutine = StartCoroutine(ChangeBGMFade(bgm2, 1, volume2, delay));
            bgmCoroutines.Add(coroutine);
        }

        // 모든 BGM이 정지 상태인 경우
        if ((bgm1 == -1 || bgm1 == BGM_STOP) && (bgm2 == -1 || bgm2 == BGM_STOP))
        {
            StopAllBGM();
        }
    }

    // 즉시 BGM 변경 (페이드 없음)
    public void ChangeDualBGMImmediate(int bgm1, int bgm2 = -1, float volume1 = 1f, float volume2 = 1f)
    {
        StopAllBGMCoroutines();
        StopAllBGM();

        currentBGMs.Clear();

        if (bgm1 != -1 && bgm1 != BGM_STOP && bgm1 < bgmClip.Length)
        {
            AudioSource player = bgmPlayers[0];
            player.clip = bgmClip[bgm1];
            player.volume = bgmVolume * volume1;
            player.Play();
            currentBGMs.Add(bgm1);
        }

        if (bgm2 != -1 && bgm2 != BGM_STOP && bgm2 < bgmClip.Length)
        {
            AudioSource player = bgmPlayers[1];
            player.clip = bgmClip[bgm2];
            player.volume = bgmVolume * volume2;
            player.Play();
            currentBGMs.Add(bgm2);
        }
    }

    private IEnumerator ChangeBGMFade(int bgm, int playerIndex, float volumeMultiplier, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        float fadeDuration = 2f;
        AudioSource newPlayer = bgmPlayers[playerIndex * 2]; // 0,2 또는 1,3 인덱스 사용
        AudioSource oldPlayer = bgmPlayers[playerIndex * 2 + 1];

        // 기존 BGM 페이드 아웃
        if (oldPlayer.isPlaying)
        {
            float startVolume = oldPlayer.volume;
            float currentTime = 0f;

            while (currentTime < fadeDuration)
            {
                currentTime += Time.deltaTime;
                oldPlayer.volume = Mathf.Lerp(startVolume, 0, currentTime / fadeDuration);
                yield return null;
            }
            oldPlayer.Stop();
        }

        // 새 BGM 재생 및 페이드 인
        if (bgm < bgmClip.Length)
        {
            newPlayer.clip = bgmClip[bgm];
            newPlayer.volume = 0;
            newPlayer.Play();

            float targetVolume = bgmVolume * volumeMultiplier;
            float currentTime = 0f;
            while (currentTime < fadeDuration)
            {
                currentTime += Time.deltaTime;
                newPlayer.volume = Mathf.Lerp(0, targetVolume, currentTime / fadeDuration);
                yield return null;
            }
            newPlayer.volume = targetVolume;
        }
    }

    // BGM 개별 볼륨 조절
    public void SetBGMVolume(int bgmIndex, float volume)
    {
        for (int i = 0; i < bgmPlayers.Length; i++)
        {
            if (bgmPlayers[i].isPlaying &&
                System.Array.IndexOf(bgmClip, bgmPlayers[i].clip) == bgmIndex)
            {
                bgmPlayers[i].volume = bgmVolume * volume;
            }
        }
    }

    // 특정 BGM만 페이드 아웃
    public void FadeOutSpecificBGM(int bgmIndex, float duration = 2f)
    {
        for (int i = 0; i < bgmPlayers.Length; i++)
        {
            if (bgmPlayers[i].isPlaying &&
                System.Array.IndexOf(bgmClip, bgmPlayers[i].clip) == bgmIndex)
            {
                StartCoroutine(FadeOutSpecificCoroutine(bgmPlayers[i], duration));
            }
        }
    }

    private IEnumerator FadeOutSpecificCoroutine(AudioSource player, float duration)
    {
        float startVolume = player.volume;
        float currentTime = 0f;

        while (currentTime < duration && player.isPlaying)
        {
            currentTime += Time.deltaTime;
            player.volume = Mathf.Lerp(startVolume, 0, currentTime / duration);
            yield return null;
        }

        player.Stop();
        // 현재 BGM 리스트에서 제거
        int bgmIndex = System.Array.IndexOf(bgmClip, player.clip);
        if (bgmIndex != -1)
            currentBGMs.Remove(bgmIndex);
    }

    public void FadeOutAllBGM(float duration = 2f)
    {
        StopAllBGMCoroutines();

        foreach (AudioSource player in bgmPlayers)
        {
            if (player.isPlaying)
            {
                StartCoroutine(FadeOutSpecificCoroutine(player, duration));
            }
        }

        currentBGMs.Clear();
    }

    private void StopAllBGMCoroutines()
    {
        foreach (Coroutine coroutine in bgmCoroutines)
        {
            if (coroutine != null)
                StopCoroutine(coroutine);
        }
        bgmCoroutines.Clear();
    }

    private void StopAllBGM()
    {
        foreach (AudioSource player in bgmPlayers)
        {
            player.Stop();
        }
        currentBGMs.Clear();
    }

    public void PauseBGM()
    {
        foreach (AudioSource player in bgmPlayers)
        {
            if (player.isPlaying)
                player.Pause();
        }
    }

    public void ResumeBGM()
    {
        foreach (AudioSource player in bgmPlayers)
        {
            if (player.clip != null && !player.isPlaying)
                player.UnPause();
        }
    }

    public void ChangeSceneBGM(string sceneName)
    {
        CheckAndPlaySceneBGM(sceneName);
    }

    // 씬 BGM 설정 가져오기
    public SceneBGMSetting GetSceneBGMSetting(string sceneName)
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

    public void UISoundPlay_LOOP(int num, bool play)
    {
        if (play)
        {
            foreach (AudioSource audio in UISoundLoopPlayer)
                if (audio.clip == UISoundClip_LOOP[num] && audio.isPlaying) return;

            int index = 0;
            if (UISoundLoopPlayer[index].isPlaying) index++;

            UISoundLoopPlayer[index].clip = UISoundClip_LOOP[num];
            UISoundLoopPlayer[index].Play();
        }
        else
        {
            foreach (AudioSource audio in UISoundLoopPlayer)
            {
                if (audio.isPlaying && audio.clip == UISoundClip_LOOP[num])
                {
                    audio.Stop();
                    break;
                }
            }
        }
    }

    public void UISoundPlay(int num)
    {
        if (num == Sound_Typing)
        {
            typingSoundPlayer.Play();
            return;
        }

        UISoundPlayer[UISoundPlayerCursor].clip = UISoundClip[num];
        UISoundPlayer[UISoundPlayerCursor].panStereo = SoundPosition();
        UISoundPlayer[UISoundPlayerCursor].Play();

        UISoundPlayerCursor = (UISoundPlayerCursor + 1) % UISoundPlayer.Length;
    }

    public void UISoundStop(int num)
    {
        if (num == Sound_Typing)
        {
            typingSoundPlayer.Stop();
            return;
        }

        foreach (AudioSource audio in UISoundPlayer)
        {
            if (num == -1) { audio.Stop(); continue; }
            if (audio.clip == UISoundClip[num]) audio.Stop();
        }
    }

    private float SoundPosition()
    {
        Vector3 clickPosition = Input.mousePosition;
        float normalizedX = (clickPosition.x / Screen.width - 0.5f) * 2f;
        return normalizedX;
    }

    // 현재 재생 중인 BGM 정보
    public List<int> GetCurrentBGMs() => new List<int>(currentBGMs);
    public bool IsBGMPlaying() => currentBGMs.Count > 0;
    public int GetBGMCount() => currentBGMs.Count;
}