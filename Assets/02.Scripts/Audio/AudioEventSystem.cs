using System;

public static class AudioEventSystem
{
    // 특정 BGM의 볼륨을 페이드하는 이벤트 (BGM 인덱스, 목표 볼륨 배율, 페이드 시간, 지연 시간)
    public static event Action<int, float, float, float> OnBGMVolumeFade;

    // 볼륨 변경 이벤트 (BGM 마스터 볼륨, SFX 마스터 볼륨)
    public static event Action<float, float> OnMasterVolumeChanged;

    // BGM 변경 이벤트 (첫 번째 BGM 인덱스, 두 번째 BGM 인덱스)
    public static event Action<int, int> OnBGMChanged;

    // 씬 오디오 변경 이벤트 (씬 이름)
    public static event Action<string> OnSceneAudioChanged;

    /// <summary>
    /// 마스터 볼륨 변경 이벤트를 발생시킵니다.
    /// </summary>
    public static void TriggerMasterVolumeChange(float bgmVol, float sfxVol)
        => OnMasterVolumeChanged?.Invoke(bgmVol, sfxVol);

    /// <summary>
    /// BGM 변경 이벤트를 발생시킵니다.
    /// </summary>
    public static void TriggerBGMChange(int bgm1, int bgm2)
        => OnBGMChanged?.Invoke(bgm1, bgm2);

    /// <summary>
    /// 씬 오디오 변경 이벤트를 발생시킵니다.
    /// </summary>
    public static void TriggerSceneAudioChange(string sceneName)
        => OnSceneAudioChanged?.Invoke(sceneName);

    /// <summary>
    /// 특정 BGM의 볼륨을 페이드하는 이벤트를 발생시킵니다.
    /// </summary>
    /// <param name="bgmIndex">볼륨을 조절할 BGM 클립의 인덱스</param>
    /// <param name="targetVolumeMultiplier">목표 볼륨 배율 (0f ~ 1f)</param>
    /// <param name="duration">페이드에 걸리는 시간</param>
    /// <param name="delay">페이드 시작 전 지연 시간</param>
    public static void TriggerBGMVolumeFade(int bgmIndex, float targetVolumeMultiplier, float duration, float delay = 0f)
        => OnBGMVolumeFade?.Invoke(bgmIndex, targetVolumeMultiplier, duration, delay);
}