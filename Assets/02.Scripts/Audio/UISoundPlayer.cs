using UnityEngine;

public class UISoundPlayer : MonoBehaviour
{
    private AudioSource audioSource;
    public AudioSource AudioSource => audioSource; // 외부에서 AudioSource 접근 가능하도록

    // masterSFXVolume을 내부적으로 유지하여 Play 호출 시 볼륨 계산에 활용
    private float masterVolumeMultiplier = 1f;

    public void Initialize(AudioSource source, float initialMasterVolume)
    {
        if (audioSource == null)
        {
            audioSource = source;
            audioSource.playOnAwake = false;
            audioSource.loop = false; // 기본적으로 단발성 UI 사운드는 루프가 아님
            masterVolumeMultiplier = initialMasterVolume; // 초기 마스터 볼륨 저장
            audioSource.volume = masterVolumeMultiplier; // 초기 볼륨 설정
        }
        else
        {
            // 이미 초기화된 경우, 마스터 볼륨만 업데이트
            UpdateVolume(initialMasterVolume);
        }
    }

    /// <summary>
    /// 사운드를 재생합니다.
    /// </summary>
    /// <param name="clip">재생할 AudioClip</param>
    /// <param name="volumeMultiplier">클립에 적용할 상대적 볼륨 배율 (0.0 ~ 1.0). masterVolumeMultiplier와 곱해집니다.</param>
    /// <param name="panStereo">스테레오 패닝 (-1: 왼쪽, 0: 중앙, 1: 오른쪽)</param>
    public void Play(AudioClip clip, float volumeMultiplier = 1f, float panStereo = 0f) // volume 매개변수를 volumeMultiplier로 변경하고 기본값 1f 추가
    {
        if (audioSource == null)
        {
            Debug.LogError("UISoundPlayer not initialized. Cannot play sound.", this); // 어떤 오브젝트에서 발생했는지 알 수 있도록 this 추가
            return;
        }
        if (clip == null) // 클립이 null인 경우도 확인하는 것이 좋습니다.
        {
            Debug.LogWarning("Attempted to play a null AudioClip on UISoundPlayer.", this);
            return;
        }

        audioSource.clip = clip;
        audioSource.volume = masterVolumeMultiplier * volumeMultiplier; // 마스터 볼륨과 개별 볼륨 배율을 곱함
        audioSource.panStereo = panStereo;
        audioSource.Play();
    }

    /// <summary>
    /// 현재 재생 중인 사운드를 정지합니다.
    /// </summary>
    public void Stop()
    {
        if (audioSource != null && audioSource.isPlaying) // 재생 중일 때만 정지하도록 조건 추가 (불필요한 Stop 호출 방지)
        {
            audioSource.Stop();
            audioSource.clip = null; // 클립 해제
        }
    }

    /// <summary>
    /// 현재 재생 중인 사운드를 일시 정지합니다.
    /// </summary>
    public void Pause()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Pause();
        }
    }

    /// <summary>
    /// 일시 정지된 사운드를 다시 재생합니다.
    /// </summary>
    public void Resume()
    {
        // Clip이 null이 아니고, 재생 중이 아닐 때만 UnPause
        if (audioSource != null && audioSource.clip != null && !audioSource.isPlaying)
        {
            audioSource.UnPause();
        }
    }

    /// <summary>
    /// 사운드가 재생 중인지 확인합니다.
    /// </summary>
    public bool IsPlaying()
    {
        return audioSource != null && audioSource.isPlaying;
    }

    /// <summary>
    /// 특정 클립이 재생 중인지 확인합니다.
    /// </summary>
    public bool IsPlaying(AudioClip clip)
    {
        return audioSource != null && audioSource.isPlaying && audioSource.clip == clip;
    }

    /// <summary>
    /// UISoundPlayer의 마스터 볼륨을 업데이트합니다.
    /// </summary>
    /// <param name="newMasterVolume">새로운 마스터 볼륨</param>
    public void UpdateVolume(float newMasterVolume)
    {
        if (audioSource != null)
        {
            masterVolumeMultiplier = newMasterVolume; // 마스터 볼륨 업데이트

            // 현재 재생 중이 아니어도 기본 볼륨은 설정해야 함
            audioSource.volume = masterVolumeMultiplier;
        }
    }

}