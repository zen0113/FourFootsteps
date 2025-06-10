using UnityEngine;

public class AudioZoneEffect : MonoBehaviour
{
    [System.Serializable]
    public class BGMEffectSettings
    {
        [Header("Volume Settings")]
        [Range(0f, 1f)] public float volumeMultiplier = 1f;

        [Header("Low Pass Filter Settings")]
        [Range(0f, 1f)] public float lowPassCutoff = 1f; // 1 = 정상, 0 = 최대 차단

        [Header("Reverb Settings")]
        [Range(0f, 1f)] public float reverbLevel = 0f; // 0 = 없음, 1 = 최대
    }

    [System.Serializable]
    public class AudioZone
    {
        public string zoneName;
        public Collider2D triggerZone;

        [Header("BGM1 Settings in Zone")]
        public BGMEffectSettings bgm1Settings = new BGMEffectSettings { volumeMultiplier = 0f, lowPassCutoff = 1f, reverbLevel = 0f };

        [Header("BGM2 Settings in Zone")]
        public BGMEffectSettings bgm2Settings = new BGMEffectSettings { volumeMultiplier = 1f, lowPassCutoff = 1f, reverbLevel = 0f };
    }

    [Header("Audio Zone Settings")]
    [SerializeField] private AudioZone[] audioZones;
    [SerializeField] private float checkInterval = 0.1f;
    [SerializeField] private float transitionSpeed = 2f;

    [Header("Default Effect Settings (Outside Zones)")]
    [SerializeField]
    private BGMEffectSettings defaultBGM1Settings = new BGMEffectSettings
    {
        volumeMultiplier = 1f,
        lowPassCutoff = 0.2f,
        reverbLevel = 0.8f
    };

    [SerializeField]
    private BGMEffectSettings defaultBGM2Settings = new BGMEffectSettings
    {
        volumeMultiplier = 0.3f,
        lowPassCutoff = 0.2f,
        reverbLevel = 0.8f
    };

    [Header("Scene Settings")]
    [SerializeField] private string targetSceneName; // 특정 씬에만 적용

    [Header("References")]
    [SerializeField] private Transform playerTransform;

    // 현재 효과 값들
    private BGMEffectSettings currentBGM1Effects = new BGMEffectSettings();
    private BGMEffectSettings currentBGM2Effects = new BGMEffectSettings();

    // 오디오 컴포넌트들
    private AudioSource[] bgmAudioSources;
    private AudioLowPassFilter[] lowPassFilters;
    private AudioReverbFilter[] reverbFilters;

    // 타이밍 제어
    private float nextCheckTime;

    private void Start()
    {
        // 씬 체크
        if (!string.IsNullOrEmpty(targetSceneName) &&
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != targetSceneName)
        {
            enabled = false;
            return;
        }

        InitializeComponents();

        // 기본 효과로 시작
        currentBGM1Effects = CloneBGMEffectSettings(defaultBGM1Settings);
        currentBGM2Effects = CloneBGMEffectSettings(defaultBGM2Settings);

        ApplyAudioEffects();
    }

    private void InitializeComponents()
    {
        // 플레이어 찾기
        if (playerTransform == null)
            playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (playerTransform == null)
        {
            Debug.LogWarning("Player not found! Make sure player has 'Player' tag.");
            enabled = false;
            return;
        }

        // SoundPlayer에서 BGM 오디오 소스 가져오기
        if (SoundPlayer.Instance != null)
        {
            bgmAudioSources = SoundPlayer.Instance.GetBGMPlayers();
        }

        if (bgmAudioSources == null || bgmAudioSources.Length < 2)
        {
            Debug.LogWarning("BGM Audio Sources not found or insufficient!");
            enabled = false;
            return;
        }

        InitializeAudioFilters();
    }

    private void InitializeAudioFilters()
    {
        lowPassFilters = new AudioLowPassFilter[2];
        reverbFilters = new AudioReverbFilter[2];

        for (int i = 0; i < 2; i++)
        {
            if (bgmAudioSources[i] != null)
            {
                // Low Pass Filter 초기화
                AudioLowPassFilter existingLowPass = bgmAudioSources[i].GetComponent<AudioLowPassFilter>();
                if (existingLowPass != null)
                    DestroyImmediate(existingLowPass);

                lowPassFilters[i] = bgmAudioSources[i].gameObject.AddComponent<AudioLowPassFilter>();
                lowPassFilters[i].cutoffFrequency = 22000f;

                // Reverb Filter 초기화
                AudioReverbFilter existingReverb = bgmAudioSources[i].GetComponent<AudioReverbFilter>();
                if (existingReverb != null)
                    DestroyImmediate(existingReverb);

                reverbFilters[i] = bgmAudioSources[i].gameObject.AddComponent<AudioReverbFilter>();
                reverbFilters[i].reverbPreset = AudioReverbPreset.Room;
                reverbFilters[i].dryLevel = 0f;
                reverbFilters[i].reverbLevel = -10000f; // 시작은 리버브 없음
                reverbFilters[i].decayTime = 1.5f;
                reverbFilters[i].diffusion = 100f;
                reverbFilters[i].density = 100f;
                reverbFilters[i].hfReference = 5000f;
            }
        }
    }

    private void Update()
    {
        if (Time.time >= nextCheckTime)
        {
            CheckAudioZones();
            ApplyAudioEffects();
            nextCheckTime = Time.time + checkInterval;
        }
    }

    private void CheckAudioZones()
    {
        if (playerTransform == null) return;

        // 기본값으로 시작 (벽에 막힌 효과)
        BGMEffectSettings targetBGM1 = CloneBGMEffectSettings(defaultBGM1Settings);
        BGMEffectSettings targetBGM2 = CloneBGMEffectSettings(defaultBGM2Settings);

        // 각 오디오 존 체크
        foreach (var zone in audioZones)
        {
            if (zone.triggerZone == null) continue;

            // 단순히 트리거 존 안에 있는지만 체크
            if (zone.triggerZone.OverlapPoint(playerTransform.position))
            {
                targetBGM1 = CloneBGMEffectSettings(zone.bgm1Settings);
                targetBGM2 = CloneBGMEffectSettings(zone.bgm2Settings);
                break; // 첫 번째로 발견된 존 사용
            }
        }

        // 부드러운 전환
        LerpBGMEffectSettings(ref currentBGM1Effects, targetBGM1, Time.deltaTime * transitionSpeed);
        LerpBGMEffectSettings(ref currentBGM2Effects, targetBGM2, Time.deltaTime * transitionSpeed);
    }

    private void ApplyAudioEffects()
    {
        if (bgmAudioSources == null) return;

        // BGM1 효과 적용
        ApplyEffectsToAudioSource(0, currentBGM1Effects);

        // BGM2 효과 적용  
        ApplyEffectsToAudioSource(1, currentBGM2Effects);
    }

    private void ApplyEffectsToAudioSource(int index, BGMEffectSettings effects)
    {
        if (index >= bgmAudioSources.Length || bgmAudioSources[index] == null) return;

        AudioSource audioSource = bgmAudioSources[index];

        // 재생 중인 오디오 소스에만 적용
        if (audioSource.isPlaying)
        {
            // 볼륨 적용 (SoundPlayer의 기본 볼륨과 곱하기)
            float baseVolume = SoundPlayer.Instance != null ? SoundPlayer.Instance.GetBGMVolume() : 1f;
            audioSource.volume = baseVolume * effects.volumeMultiplier;

            // Low Pass Filter 적용
            if (lowPassFilters[index] != null)
            {
                // lowPassCutoff: 1 = 22000Hz (정상), 0 = 800Hz (많이 차단됨)
                lowPassFilters[index].cutoffFrequency = Mathf.Lerp(800f, 22000f, effects.lowPassCutoff);
            }

            // Reverb Filter 적용
            if (reverbFilters[index] != null)
            {
                // reverbLevel: 0 = -10000 (없음), 1 = 0 (최대)
                reverbFilters[index].reverbLevel = Mathf.Lerp(-10000f, 0f, effects.reverbLevel);
            }
        }
    }

    // BGMEffectSettings 복사 함수
    private BGMEffectSettings CloneBGMEffectSettings(BGMEffectSettings original)
    {
        return new BGMEffectSettings
        {
            volumeMultiplier = original.volumeMultiplier,
            lowPassCutoff = original.lowPassCutoff,
            reverbLevel = original.reverbLevel
        };
    }

    // BGMEffectSettings 보간 함수
    private void LerpBGMEffectSettings(ref BGMEffectSettings current, BGMEffectSettings target, float lerpFactor)
    {
        current.volumeMultiplier = Mathf.Lerp(current.volumeMultiplier, target.volumeMultiplier, lerpFactor);
        current.lowPassCutoff = Mathf.Lerp(current.lowPassCutoff, target.lowPassCutoff, lerpFactor);
        current.reverbLevel = Mathf.Lerp(current.reverbLevel, target.reverbLevel, lerpFactor);
    }

    // 수동 제어 함수들
    public void SetZoneEffect(int zoneIndex, bool enable)
    {
        if (zoneIndex >= 0 && zoneIndex < audioZones.Length)
        {
            audioZones[zoneIndex].triggerZone.gameObject.SetActive(enable);
        }
    }

    public void SetTransitionSpeed(float speed)
    {
        transitionSpeed = Mathf.Max(0.1f, speed);
    }

    public void SetCheckInterval(float interval)
    {
        checkInterval = Mathf.Max(0.01f, interval);
    }

    // 현재 상태 확인 함수들
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

            // 존별로 다른 색상 사용
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f + (i * 0.1f) % 0.7f);

            if (zone.triggerZone is BoxCollider2D boxCollider)
            {
                Vector3 center = zone.triggerZone.bounds.center;
                Vector3 size = boxCollider.size;
                Gizmos.DrawWireCube(center, size);
                Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.1f);
                Gizmos.DrawCube(center, size);
            }
            else if (zone.triggerZone is CircleCollider2D circleCollider)
            {
                Vector3 center = zone.triggerZone.bounds.center;
                float radius = circleCollider.radius;
                Gizmos.DrawWireSphere(center, radius);
                Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.1f);
                Gizmos.DrawSphere(center, radius);
            }
        }
    }

    private void OnDestroy()
    {
        // 필터들 정리
        if (lowPassFilters != null)
        {
            for (int i = 0; i < lowPassFilters.Length; i++)
            {
                if (lowPassFilters[i] != null)
                    DestroyImmediate(lowPassFilters[i]);
            }
        }

        if (reverbFilters != null)
        {
            for (int i = 0; i < reverbFilters.Length; i++)
            {
                if (reverbFilters[i] != null)
                    DestroyImmediate(reverbFilters[i]);
            }
        }
    }
}