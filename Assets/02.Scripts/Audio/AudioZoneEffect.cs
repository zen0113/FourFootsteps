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
        ApplyVolumeSettingsToBGMs(initialBGM1Settings, initialBGM2Settings);


        nextCheckTime = Time.time; // 즉시 첫 체크 실행
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

        AudioZone playerInZone = null;

        // 현재 재생 중인 BGM 인덱스 가져오기 (매번 체크)
        if (SoundPlayer.Instance != null)
        {
            currentPlayingBGMIndexes = SoundPlayer.Instance.GetCurrentBGMs();
        }

        // 각 오디오 존 체크
        foreach (var zone in audioZones)
        {
            if (zone.triggerZone == null) continue;

            if (zone.triggerZone.OverlapPoint(playerTransform.position))
            {
                playerInZone = zone;
                break;
            }
        }

        // 플레이어가 새로운 존에 들어갔거나, 존에서 벗어났을 때만 볼륨 변경 이벤트 발생
        if (playerInZone != currentActiveZone)
        {
            currentActiveZone = playerInZone;

            if (currentActiveZone != null)
            {
                Debug.Log($"Player entered zone: {currentActiveZone.zoneName}");
                ApplyVolumeSettingsToBGMs(currentActiveZone.bgm1Settings, currentActiveZone.bgm2Settings);
            }
            else
            {
                Debug.Log("Player exited all zones, returning to default BGM volume.");
                // 존을 벗어났을 때 SoundPlayer에서 가져온 기본 값으로 돌아갑니다.
                BGMVolumeSettings default1 = new BGMVolumeSettings { volumeMultiplier = defaultBGM1Multiplier };
                BGMVolumeSettings default2 = new BGMVolumeSettings { volumeMultiplier = defaultBGM2Multiplier };
                ApplyVolumeSettingsToBGMs(default1, default2);
            }
        }
    }

    /// <summary>
    /// AudioEventSystem을 통해 BGM 볼륨 변경 이벤트를 발생시킵니다.
    /// </summary>
    /// <param name="bgm1Settings">첫 번째 BGM에 적용할 볼륨 설정</param>
    /// <param name="bgm2Settings">두 번째 BGM에 적용할 볼륨 설정</param>
    private void ApplyVolumeSettingsToBGMs(BGMVolumeSettings bgm1Settings, BGMVolumeSettings bgm2Settings)
    {
        // bgmAudioSources[0]에 해당하는 BGM의 인덱스를 찾아서 볼륨 조절
        if (SoundPlayer.Instance != null && SoundPlayer.Instance.GetBGMPlayers().Count > 0)
        {
            AudioSource player1Source = SoundPlayer.Instance.GetBGMPlayers()[0];
            int? bgm1Index = null;
            foreach (var entry in SoundPlayer.Instance.activeBGMPlayers)
            {
                if (entry.Value.source == player1Source)
                {
                    bgm1Index = entry.Key;
                    break;
                }
            }

            if (bgm1Index.HasValue)
            {
                AudioEventSystem.TriggerBGMVolumeFade(bgm1Index.Value, bgm1Settings.volumeMultiplier, transitionDuration);
            }
            else
            {
                Debug.Log("AudioZoneEffect: BGM1 player is not currently playing any assigned BGM.");
            }
        }

        // bgmAudioSources[1]에 해당하는 BGM의 인덱스를 찾아서 볼륨 조절
        if (SoundPlayer.Instance != null && SoundPlayer.Instance.GetBGMPlayers().Count > 1)
        {
            AudioSource player2Source = SoundPlayer.Instance.GetBGMPlayers()[1];
            int? bgm2Index = null;
            foreach (var entry in SoundPlayer.Instance.activeBGMPlayers)
            {
                if (entry.Value.source == player2Source)
                {
                    bgm2Index = entry.Key;
                    break;
                }
            }

            if (bgm2Index.HasValue)
            {
                AudioEventSystem.TriggerBGMVolumeFade(bgm2Index.Value, bgm2Settings.volumeMultiplier, transitionDuration);
            }
            else
            {
                // 현재 SoundPlayer의 BGM2 플레이어에 재생 중인 BGM이 없는 경우
                Debug.Log("AudioZoneEffect: BGM2 player is not currently playing any assigned BGM.");
            }
        }
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