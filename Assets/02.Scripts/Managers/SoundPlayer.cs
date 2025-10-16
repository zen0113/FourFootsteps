using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SoundPlayer : MonoBehaviour
{
    public static SoundPlayer Instance { get; private set; }

    [Header("Audio Source")]
    [SerializeField] private AudioSource[] bgmAudioSources = new AudioSource[2]; // BGM은 2개만 활성화 상태로 유지
    [SerializeField] private AudioSource typingSoundPlayer;
    [SerializeField] private AudioSource[] uiSoundLoopPlayers = new AudioSource[2];

    // UISoundPlayer 배열로 변경
    [SerializeField] private UISoundPlayer[] uiPlayers;
    private List<AudioSource> sceneSFXAudioSources = new List<AudioSource>();

    [Header("AudioClip")]
    [SerializeField] private AudioClip[] bgmClips;
    [SerializeField] private AudioClip[] uiSoundClips;
    [SerializeField] private AudioClip[] uiSoundLoopClips;

    [Header("Scene BGM Settings")]
    [SerializeField] private SceneBGMSetting[] sceneBGMSettings;

    private float masterBGMVolume = 0.5f;
    private float masterSFXVolume = 0.5f;
    private readonly List<Coroutine> bgmFadeCoroutines = new(); // BGM 페이드 코루틴 관리
    public readonly Dictionary<int, (AudioSource source, float volumeMultiplier)> activeBGMPlayers = new(); // 현재 재생 중인 BGM 클립 인덱스와 AudioSource, 볼륨 배율 매핑

    private const int BGM_STOP = -1; // BGM을 정지시키는 상수
    private const int SOUND_TYPING = 0; // 타이핑 사운드 인덱스
    private const int SOUND_CLICK = 1; // 클릭 사운드 인덱스

    [System.Serializable]
    public class SceneBGMSetting
    {
        public string sceneName;
        [Header("BGM 설정 (최대 2개)")]
        public int bgmIndex1 = -1;
        public int bgmIndex2 = -1;
        [Range(0f, 1f)] public float bgm1VolumeMultiplier = 1f;
        [Range(0f, 1f)] public float bgm2VolumeMultiplier = 1f;
        public bool autoPlay = true;

        [Header("동기화 설정")]
        public bool syncStart = true; // 동시에 시작할지 여부
        public float bgm2Delay = 0f; // BGM2의 시작 지연 시간
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
            AudioEventSystem.OnBGMVolumeFade += FadeBGMVolume;
            InitializeAudioSources();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 시작 시 현재 씬의 BGM 자동 재생
        CheckAndPlaySceneBGM(SceneManager.GetActiveScene().name);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            AudioEventSystem.OnBGMVolumeFade -= FadeBGMVolume;
        }
    }

    private void InitializeAudioSources()
    {
        // BGM 플레이어 초기화 (최대 2개)
        if (bgmAudioSources == null || bgmAudioSources.Length < 2)
        {
            bgmAudioSources = new AudioSource[2];
        }
        for (int i = 0; i < 2; i++) // BGM은 두 개만 활성화
        {
            if (bgmAudioSources[i] == null)
            {
                bgmAudioSources[i] = gameObject.AddComponent<AudioSource>();
            }
            bgmAudioSources[i].playOnAwake = false;
            bgmAudioSources[i].loop = true;
            bgmAudioSources[i].volume = masterBGMVolume; // 초기 볼륨 설정
        }

        // UI 루프 사운드 플레이어 초기화
        if (uiSoundLoopPlayers == null || uiSoundLoopPlayers.Length < 2)
        {
            uiSoundLoopPlayers = new AudioSource[2];
        }
        for (int i = 0; i < 2; i++)
        {
            if (uiSoundLoopPlayers[i] == null)
            {
                uiSoundLoopPlayers[i] = gameObject.AddComponent<AudioSource>();
            }
            uiSoundLoopPlayers[i].playOnAwake = false;
            uiSoundLoopPlayers[i].loop = true;
            uiSoundLoopPlayers[i].volume = masterSFXVolume;
        }

        // 타이핑 사운드 플레이어 초기화
        if (typingSoundPlayer == null)
        {
            typingSoundPlayer = gameObject.AddComponent<AudioSource>();
        }
        typingSoundPlayer.playOnAwake = false;
        typingSoundPlayer.volume = masterSFXVolume;
        typingSoundPlayer.loop = true; // 타이핑 사운드는 보통 루프

        // UISoundPlayer 초기화: 필요한 만큼 생성하고 AudioSource 할당
        if (uiPlayers == null || uiPlayers.Length == 0)
        {
            uiPlayers = new UISoundPlayer[5]; // 예시로 5개 할당, 필요에 따라 조절
            for (int i = 0; i < uiPlayers.Length; i++)
            {
                if (uiPlayers[i] == null)
                {
                    GameObject uiPlayerGo = new GameObject($"UISoundPlayer_{i}");
                    uiPlayerGo.transform.SetParent(this.transform); // SoundPlayer의 자식으로 설정
                    uiPlayers[i] = uiPlayerGo.AddComponent<UISoundPlayer>();
                    // 수정된 라인: uiPlayerGo에 AudioSource를 추가하도록 변경
                    uiPlayers[i].Initialize(uiPlayerGo.AddComponent<AudioSource>(), masterSFXVolume);
                }
            }
        }
        else
        {
            foreach (var player in uiPlayers)
            {
                if (player != null && player.AudioSource == null)
                {
                    // 수정된 라인: UISoundPlayer의 GameObject에 AudioSource를 추가하도록 변경
                    player.Initialize(player.gameObject.AddComponent<AudioSource>(), masterSFXVolume);
                }
                else if (player != null)
                {
                    player.UpdateVolume(masterSFXVolume); // 기존 플레이어도 볼륨 업데이트
                }
            }
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            UISoundPlay(SOUND_CLICK);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 씬 로드 시 기존 외부 SFX AudioSource 목록 초기화 및 새로운 씬에서 검색
        sceneSFXAudioSources.Clear();
        FindAndAddSceneAudioSourcesAdvanced();

        CheckAndPlaySceneBGM(scene.name);
    }

    private void FindAndAddSceneAudioSourcesAdvanced()
    {
        sceneSFXAudioSources.Clear();

        // 모든 루트 GameObject 가져오기
        GameObject[] rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();

        List<AudioSource> soundPlayerManagedSources = new List<AudioSource>();
        soundPlayerManagedSources.AddRange(bgmAudioSources);
        soundPlayerManagedSources.Add(typingSoundPlayer);
        soundPlayerManagedSources.AddRange(uiSoundLoopPlayers);
        foreach (var uiPlayer in uiPlayers)
        {
            if (uiPlayer != null && uiPlayer.AudioSource != null)
            {
                soundPlayerManagedSources.Add(uiPlayer.AudioSource);
            }
        }

        foreach (GameObject rootObj in rootObjects)
        {
            // SoundPlayer 자신은 제외
            if (rootObj.transform.root == this.transform.root) continue;

            // 각 루트 오브젝트의 모든 자식에서 AudioSource 검색 (비활성화된 것도 포함)
            AudioSource[] audioSources = rootObj.GetComponentsInChildren<AudioSource>(true);

            foreach (AudioSource source in audioSources)
            {
                if (!soundPlayerManagedSources.Contains(source))
                {
                    sceneSFXAudioSources.Add(source);
                    source.volume = masterSFXVolume;
                    Debug.Log($"Found AudioSource: {source.name} on {source.gameObject.name} (Active: {source.gameObject.activeInHierarchy})");
                }
            }
        }
    }

    private void CheckAndPlaySceneBGM(string sceneName)
    {
        SceneBGMSetting sceneSetting = null;
        foreach (var setting in sceneBGMSettings)
        {
            if (setting.sceneName == sceneName)
            {
                sceneSetting = setting;
                break;
            }
        }

        if (sceneSetting != null && sceneSetting.autoPlay)
        {
            // 현재 씬에 설정된 BGM이 있고 자동 재생인 경우
            ChangeDualBGM(sceneSetting.bgmIndex1, sceneSetting.bgmIndex2,
                            sceneSetting.bgm1VolumeMultiplier, sceneSetting.bgm2VolumeMultiplier,
                            sceneSetting.syncStart, sceneSetting.bgm2Delay);
        }
        else if (sceneSetting == null) // 씬 설정이 없는 경우
        {
            // 씬에 대한 BGM 설정이 없는 경우, 현재 재생 중인 BGM을 유지합니다.
            Debug.Log($"No specific BGM setting found for scene: {sceneName}. Keeping current BGM.");
        }
        else // 씬 설정은 있지만 autoPlay가 false인 경우
        {
            // 씬 설정은 있지만 자동 재생을 하지 않도록 설정된 경우, 모든 BGM을 정지합니다.
            StopAllBGM();
        }
    }

    // autoPlay false인 거 tutorial Manager에서 알맞은 타이밍에 재생함 
    public void PlaySceneBGMNotAuto()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        SceneBGMSetting sceneSetting = null;
        foreach (var setting in sceneBGMSettings)
        {
            if (setting.sceneName == sceneName)
            {
                sceneSetting = setting;
                break;
            }
        }

        if (sceneSetting != null && !sceneSetting.autoPlay)
        {
            ChangeDualBGM(sceneSetting.bgmIndex1, sceneSetting.bgmIndex2,
                            sceneSetting.bgm1VolumeMultiplier, sceneSetting.bgm2VolumeMultiplier,
                            sceneSetting.syncStart, sceneSetting.bgm2Delay);
        }
    }


    /// <summary>
    /// 마스터 볼륨을 변경하고 모든 오디오 소스에 적용합니다.
    /// </summary>
    /// <param name="bgmVolume">BGM 마스터 볼륨 (변경하지 않으려면 -1)</param>
    /// <param name="sfxVolume">SFX 마스터 볼륨 (변경하지 않으려면 -1)</param>
    public void ChangeVolume(float bgmVolume = -1f, float sfxVolume = -1f)
    {
        if (bgmVolume >= 0)
        {
            masterBGMVolume = bgmVolume;
            // 현재 재생 중인 BGM들의 볼륨을 즉시 변경
            foreach (var entry in activeBGMPlayers.Values)
            {
                entry.source.volume = masterBGMVolume * entry.volumeMultiplier;
            }
        }

        if (sfxVolume >= 0)
        {
            masterSFXVolume = sfxVolume;
            if (typingSoundPlayer != null) typingSoundPlayer.volume = masterSFXVolume;
            foreach (AudioSource audio in uiSoundLoopPlayers)
            {
                if (audio != null) audio.volume = masterSFXVolume;
            }
            // UISoundPlayer도 볼륨 업데이트
            foreach (UISoundPlayer uiPlayer in uiPlayers)
            {
                if (uiPlayer != null) uiPlayer.UpdateVolume(masterSFXVolume);
            }
            // 추가: 씬 내 외부 SFX AudioSource들의 볼륨 업데이트
            foreach (AudioSource sfxSource in sceneSFXAudioSources)
            {
                if (sfxSource != null)
                {
                    sfxSource.volume = masterSFXVolume;
                }
            }
        }
    }

    /// <summary>
    /// 단일 BGM을 페이드인/아웃하여 변경합니다.
    /// </summary>
    /// <param name="bgmIndex">재생할 BGM 클립의 인덱스</param>
    public void ChangeBGM(int bgmIndex)
    {
        ChangeDualBGM(bgmIndex, BGM_STOP, 1f, 1f, true, 0f);
    }

    /// <summary>
    /// 두 개의 BGM을 페이드인/아웃하여 동시에 재생하거나 변경합니다.
    /// 만약 씬에서 설정된 BGM이 현재 재생 중인 BGM과 동일하다면 페이드 효과 없이 볼륨만 조절합니다.
    /// </summary>
    /// <param name="bgm1Index">첫 번째 BGM 클립 인덱스 (-1 또는 BGM_STOP은 재생 안 함)</param>
    /// <param name="bgm2Index">두 번째 BGM 클립 인덱스 (-1 또는 BGM_STOP은 재생 안 함)</param>
    /// <param name="volume1Multiplier">첫 번째 BGM의 상대적 볼륨 배율</param>
    /// <param name="volume2Multiplier">두 번째 BGM의 상대적 볼륨 배율</param>
    /// <param name="syncStart">두 BGM을 동시에 시작할지 여부</param>
    /// <param name="bgm2Delay">BGM2의 시작 지연 시간 (syncStart가 false일 때 유효)</param>
    public void ChangeDualBGM(int bgm1Index, int bgm2Index = BGM_STOP, float volume1Multiplier = 1f, float volume2Multiplier = 1f, bool syncStart = true, float bgm2Delay = 0f)
    {
        StopAllBGMCoroutines(); // 기존 페이드 코루틴 정지

        // 새롭게 재생될 BGM 인덱스를 저장할 리스트
        List<int> newBGMIndexes = new List<int>();
        if (bgm1Index != BGM_STOP) newBGMIndexes.Add(bgm1Index);
        if (bgm2Index != BGM_STOP) newBGMIndexes.Add(bgm2Index);

        // 현재 activeBGMPlayers에 있지만, 새롭게 재생될 BGM 목록에 없는 BGM들을 찾아서 페이드 아웃
        List<int> bgmsToFadeOut = new List<int>();
        foreach (var entry in activeBGMPlayers)
        {
            if (!newBGMIndexes.Contains(entry.Key))
            {
                bgmsToFadeOut.Add(entry.Key);
            }
        }

        foreach (int indexToFadeOut in bgmsToFadeOut)
        {
            if (activeBGMPlayers.TryGetValue(indexToFadeOut, out var entryToFadeOut))
            {
                // 페이드 아웃할 BGM 플레이어는 해당 코루틴에서 activeBGMPlayers에서 제거하도록 함
                bgmFadeCoroutines.Add(StartCoroutine(FadeOutCoroutine(entryToFadeOut.source, 2f, indexToFadeOut)));
            }
        }

        // BGM1 처리
        AudioSource player1 = bgmAudioSources[0]; // BGM1은 항상 첫 번째 플레이어 사용
        if (bgm1Index != BGM_STOP && bgm1Index < bgmClips.Length && bgm1Index >= 0)
        {
            // player1이 현재 bgm1Index를 재생 중이고, 클립도 동일한지 확인
            bool isPlayer1PlayingBGM1 = activeBGMPlayers.ContainsKey(bgm1Index) && activeBGMPlayers[bgm1Index].source == player1 && player1.clip == bgmClips[bgm1Index];

            if (isPlayer1PlayingBGM1)
            {
                // 이미 재생 중인 BGM의 볼륨만 조정하고 activeBGMPlayers 정보 업데이트
                SetBGMVolume(bgm1Index, volume1Multiplier);
                // activeBGMPlayers 딕셔너리에 올바른 AudioSource와 volumeMultiplier가 매핑되도록 확실히 업데이트
                if (activeBGMPlayers.ContainsKey(bgm1Index))
                {
                    activeBGMPlayers[bgm1Index] = (player1, volume1Multiplier);
                }
                else
                {
                    activeBGMPlayers.Add(bgm1Index, (player1, volume1Multiplier));
                }
            }
            else
            {
                // 새로운 BGM1을 재생하거나, player1이 다른 BGM을 재생 중이거나, 재생 중이 아니면 페이드인 시작
                // 기존에 player1이 다른 BGM을 재생 중이었다면, 해당 BGM에 대한 activeBGMPlayers 엔트리 제거
                RemoveBGMPlayerFromActive(player1);
                bgmFadeCoroutines.Add(StartCoroutine(FadeInCoroutine(player1, bgmClips[bgm1Index], masterBGMVolume * volume1Multiplier, 2f, 0f, bgm1Index, volume1Multiplier)));
            }
        }
        else // bgm1Index가 BGM_STOP이거나 유효하지 않으면 첫 번째 BGM 플레이어를 정지
        {
            // player1이 현재 activeBGMPlayers에 포함되어 있다면 페이드 아웃
            int? currentBGM1Index = null;
            foreach (var entry in activeBGMPlayers)
            {
                if (entry.Value.source == player1)
                {
                    currentBGM1Index = entry.Key;
                    break;
                }
            }

            if (currentBGM1Index.HasValue)
            {
                bgmFadeCoroutines.Add(StartCoroutine(FadeOutCoroutine(player1, 2f, currentBGM1Index.Value)));
            }
            else if (player1.isPlaying) // activeBGMPlayers에 없지만 재생 중이라면 바로 정지
            {
                player1.Stop();
                player1.clip = null;
            }
        }

        // BGM2 처리
        AudioSource player2 = bgmAudioSources[1]; // BGM2는 항상 두 번째 플레이어 사용
        if (bgm2Index != BGM_STOP && bgm2Index < bgmClips.Length && bgm2Index >= 0)
        {
            // player2가 현재 bgm2Index를 재생 중이고, 클립도 동일한지 확인
            bool isPlayer2PlayingBGM2 = activeBGMPlayers.ContainsKey(bgm2Index) && activeBGMPlayers[bgm2Index].source == player2 && player2.clip == bgmClips[bgm2Index];

            if (isPlayer2PlayingBGM2)
            {
                // 이미 재생 중인 BGM의 볼륨만 조정하고 activeBGMPlayers 정보 업데이트
                SetBGMVolume(bgm2Index, volume2Multiplier);
                // activeBGMPlayers 딕셔너리에 올바른 AudioSource와 volumeMultiplier가 매핑되도록 확실히 업데이트
                if (activeBGMPlayers.ContainsKey(bgm2Index))
                {
                    activeBGMPlayers[bgm2Index] = (player2, volume2Multiplier);
                }
                else
                {
                    activeBGMPlayers.Add(bgm2Index, (player2, volume2Multiplier));
                }
            }
            else
            {
                // 새로운 BGM2를 재생하거나, player2가 다른 BGM을 재생 중이거나, 재생 중이 아니면 페이드인 시작
                // 기존에 player2가 다른 BGM을 재생 중이었다면, 해당 BGM에 대한 activeBGMPlayers 엔트리 제거
                RemoveBGMPlayerFromActive(player2);
                float delay = syncStart ? 0f : bgm2Delay;
                bgmFadeCoroutines.Add(StartCoroutine(FadeInCoroutine(player2, bgmClips[bgm2Index], masterBGMVolume * volume2Multiplier, 2f, delay, bgm2Index, volume2Multiplier)));
            }
        }
        else // bgm2Index가 BGM_STOP이거나 유효하지 않으면 두 번째 BGM 플레이어를 정지
        {
            // player2가 현재 activeBGMPlayers에 포함되어 있다면 페이드 아웃
            int? currentBGM2Index = null;
            foreach (var entry in activeBGMPlayers)
            {
                if (entry.Value.source == player2)
                {
                    currentBGM2Index = entry.Key;
                    break;
                }
            }

            if (currentBGM2Index.HasValue)
            {
                bgmFadeCoroutines.Add(StartCoroutine(FadeOutCoroutine(player2, 2f, currentBGM2Index.Value)));
            }
            else if (player2.isPlaying) // activeBGMPlayers에 없지만 재생 중이라면 바로 정지
            {
                player2.Stop();
                player2.clip = null;
            }
        }
    }

    /// <summary>
    /// 특정 BGM의 볼륨을 특정 목표 볼륨으로 페이드합니다.
    /// 이 메서드는 AudioEventSystem.OnBGMVolumeFade 이벤트에 의해 호출됩니다.
    /// </summary>
    /// <param name="bgmIndex">볼륨을 조절할 BGM 클립의 인덱스</param>
    /// <param name="targetVolumeMultiplier">목표 볼륨 배율 (0f ~ 1f)</param>
    /// <param name="duration">페이드에 걸리는 시간</param>
    /// <param name="delay">페이드 시작 전 지연 시간</param>
    public void FadeBGMVolume(int bgmIndex, float targetVolumeMultiplier, float duration, float delay = 0f)
    {
        if (bgmIndex < 0 || bgmIndex >= bgmClips.Length)
        {
            Debug.LogWarning($"Invalid BGM index: {bgmIndex}");
            return;
        }

        // 딕셔너리에서 엔트리 찾기
        if (!activeBGMPlayers.TryGetValue(bgmIndex, out var entry))
        {
            // 폴백: 같은 클립을 재생 중인 소스를 탐색
            var targetClip = bgmClips[bgmIndex];
            bool found = false;
            foreach (var kv in activeBGMPlayers)
            {
                var s = kv.Value.source;
                if (s != null && s.isPlaying && s.clip == targetClip)
                {
                    entry = kv.Value; // ValueTuple 복사
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                Debug.LogWarning($"BGM with index {bgmIndex} is not currently active. Cannot fade its volume.");
                return;
            }
        }

        // 여기서 null 비교는 튜플 자체가 아니라, 내부의 참조형(AudioSource)로
        if (entry.source == null)
        {
            Debug.LogWarning($"BGM entry for index {bgmIndex} has no AudioSource.");
            return;
        }

        float actualTargetVolume = masterBGMVolume * Mathf.Clamp01(targetVolumeMultiplier);
        bgmFadeCoroutines.Add(StartCoroutine(
            FadeVolumeCoroutine(entry.source, actualTargetVolume, duration, delay, bgmIndex, targetVolumeMultiplier)));
    }
    //public void FadeBGMVolume(int bgmIndex, float targetVolumeMultiplier, float duration, float delay = 0f)
    //{
    //    if (bgmIndex < 0 || bgmIndex >= bgmClips.Length)
    //    {
    //        Debug.LogWarning($"Invalid BGM index: {bgmIndex}");
    //        return;
    //    }

    //    if (activeBGMPlayers.TryGetValue(bgmIndex, out var entry))
    //    {
    //        float actualTargetVolume = masterBGMVolume * targetVolumeMultiplier;
    //        bgmFadeCoroutines.Add(StartCoroutine(FadeVolumeCoroutine(entry.source, actualTargetVolume, duration, delay, bgmIndex, targetVolumeMultiplier)));
    //    }
    //    else
    //    {
    //        Debug.LogWarning($"BGM with index {bgmIndex} is not currently active. Cannot fade its volume.");
    //    }
    //}


    private IEnumerator FadeVolumeCoroutine(AudioSource player, float targetVolume, float duration, float delay, int bgmIndex, float newVolumeMultiplier)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        float startVolume = player.volume;
        float timeElapsed = 0f;

        while (timeElapsed < duration)
        {
            timeElapsed += Time.deltaTime;
            player.volume = Mathf.Lerp(startVolume, targetVolume, Mathf.SmoothStep(0f, 1f, timeElapsed / duration));
            yield return null;
        }
        player.volume = targetVolume; // 최종 볼륨 보장

        // 페이드 완료 후 activeBGMPlayers의 volumeMultiplier 업데이트
        if (activeBGMPlayers.ContainsKey(bgmIndex))
        {
            activeBGMPlayers[bgmIndex] = (player, newVolumeMultiplier);
        }
    }


    /// <summary>
    /// activeBGMPlayers 딕셔너리에서 특정 AudioSource를 제거합니다.
    /// </summary>
    /// <param name="sourceToRemove">제거할 AudioSource</param>
    private void RemoveBGMPlayerFromActive(AudioSource sourceToRemove)
    {
        List<int> keysToRemove = new List<int>();
        foreach (var entry in activeBGMPlayers)
        {
            if (entry.Value.source == sourceToRemove)
            {
                keysToRemove.Add(entry.Key);
            }
        }
        foreach (int key in keysToRemove)
        {
            activeBGMPlayers.Remove(key);
        }
    }

    /// <summary>
    /// 두 개의 BGM을 즉시 변경합니다 (페이드 없음).
    /// </summary>
    /// <param name="bgm1Index">첫 번째 BGM 클립 인덱스 (-1 또는 BGM_STOP은 재생 안 함)</param>
    /// <param name="bgm2Index">두 번째 BGM 클립 인덱스 (-1 또는 BGM_STOP은 재생 안 함)</param>
    /// <param name="volume1Multiplier">첫 번째 BGM의 상대적 볼륨 배율</param>
    /// <param name="volume2Multiplier">두 번째 BGM의 상대적 볼륨 배율</param>
    public void ChangeDualBGMImmediate(int bgm1Index, int bgm2Index = BGM_STOP, float volume1Multiplier = 1f, float volume2Multiplier = 1f)
    {
        StopAllBGMCoroutines();
        StopAllBGM(); // 모든 BGM 플레이어 정지 및 activeBGMPlayers 초기화

        if (bgm1Index != BGM_STOP && bgm1Index < bgmClips.Length && bgm1Index >= 0)
        {
            AudioSource player = bgmAudioSources[0]; // 첫 번째 BGM 플레이어 사용
            player.clip = bgmClips[bgm1Index];
            player.volume = masterBGMVolume * volume1Multiplier;
            player.Play();
            activeBGMPlayers[bgm1Index] = (player, volume1Multiplier);
        }

        if (bgm2Index != BGM_STOP && bgm2Index < bgmClips.Length && bgm2Index >= 0)
        {
            AudioSource player = bgmAudioSources[1]; // 두 번째 BGM 플레이어 사용
            player.clip = bgmClips[bgm2Index];
            player.volume = masterBGMVolume * volume2Multiplier;
            player.Play();
            activeBGMPlayers[bgm2Index] = (player, volume2Multiplier);
        }
    }

    private AudioSource GetAvailableBGMPlayer() // 이 메서드는 더 이상 ChangeDualBGM에서 직접 사용되지 않습니다.
    {
        // 현재 사용되지 않는 플레이어 찾기
        foreach (AudioSource player in bgmAudioSources)
        {
            bool isUsed = false;
            foreach (var activePlayerEntry in activeBGMPlayers.Values) // 튜플로 변경
            {
                if (activePlayerEntry.source == player) // 튜플의 source 필드 사용
                {
                    isUsed = true;
                    break;
                }
            }
            if (!isUsed)
            {
                return player;
            }
        }
        Debug.LogWarning("No available BGM player found. Max 2 BGM players are allowed.");
        return null;
    }

    private IEnumerator FadeInCoroutine(AudioSource player, AudioClip clip, float targetVolume, float duration, float delay, int bgmIndex, float volumeMultiplier)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        // (추가) 사전 등록: 볼륨은 일단 0 기준으로
        RemoveBGMPlayerFromActive(player);
        activeBGMPlayers[bgmIndex] = (player, volumeMultiplier); // ← 이 줄 추가

        // 현재 클립이 재생하려는 클립과 다를 경우에만 정지하고 새로 설정
        // 또는 플레이어가 정지 상태일 경우 새로 설정
        if (player.clip != clip || !player.isPlaying)
        {
            player.Stop();
            player.clip = clip;
            player.volume = 0; // 시작 볼륨을 0으로 설정
            player.Play();
        }

        float startVolume = player.volume;
        float timeElapsed = 0f;

        while (timeElapsed < duration)
        {
            timeElapsed += Time.deltaTime;
            player.volume = Mathf.Lerp(startVolume, targetVolume, Mathf.SmoothStep(0f, 1f, timeElapsed / duration));
            yield return null;
        }
        player.volume = targetVolume; // 최종 볼륨 보장


        RemoveBGMPlayerFromActive(player);
        activeBGMPlayers[bgmIndex] = (player, volumeMultiplier);
    }

    private IEnumerator FadeOutCoroutine(AudioSource player, float duration, int bgmIndex)
    {
        AudioClip initialClip = player.clip;
        float startVolume = player.volume;
        float timeElapsed = 0f;

        // BGM이 재생 중이고, 코루틴 시작 시의 클립과 현재 클립이 동일할 때만 페이드 아웃 진행
        while (timeElapsed < duration && player.isPlaying && player.clip == initialClip)
        {
            timeElapsed += Time.deltaTime;
            // Mathf.SmoothStep을 사용하여 부드러운 페이드 아웃
            player.volume = Mathf.Lerp(startVolume, 0, Mathf.SmoothStep(0f, 1f, timeElapsed / duration));
            yield return null;
        }

        if (player.clip == initialClip)
        {
            player.Stop();
            player.clip = null; // 클립도 해제
            activeBGMPlayers.Remove(bgmIndex); // 딕셔너리에서 제거
        }
    }

    /// <summary>
    /// 특정 BGM의 볼륨을 조절합니다.
    /// </summary>
    /// <param name="bgmIndex">BGM 클립 인덱스</param>
    /// <param name="volumeMultiplier">적용할 상대적 볼륨 배율</param>
    public void SetBGMVolume(int bgmIndex, float volumeMultiplier)
    {
        if (activeBGMPlayers.TryGetValue(bgmIndex, out var entry)) // 튜플로 변경
        {
            entry.source.volume = masterBGMVolume * volumeMultiplier;
            activeBGMPlayers[bgmIndex] = (entry.source, volumeMultiplier); // 볼륨 배율 업데이트
        }
    }

    /// <summary>
    /// 특정 BGM을 페이드 아웃합니다.
    /// </summary>
    /// <param name="bgmIndex">BGM 클립 인덱스</param>
    /// <param name="duration">페이드 아웃 시간</param>
    public void FadeOutSpecificBGM(int bgmIndex, float duration = 2f)
    {
        if (activeBGMPlayers.TryGetValue(bgmIndex, out var entry)) // 튜플로 변경
        {
            bgmFadeCoroutines.Add(StartCoroutine(FadeOutCoroutine(entry.source, duration, bgmIndex)));
        }
    }

    /// <summary>
    /// 모든 BGM을 페이드 아웃합니다.
    /// </summary>
    /// <param name="duration">페이드 아웃 시간</param>
    public void FadeOutAllBGM(float duration = 2f)
    {
        StopAllBGMCoroutines(); // 모든 기존 페이드 코루틴 중지

        List<int> currentActiveBGMIndexes = new List<int>(activeBGMPlayers.Keys);
        foreach (int bgmIndex in currentActiveBGMIndexes)
        {
            if (activeBGMPlayers.TryGetValue(bgmIndex, out var entry))
            {
                bgmFadeCoroutines.Add(StartCoroutine(FadeOutCoroutine(entry.source, duration, bgmIndex)));
            }
        }
    }

    private void StopAllBGMCoroutines()
    {
        foreach (Coroutine coroutine in bgmFadeCoroutines)
        {
            if (coroutine != null)
                StopCoroutine(coroutine);
        }
        bgmFadeCoroutines.Clear();
    }

    /// <summary>
    /// 모든 BGM을 즉시 정지합니다.
    /// </summary>
    public void StopAllBGM()
    {
        StopAllBGMCoroutines(); // 혹시 모를 잔여 페이드 코루틴 중지

        foreach (AudioSource player in bgmAudioSources)
        {
            player.Stop();
            player.clip = null; // 클립도 해제하여 다음 재생을 위해 준비
        }
        activeBGMPlayers.Clear();
    }

    /// <summary>
    /// 현재 재생 중인 모든 BGM을 일시 정지합니다.
    /// </summary>
    public void PauseBGM()
    {
        foreach (var entry in activeBGMPlayers.Values) 
        {
            if (entry.source.isPlaying)
                entry.source.Pause();
        }
    }

    /// <summary>
    /// 일시 정지된 모든 BGM을 다시 재생합니다.
    /// </summary>
    public void ResumeBGM()
    {
        foreach (var entry in activeBGMPlayers.Values) // activeBGMPlayers를 순회하여 정확히 일시 정지된 것만 제어
        {
            if (entry.source.clip != null && !entry.source.isPlaying)
                entry.source.UnPause();
        }
    }

    /// <summary>
    /// 씬 이름에 따라 BGM을 변경합니다.
    /// </summary>
    /// <param name="sceneName">대상 씬 이름</param>
    public void ChangeSceneBGM(string sceneName)
    {
        CheckAndPlaySceneBGM(sceneName);
    }

    /// <summary>
    /// 현재 씬의 BGM 설정을 반환합니다.
    /// AudioZoneEffect 스크립트에서 기본 볼륨 설정을 가져올 때 사용됩니다.
    /// </summary>
    /// <param name="sceneName">현재 씬 이름</param>
    /// <returns>씬의 BGM 설정 또는 null</returns>
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

    /// <summary>
    /// UI 루프 사운드를 재생하거나 정지합니다.
    /// </summary>
    /// <param name="num">UI 루프 사운드 클립의 인덱스</param>
    /// <param name="play">재생 여부 (true: 재생, false: 정지)</param>
    public void UISoundPlay_LOOP(int num, bool play)
    {
        if (num < 0 || num >= uiSoundLoopClips.Length)
        {
            Debug.LogWarning($"Invalid UI loop sound index: {num}");
            return;
        }

        AudioClip targetClip = uiSoundLoopClips[num];

        if (play)
        {
            // 이미 재생 중인 같은 루프 사운드가 있는지 확인
            foreach (AudioSource audio in uiSoundLoopPlayers)
            {
                if (audio != null && audio.isPlaying && audio.clip == targetClip)
                {
                    return; // 이미 재생 중이므로 다시 재생하지 않음
                }
            }

            // 비어있는 플레이어를 찾아 재생
            foreach (AudioSource audio in uiSoundLoopPlayers)
            {
                if (audio != null && !audio.isPlaying)
                {
                    audio.clip = targetClip;
                    audio.volume = masterSFXVolume;
                    audio.Play();
                    return;
                }
            }
            Debug.LogWarning("No available UI loop sound player found. Consider increasing array size.");
        }
        else // 정지
        {
            foreach (AudioSource audio in uiSoundLoopPlayers)
            {
                if (audio != null && audio.isPlaying && audio.clip == targetClip)
                {
                    audio.Stop();
                    audio.clip = null; // 클립도 해제
                    return;
                }
            }
        }
    }

    /// <summary>
    /// 단발성 UI 사운드를 재생합니다. (UISoundPlayer 활용)
    /// </summary>
    /// <param name="num">UI 사운드 클립의 인덱스</param>
    /// <param name="uiPlayerIndex">사용할 UISoundPlayer의 인덱스 (-1이면 사용 가능한 플레이어 자동 선택)</param>
    public void UISoundPlay(int num, int uiPlayerIndex = -1)
    {
        if (num == -1) return;

        if (num < 0 || num >= uiSoundClips.Length)
        {
            Debug.LogWarning($"Invalid UI sound index: {num}");
            return;
        }

        AudioClip clip = uiSoundClips[num];

        // 현재 이 clip을 재생 중인 플레이어 수를 센다
        int playingCount = 0;
        foreach (var player in uiPlayers)
        {
            if (player != null && player.IsPlaying(clip))
            {
                playingCount++;
            }
        }

        // 최대 3개까지 허용
        if (playingCount >= 3) return;

        UISoundPlayer targetPlayer = null;
        if (uiPlayerIndex != -1 && uiPlayerIndex >= 0 && uiPlayerIndex < uiPlayers.Length)
        {
            targetPlayer = uiPlayers[uiPlayerIndex];
            if (targetPlayer == null)
                Debug.LogWarning($"UISoundPlayer at index {uiPlayerIndex} is null. Searching for an available player.");
        }

        if (targetPlayer == null)
        {
            // 사용 가능한 UISoundPlayer를 찾음
            foreach (var player in uiPlayers)
            {
                // 현재 플레이어가 재생 중이 아니거나, 같은 clip을 재생 중이 아닌 경우!
                if (player != null && !player.IsPlaying())
                {
                    targetPlayer = player;
                    break;
                }
            }
        }

        if (targetPlayer != null)
        {
            targetPlayer.Play(clip, masterSFXVolume, SoundPosition());
        }
        else
        {
            Debug.LogWarning("No available UISoundPlayer found. Consider increasing uiPlayers array size or check assignments in Inspector.");
        }
    }


    /// <summary>
    /// 특정 UI 사운드를 정지합니다. (UISoundPlayer 활용)
    /// </summary>
    /// <param name="num">정지할 UI 사운드 클립의 인덱스 (SOUND_TYPING 또는 UISoundPlay로 재생된 클립)</param>
    /// <param name="uiPlayerIndex">정지할 UISoundPlayer의 인덱스 (-1이면 해당 클립을 재생 중인 모든 플레이어 정지)</param>
    public void UISoundStop(int num, int uiPlayerIndex = -1)
    {
        if (num == SOUND_TYPING)
        {
            if (typingSoundPlayer != null)
            {
                typingSoundPlayer.Stop();
                typingSoundPlayer.clip = null; // 클립도 해제
            }
            return;
        }

        AudioClip targetClip = null;
        if (num >= 0 && num < uiSoundClips.Length)
        {
            targetClip = uiSoundClips[num];
        }
        else
        {
            Debug.LogWarning($"Invalid UI sound index for stopping: {num}");
            return;
        }

        if (targetClip == null) return;

        if (uiPlayerIndex != -1 && uiPlayerIndex >= 0 && uiPlayerIndex < uiPlayers.Length)
        {
            if (uiPlayers[uiPlayerIndex] != null && uiPlayers[uiPlayerIndex].IsPlaying(targetClip))
            {
                uiPlayers[uiPlayerIndex].Stop();
            }
        }
        else
        {
            // 해당 클립을 재생 중인 모든 UISoundPlayer 정지
            foreach (var player in uiPlayers)
            {
                if (player != null && player.IsPlaying(targetClip))
                {
                    player.Stop();
                }
            }
        }
    }

    /// <summary>
    /// 특정 UI 사운드를 일시 정지합니다. (UISoundPlayer 활용)
    /// </summary>
    /// <param name="num">일시 정지할 UI 사운드 클립의 인덱스</param>
    /// <param name="uiPlayerIndex">일시 정지할 UISoundPlayer의 인덱스 (-1이면 해당 클립을 재생 중인 모든 플레이어 일시 정지)</param>
    public void UISoundPause(int num, int uiPlayerIndex = -1)
    {
        AudioClip targetClip = null;
        if (num >= 0 && num < uiSoundClips.Length)
        {
            targetClip = uiSoundClips[num];
        }
        else
        {
            Debug.LogWarning($"Invalid UI sound index for pausing: {num}");
            return;
        }

        if (targetClip == null) return;

        if (uiPlayerIndex != -1 && uiPlayerIndex >= 0 && uiPlayerIndex < uiPlayers.Length)
        {
            if (uiPlayers[uiPlayerIndex] != null && uiPlayers[uiPlayerIndex].IsPlaying(targetClip))
            {
                uiPlayers[uiPlayerIndex].Pause();
            }
        }
        else
        {
            foreach (var player in uiPlayers)
            {
                if (player != null && player.IsPlaying(targetClip))
                {
                    player.Pause();
                }
            }
        }
    }

    /// <summary>
    /// 특정 UI 사운드를 다시 재생합니다. (UISoundPlayer 활용)
    /// </summary>
    /// <param name="num">다시 재생할 UI 사운드 클립의 인덱스</param>
    /// <param name="uiPlayerIndex">다시 재생할 UISoundPlayer의 인덱스 (-1이면 해당 클립을 일시 정지 중인 모든 플레이어 다시 재생)</param>
    public void UISoundResume(int num, int uiPlayerIndex = -1)
    {
        AudioClip targetClip = null;
        if (num >= 0 && num < uiSoundClips.Length)
        {
            targetClip = uiSoundClips[num];
        }
        else
        {
            Debug.LogWarning($"Invalid UI sound index for resuming: {num}");
            return;
        }

        if (targetClip == null) return;

        if (uiPlayerIndex != -1 && uiPlayerIndex >= 0 && uiPlayerIndex < uiPlayers.Length)
        {
            if (uiPlayers[uiPlayerIndex] != null && uiPlayers[uiPlayerIndex].AudioSource.clip == targetClip && !uiPlayers[uiPlayerIndex].IsPlaying())
            {
                uiPlayers[uiPlayerIndex].Resume();
            }
        }
        else
        {
            foreach (var player in uiPlayers)
            {
                if (player != null && player.AudioSource.clip == targetClip && !player.IsPlaying())
                {
                    player.Resume();
                }
            }
        }
    }


    private float SoundPosition()
    {
        Vector3 clickPosition = Input.mousePosition;
        float normalizedX = (clickPosition.x / Screen.width - 0.5f) * 2f;
        return normalizedX;
    }

    // 현재 재생 중인 BGM 정보 (bgmPlayers 배열 대신 activeBGMPlayers 딕셔너리 기반)
    public List<int> GetCurrentBGMs() => new List<int>(activeBGMPlayers.Keys);
    public bool IsBGMPlaying() => activeBGMPlayers.Count > 0;
    public int GetBGMCount() => activeBGMPlayers.Count;

    // BGM 플레이어 배열 반환 (읽기 전용으로 변경)
    public IReadOnlyList<AudioSource> GetBGMPlayers()
    {
        return bgmAudioSources;
    }

    // 현재 BGM 마스터 볼륨 반환
    public float GetBGMVolume()
    {
        return masterBGMVolume;
    }

    public float GetSFXVolume()
    {
        return masterSFXVolume;
    }
}