using System.Collections;
using UnityEngine;
using System;

public class AnimationLooper : MonoBehaviour
{
    [Header("애니메이션 설정")]
    [SerializeField] private string animationParameterName = "PlayAnimation"; // 애니메이션 파라미터 이름
    [SerializeField] private bool startOnAwake = true; // 시작 시 자동 재생
    [SerializeField] private float loopDelay = 0f; // 루프 간 딜레이 (초)

    [Header("오디오 설정")]
    [SerializeField] private AudioClip loopSound; // 루프할 때 재생할 사운드
    [SerializeField] private bool playAudioWithAnimation = true; // 애니메이션과 함께 오디오 재생
    [SerializeField] private float audioInterval = 1f; // 오디오 재생 간격

    private Animator animator;
    private AudioSource audioSource;
    private bool isLooping = false;
    private float lastAudioTime;

    // 이벤트
    public Action OnLoopStart; // 루프 시작 시 호출
    public Action OnLoopEnd; // 루프 종료 시 호출

    private void Awake()
    {
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            // AudioSource 추가 시 기본 설정
            audioSource.playOnAwake = false; // 시작 시 자동 재생하지 않음
            audioSource.loop = false; // 루프하지 않음 (PlayOneShot 사용하므로)
        }

        // loopSound가 할당되어 있다면 AudioSource의 clip으로 설정 (선택 사항)
        if (loopSound != null)
        {
            audioSource.clip = loopSound;
        }

        // Animator가 없으면 경고
        if (animator == null)
        {
            Debug.LogWarning($"{gameObject.name}에 Animator 컴포넌트가 없습니다!");
        }
    }

    private void Start()
    {
        if (startOnAwake)
        {
            StartAnimationLoop();
        }
    }

    private void Update()
    {
        // 오디오 재생 처리
        if (isLooping && playAudioWithAnimation && loopSound != null)
        {
            if (Time.time - lastAudioTime >= audioInterval)
            {
                PlayLoopSound();
                lastAudioTime = Time.time;
            }
        }
    }

    /// <summary>
    /// 애니메이션 무한 루프 시작
    /// </summary>
    public void StartAnimationLoop()
    {
        if (animator == null) return;

        isLooping = true;
        lastAudioTime = Time.time;

        // 애니메이션 파라미터 설정 (Bool 타입 가정)
        animator.SetBool(animationParameterName, true);

        OnLoopStart?.Invoke();

        // 첫 번째 오디오 재생
        if (playAudioWithAnimation)
        {
            PlayLoopSound();
        }

        // 딜레이가 있다면 코루틴으로 처리
        if (loopDelay > 0)
        {
            StartCoroutine(DelayedLoopCoroutine());
        }
    }

    /// <summary>
    /// 애니메이션 무한 루프 중지
    /// </summary>
    public void StopAnimationLoop()
    {
        if (animator == null) return;

        isLooping = false;

        // 애니메이션 파라미터 해제
        animator.SetBool(animationParameterName, false);

        // 오디오 중지 (PlayOneShot이므로 재생 중이 아닐 수 있지만 안전을 위해)
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        // 딜레이 코루틴 중지
        StopAllCoroutines();

        OnLoopEnd?.Invoke();
    }

    /// <summary>
    /// 애니메이션 루프 토글
    /// </summary>
    public void ToggleAnimationLoop()
    {
        if (isLooping)
        {
            StopAnimationLoop();
        }
        else
        {
            StartAnimationLoop();
        }
    }

    /// <summary>
    /// 특정 애니메이션으로 루프 변경
    /// </summary>
    /// <param name="newAnimationParameter">새로운 애니메이션 파라미터 이름</param>
    public void ChangeLoopAnimation(string newAnimationParameter)
    {
        bool wasLooping = isLooping;

        if (isLooping)
        {
            StopAnimationLoop();
        }

        animationParameterName = newAnimationParameter;

        if (wasLooping)
        {
            StartAnimationLoop();
        }
    }

    /// <summary>
    /// 루프 딜레이 설정
    /// </summary>
    /// <param name="delay">딜레이 시간 (초)</param>
    public void SetLoopDelay(float delay)
    {
        loopDelay = Mathf.Max(0, delay);

        // 현재 루핑 중이고 딜레이가 설정되었다면 코루틴 재시작
        if (isLooping && loopDelay > 0)
        {
            StopAllCoroutines();
            StartCoroutine(DelayedLoopCoroutine());
        }
    }

    /// <summary>
    /// 오디오 간격 설정
    /// </summary>
    /// <param name="interval">오디오 재생 간격 (초)</param>
    public void SetAudioInterval(float interval)
    {
        audioInterval = Mathf.Max(0.1f, interval);
    }

    private void PlayLoopSound()
    {
        if (audioSource == null)
        {
            Debug.LogWarning("AudioSource가 없습니다. 오디오를 재생할 수 없습니다.");
            return;
        }

        if (loopSound == null)
        {
            Debug.LogWarning("Loop Sound AudioClip이 할당되지 않았습니다. 오디오를 재생할 수 없습니다.");
            return;
        }

        audioSource.PlayOneShot(loopSound);
    }

    private IEnumerator DelayedLoopCoroutine()
    {
        while (isLooping && loopDelay > 0)
        {
            yield return new WaitForSeconds(loopDelay);

            if (isLooping && animator != null)
            {
                // 딜레이 후 애니메이션 다시 트리거 (필요한 경우)
                animator.SetBool(animationParameterName, false);
                yield return null; // 한 프레임 대기하여 애니메이션 파라미터 업데이트 보장
                animator.SetBool(animationParameterName, true);
            }
        }
    }

    /// <summary>
    /// 현재 루핑 상태 확인
    /// </summary>
    public bool IsLooping => isLooping;

    /// <summary>
    /// 현재 애니메이션 파라미터 이름 가져오기
    /// </summary>
    public string CurrentAnimationParameter => animationParameterName;

    // 에디터에서 테스트용 (개발 중에만 사용)
    [ContextMenu("Start Loop")]
    private void TestStartLoop()
    {
        StartAnimationLoop();
    }

    [ContextMenu("Stop Loop")]
    private void TestStopLoop()
    {
        StopAnimationLoop();
    }

    [ContextMenu("Toggle Loop")]
    private void TestToggleLoop()
    {
        ToggleAnimationLoop();
    }
}