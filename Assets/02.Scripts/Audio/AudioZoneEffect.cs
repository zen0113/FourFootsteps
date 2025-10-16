using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioZoneEffect : MonoBehaviour
{
    [System.Serializable]
    public class BGMVolumeSettings
    {
        [Header("Volume Settings")]
        [Range(0f, 1f)] public float volumeMultiplier = 1f;
    }

    [System.Serializable]
    public class AudioZone
    {
        public string zoneName;
        public Collider2D triggerZone;

        [Header("BGM1 Volume in Zone")]
        public BGMVolumeSettings bgm1Settings = new BGMVolumeSettings { volumeMultiplier = 1f };

        [Header("BGM2 Volume in Zone")]
        public BGMVolumeSettings bgm2Settings = new BGMVolumeSettings { volumeMultiplier = 0.3f };
    }

    [Header("Audio Zone Settings")]
    [SerializeField] private AudioZone[] audioZones;
    [SerializeField] private float checkInterval = 0.1f;
    [SerializeField] private float transitionDuration = 2f; // Lerp 대신 FadeDuration으로 사용

    [Header("Scene Settings")]
    [SerializeField] private string targetSceneName;

    [Header("References")]
    [SerializeField] private Transform playerTransform;

    // 현재 플레이어가 어떤 존에 있는지 추적 (중복 이벤트 발생 방지)
    private AudioZone currentActiveZone = null;
    private List<int> currentPlayingBGMIndexes = new List<int>(); // SoundPlayer에서 현재 재생 중인 BGM 인덱스

    // 씬의 기본 BGM 설정을 저장할 변수
    private int defaultBGM1Index = -1;
    private float defaultBGM1Multiplier = 1f;
    private int defaultBGM2Index = -1;
    private float defaultBGM2Multiplier = 1f;

    private float nextCheckTime;

    private void Start()
    {
        // 씬 체크
        if (!string.IsNullOrEmpty(targetSceneName) &&
            SceneManager.GetActiveScene().name != targetSceneName)
        {
            enabled = false;
            return;
        }

        InitializeComponents();

        // SoundPlayer에서 현재 씬의 기본 BGM 설정 가져오기
        if (SoundPlayer.Instance != null)
        {
            SoundPlayer.SceneBGMSetting currentSceneSetting = SoundPlayer.Instance.GetSceneBGMSetting(SceneManager.GetActiveScene().name);
            if (currentSceneSetting != null)
            {
                defaultBGM1Index = currentSceneSetting.bgmIndex1;
                defaultBGM1Multiplier = currentSceneSetting.bgm1VolumeMultiplier;
                defaultBGM2Index = currentSceneSetting.bgmIndex2;
                defaultBGM2Multiplier = currentSceneSetting.bgm2VolumeMultiplier;

                Debug.Log($"AudioZoneEffect: Default BGM settings initialized from SoundPlayer for scene {currentSceneSetting.sceneName}. BGM1 Index: {defaultBGM1Index}, Multiplier: {defaultBGM1Multiplier}, BGM2 Index: {defaultBGM2Index}, Multiplier: {defaultBGM2Multiplier}");
            }
            else
            {
                // SoundPlayer에 씬 설정이 없는 경우, 기본값(1f, 0.3f) 유지 또는 경고
                Debug.LogWarning("AudioZoneEffect: No specific BGM setting found in SoundPlayer for the current scene. Using hardcoded default values (BGM1: 1f, BGM2: 0.3f).");
                defaultBGM1Index = -1; // 또는 기본 BGM 인덱스 지정
                defaultBGM1Multiplier = 1f;
                defaultBGM2Index = -1; // 또는 기본 BGM 인덱스 지정
                defaultBGM2Multiplier = 0.3f;
            }
        }
        else
        {
            Debug.LogWarning("AudioZoneEffect: SoundPlayer instance not found. Using hardcoded default BGM volumes.");
            defaultBGM1Index = -1;
            defaultBGM1Multiplier = 1f;
            defaultBGM2Index = -1;
            defaultBGM2Multiplier = 0.3f;
        }

        // 초기에는 기본 설정에 맞춰 볼륨 조절 (존 바깥이므로)
        // BGMVolumeSettings 객체를 생성하여 전달
        BGMVolumeSettings initialBGM1Settings = new BGMVolumeSettings { volumeMultiplier = defaultBGM1Multiplier };
        BGMVolumeSettings initialBGM2Settings = new BGMVolumeSettings { volumeMultiplier = defaultBGM2Multiplier };
        //ApplyVolumeSettingsToBGMs(initialBGM1Settings, initialBGM2Settings);
        StartCoroutine(ApplyInitialVolumesWhenReady());

        nextCheckTime = Time.time; // 즉시 첫 체크 실행
    }

    private IEnumerator ApplyInitialVolumesWhenReady()
    {
        // 1) SoundPlayer 인스턴스가 뜰 때까지
        yield return new WaitUntil(() => SoundPlayer.Instance != null);

        // 2) (최대 2초) 실제 등록(activeBGMPlayers)이 될 때까지 대기
        float timeout = 2f;
        while (timeout > 0f && SoundPlayer.Instance.activeBGMPlayers.Count == 0)
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        // 3) 등록이 끝났다면, 현재 실제 플레이어와 매칭된 인덱스로 페이드 적용
        ApplyVolumesToCurrentPlayers(defaultBGM1Multiplier, defaultBGM2Multiplier);
    }

    private void InitializeComponents()
    {
        // 플레이어 찾기
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
        }

        if (playerTransform == null)
        {
            Debug.LogWarning("AudioZoneEffect: Player not found! Make sure player has 'Player' tag.");
            enabled = false;
            return;
        }


        if (SoundPlayer.Instance == null)
        {
            Debug.LogWarning("AudioZoneEffect: SoundPlayer not found! BGM volume changes will not work.");
        }
    }

    private void Update()
    {
        if (Time.time >= nextCheckTime)
        {
            CheckAudioZones();
            nextCheckTime = Time.time + checkInterval;
        }
    }

    private void CheckAudioZones()
    {
        if (playerTransform == null) return;

        // 아직 SoundPlayer가 준비되지 않았거나, BGM 등록이 끝나지 않았다면 대기
        if (SoundPlayer.Instance == null) return;
        if (SoundPlayer.Instance.activeBGMPlayers.Count == 0) return;

        // 현재 재생 중인 BGM 인덱스 최신화(디버그/확인용)
        currentPlayingBGMIndexes = SoundPlayer.Instance.GetCurrentBGMs();

        // 플레이어가 들어가 있는 존 탐색
        AudioZone playerInZone = null;
        // 각 오디오 존 체크
        foreach (var zone in audioZones)
        {
            if (zone?.triggerZone == null) continue;

            if (zone.triggerZone.OverlapPoint(playerTransform.position))
            {
                playerInZone = zone;
                break;
            }
        }

        // 존 변동이 있을 때만 처리(중복 트리거 방지)
        if (playerInZone == currentActiveZone) return;

        currentActiveZone = playerInZone;

        if (currentActiveZone != null)
        {
            // 존 진입: 실제 플레이어(플레이어1/2)에 매핑된 인덱스 기준으로 페이드 트리거
            Debug.Log($"Player entered zone: {currentActiveZone.zoneName}");
            ApplyVolumesToCurrentPlayers(
                currentActiveZone.bgm1Settings?.volumeMultiplier ?? defaultBGM1Multiplier,
                currentActiveZone.bgm2Settings?.volumeMultiplier ?? defaultBGM2Multiplier
            );
        }
        else
        {
            // 존 이탈: 씬의 기본값으로 복귀 (역시 실제 플레이어 매핑 기준)
            Debug.Log("Player exited all zones, returning to default BGM volume.");
            ApplyVolumesToCurrentPlayers(defaultBGM1Multiplier, defaultBGM2Multiplier);
        }
    }

    /// <summary>
    /// AudioEventSystem을 통해 BGM 볼륨 변경 이벤트를 발생시킵니다.
    /// </summary>
    /// <param name="bgm1Settings">첫 번째 BGM에 적용할 볼륨 설정</param>
    /// <param name="bgm2Settings">두 번째 BGM에 적용할 볼륨 설정</param>
    private void ApplyVolumeSettingsToBGMs(BGMVolumeSettings bgm1Settings, BGMVolumeSettings bgm2Settings)
    {
        // SoundPlayer 인스턴스가 존재하고, defaultBGM1Index가 유효한 경우 (즉, Start에서 설정되었을 경우)
        if (SoundPlayer.Instance != null && defaultBGM1Index != -1)
        {
            AudioEventSystem.TriggerBGMVolumeFade(defaultBGM1Index, bgm1Settings.volumeMultiplier, transitionDuration);
        }
        else if (SoundPlayer.Instance == null)
        {
            Debug.LogWarning("AudioZoneEffect: SoundPlayer instance not found. Cannot apply BGM1 volume settings.");
        }
        else // defaultBGM1Index가 -1인 경우 (Start에서 초기화되지 않았거나 기본 BGM이 없는 경우)
        {
            Debug.Log("AudioZoneEffect: Default BGM1 index is not set. Skipping BGM1 volume adjustment.");
        }


        // SoundPlayer 인스턴스가 존재하고, defaultBGM2Index가 유효한 경우 (즉, Start에서 설정되었을 경우)
        if (SoundPlayer.Instance != null && defaultBGM2Index != -1)
        {
            AudioEventSystem.TriggerBGMVolumeFade(defaultBGM2Index, bgm2Settings.volumeMultiplier, transitionDuration);
        }
        else if (SoundPlayer.Instance == null)
        {
            Debug.LogWarning("AudioZoneEffect: SoundPlayer instance not found. Cannot apply BGM2 volume settings.");
        }
        else // defaultBGM2Index가 -1인 경우 (Start에서 초기화되지 않았거나 기본 BGM이 없는 경우)
        {
            Debug.Log("AudioZoneEffect: Default BGM2 index is not set. Skipping BGM2 volume adjustment.");
        }
    }

    private void ApplyVolumesToCurrentPlayers(float bgm1Multiplier, float bgm2Multiplier)
    {
        if (SoundPlayer.Instance == null) return;

        var players = SoundPlayer.Instance.GetBGMPlayers();
        if (players == null || players.Count == 0) return;

        int? idx1 = FindIndexBySource(players[0]);
        int? idx2 = (players.Count > 1) ? FindIndexBySource(players[1]) : null;

        if (idx1.HasValue)
            AudioEventSystem.TriggerBGMVolumeFade(idx1.Value, bgm1Multiplier, transitionDuration);

        if (idx2.HasValue)
            AudioEventSystem.TriggerBGMVolumeFade(idx2.Value, bgm2Multiplier, transitionDuration);
    }

    private int? FindIndexBySource(AudioSource src)
    {
        if (src == null || SoundPlayer.Instance == null) return null;

        foreach (var kv in SoundPlayer.Instance.activeBGMPlayers)
        {
            if (kv.Value.source == src) return kv.Key;
        }
        return null;
    }


    private BGMVolumeSettings CloneBGMVolumeSettings(BGMVolumeSettings original)
    {
        if (original == null) return new BGMVolumeSettings();
        return new BGMVolumeSettings
        {
            volumeMultiplier = original.volumeMultiplier
        };
    }


    public void SetZoneEffect(int zoneIndex, bool enable)
    {
        if (zoneIndex >= 0 && zoneIndex < audioZones.Length)
        {
            if (audioZones[zoneIndex].triggerZone != null)
            {
                audioZones[zoneIndex].triggerZone.gameObject.SetActive(enable);
            }
        }
    }

    public void SetTransitionSpeed(float speed)
    {
        transitionDuration = Mathf.Max(0.1f, speed); // Speed -> Duration으로 변경
    }

    public void SetCheckInterval(float interval)
    {
        checkInterval = Mathf.Max(0.01f, interval);
    }

    public bool IsPlayerInAnyZone()
    {
        if (playerTransform == null) return false;

        foreach (var zone in audioZones)
        {
            if (zone.triggerZone != null && zone.triggerZone.OverlapPoint(playerTransform.position))
            {
                return true;
            }
        }
        return false;
    }

    public string GetCurrentZoneName()
    {
        if (playerTransform == null) return "None";

        foreach (var zone in audioZones)
        {
            if (zone.triggerZone != null && zone.triggerZone.OverlapPoint(playerTransform.position))
            {
                return zone.zoneName;
            }
        }
        return "Outside";
    }

    private void TriggerFadeSafely(int index, float multiplier, float duration)
    {
        StartCoroutine(Co_TriggerFadeSafely(index, multiplier, duration));
    }

    private IEnumerator Co_TriggerFadeSafely(int index, float multiplier, float duration)
    {
        for (int i = 0; i < 5; i++) // 최대 5프레임 대기
        {
            if (SoundPlayer.Instance != null &&
                SoundPlayer.Instance.activeBGMPlayers.ContainsKey(index))
            {
                AudioEventSystem.TriggerBGMVolumeFade(index, multiplier, duration);
                yield break;
            }
            yield return null;
        }
        // 실패 시에는 실제 플레이어 매핑으로 대체 트리거
        ApplyVolumesToCurrentPlayers(
            index == defaultBGM1Index ? multiplier : defaultBGM1Multiplier,
            index == defaultBGM2Index ? multiplier : defaultBGM2Multiplier
        );
    }


    // 기즈모로 존 시각화
    private void OnDrawGizmos()
    {
        if (audioZones == null) return;

        for (int i = 0; i < audioZones.Length; i++)
        {
            var zone = audioZones[i];
            if (zone.triggerZone == null) continue;

            Gizmos.color = new Color(1f, 1f, 0f, 0.3f + (i * 0.1f) % 0.7f);

            if (zone.triggerZone is BoxCollider2D boxCollider)
            {
                Gizmos.matrix = Matrix4x4.TRS(boxCollider.transform.position, boxCollider.transform.rotation, boxCollider.transform.localScale);
                Gizmos.DrawWireCube(boxCollider.offset, boxCollider.size);
                Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.1f);
                Gizmos.DrawCube(boxCollider.offset, boxCollider.size);
            }
            else if (zone.triggerZone is CircleCollider2D circleCollider)
            {
                Gizmos.matrix = Matrix4x4.TRS(circleCollider.transform.position, circleCollider.transform.rotation, circleCollider.transform.localScale);
                Gizmos.DrawWireSphere(circleCollider.offset, circleCollider.radius);
                Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.1f);
                Gizmos.DrawSphere(circleCollider.offset, circleCollider.radius);
            }
            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}