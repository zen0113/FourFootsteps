using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectSfxController : MonoBehaviour
{
    [Header("Defaults")]
    [SerializeField] private bool playOnStart = false;
    [SerializeField] private AudioClip defaultClip;
    [Range(0f, 1f)] [SerializeField] private float defaultVolume = 1f;
    [SerializeField] private float defaultPitch = 1f;

    public bool IsPlaying => audioSource.isPlaying;
    public AudioClip CurrentClip => audioSource.clip;

    [SerializeField] AudioSource audioSource;
    Coroutine fadeCoroutine;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = 0f;
    }

    void Start()
    {
        if (playOnStart && defaultClip != null)
            PlayLoop(defaultClip, defaultVolume, defaultPitch, fadeIn: 0.2f);
    }
    
    public void StartPlayLoopByDefault()
    {
        PlayLoop(defaultClip, defaultVolume, defaultPitch, fadeIn: 0.2f);
    }

    /// <summary>
    /// 루프 시작(혹은 교체). 이미 같은 클립이 재생 중이면 볼륨/피치만 갱신
    /// </summary>
    public void PlayLoop(AudioClip clip, float volume = 1f, float pitch=1f, float fadeIn=0f,
        float startTime =0f, bool restartIfSame = false)
    {
        if (clip == null) return;

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);

        if (audioSource.isPlaying && audioSource.clip == clip && !restartIfSame)
        {
            // 같은 클립: 파라미터만 업데이트+ 필요 시 볼륨 램프
            audioSource.pitch = pitch;
            if (fadeIn > 0f) fadeCoroutine = StartCoroutine(FadeTo(volume, fadeIn));
            else audioSource.volume = volume;
            return;
        }

        // 다른 클립으로 교체(크로스페이드)
        if(audioSource.isPlaying && audioSource != clip&& fadeIn > 0f)
        {
            fadeCoroutine = StartCoroutine(Crossfade(clip, volume, pitch, fadeIn));
            return;
        }

        // 새로 시작
        audioSource.clip = clip;
        audioSource.time = Mathf.Clamp(startTime, 0f, clip.length - 0.01f);
        audioSource.pitch = pitch;
        audioSource.volume = (fadeIn > 0f) ? 0f : volume;
        audioSource.Play();

        if(fadeIn > 0f)
            fadeCoroutine = StartCoroutine(FadeTo(volume, fadeIn));
    }

    /// <summary>
    /// 루프 정지(페이드 아웃)
    /// </summary>
    public void StopLoop(float fadeOut = 0.15f)
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        if (!audioSource.isPlaying) return;

        if (fadeOut > 0f) fadeCoroutine = StartCoroutine(FadeOutAndStop(fadeOut));
        else audioSource.Stop();
    }

    /// <summary>
    /// 일시정지/재개 (선형 페이드 지원)
    /// </summary>
    public void Pause(float fadeOut = 0.1f)
    {
        if (!audioSource.isPlaying) return;
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        if (fadeOut > 0f) fadeCoroutine = StartCoroutine(FadeOutAndPause(fadeOut));
        else audioSource.Pause();
    }

    public void Resume(float fadeIn = 0.1f, float targetVolume = -1f)
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        if (targetVolume < 0f) targetVolume = defaultVolume > 0f ? defaultVolume : 1f;

        if (audioSource.clip == null) return;
        audioSource.UnPause();
        if (fadeIn > 0f)
        {
            float start = audioSource.volume;
            fadeCoroutine = StartCoroutine(FadeTo(targetVolume, fadeIn, start));
        }
        else audioSource.volume = targetVolume;
    }

    /// <summary>
    /// 실시간 파라미터 조정(볼륨/피치). 볼륨은 램프 가능.
    /// </summary>
    public void SetVolume(float volume, float ramp = 0f)
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        if (ramp > 0f) fadeCoroutine = StartCoroutine(FadeTo(volume, ramp));
        else audioSource.volume = volume;
    }
    public void SetPitch(float pitch) => audioSource.pitch = pitch;

    // ---------- 내부 유틸 ----------
    IEnumerator FadeTo(float target, float duration, float? startOverride = null)
    {
        float t = 0f;
        float start = startOverride ?? audioSource.volume;
        while (t < duration)
        {
            //t += Time.unscaledDeltaTime; // 시간정지 시에도 동작 원하면 unscaled 사용
            t += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(start, target, t / duration);
            yield return null;
        }
        audioSource.volume = target;
        fadeCoroutine = null;
    }

    IEnumerator FadeOutAndStop(float duration)
    {
        float start = audioSource.volume;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(start, 0f, t / duration);
            yield return null;
        }
        audioSource.Stop();
        fadeCoroutine = null;
    }

    IEnumerator FadeOutAndPause(float duration)
    {
        float start = audioSource.volume;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(start, 0f, t / duration);
            yield return null;
        }
        audioSource.Pause();
        fadeCoroutine = null;
    }

    IEnumerator Crossfade(AudioClip next, float nextVol, float nextPitch, float duration)
    {
        // 보이스 하나로도 “pseudo 크로스페이드”: 앞부분은 줄이고 교체 후 올리기
        float half = duration * 0.5f;
        yield return FadeTo(0f, half);
        audioSource.Stop();
        audioSource.clip = next;
        audioSource.pitch = nextPitch;
        audioSource.volume = 0f;
        audioSource.Play();
        yield return FadeTo(nextVol, half);
        fadeCoroutine = null;
    }
}
