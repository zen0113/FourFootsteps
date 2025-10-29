using UnityEngine;
using System.Collections;

/// <summary>
/// 화면 암전, 효과음, 오브젝트 상태 변경, 플레이어 위치 이동 등
/// 복합적인 연출을 하나의 튜토리얼 단계로 실행하는 스크립트입니다.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class SceneTransitionTutorial : TutorialBase
{
    [Header("연출 설정")]
    [SerializeField] private float fadeOutDuration = 1.5f; // 화면이 어두워지는 속도
    [SerializeField] private float fadeInDuration = 1.5f;  // 화면이 다시 밝아지는 속도

    [Header("효과음 설정")]
    [SerializeField] private AudioClip transitionSound; // 암전 중에 재생할 효과음
    [Tooltip("효과음 재생 후 대기할 시간(초)입니다. 0으로 설정하면 효과음의 전체 길이만큼 자동으로 대기합니다.")]
    [SerializeField] private float soundWaitDuration = 0f;

    [Header("오브젝트 상태 변경")]
    [SerializeField] private GameObject[] objectsToDeactivate; // 비활성화할 오브젝트 목록
    [SerializeField] private GameObject[] objectsToActivate;   // 활성화할 오브젝트 목록

    [Header("플레이어 위치 변경")]
    [SerializeField] private Transform newPlayerPosition; // 플레이어가 이동할 위치 (Transform)

    private bool isCompleted = false;
    private PlayerCatMovement playerMovement;
    private AudioSource audioSource;

    public override void Enter()
    {
        Debug.Log("[SceneTransitionTutorial] 연출 시퀀스 시작.");
        isCompleted = false;

        // AudioSource 컴포넌트를 가져옵니다.
        audioSource = GetComponent<AudioSource>();
        // 씬이 시작될 때 자동으로 재생되지 않도록 설정
        audioSource.playOnAwake = false;
        // 2D 사운드로 설정
        audioSource.spatialBlend = 0;

        playerMovement = FindObjectOfType<PlayerCatMovement>();

        if (playerMovement != null)
        {
            playerMovement.SetMiniGameInputBlocked(true);
            Debug.Log("[SceneTransitionTutorial] Player input blocked.");
        }
        else
        {
            Debug.LogError("PlayerCatMovement component not found in the scene!");
        }

        StartCoroutine(TransitionSequence());
    }

    private IEnumerator TransitionSequence()
    {
        // --- 1. 화면 페이드 아웃 ---
        if (UIManager.Instance != null)
        {
            yield return UIManager.Instance.OnFade(null, 0, 1, fadeOutDuration);
        }

        // --- 2. 효과음 재생 및 대기 ---
        if (transitionSound != null)
        {
            audioSource.clip = transitionSound;
            audioSource.Play();

            float waitTime = soundWaitDuration > 0 ? soundWaitDuration : transitionSound.length;
            yield return new WaitForSeconds(waitTime);
        }

        // --- 3. 오브젝트 활성/비활성화 ---
        if (objectsToDeactivate != null)
        {
            foreach (var obj in objectsToDeactivate)
            {
                if (obj != null) obj.SetActive(false);
            }
        }
        if (objectsToActivate != null)
        {
            foreach (var obj in objectsToActivate)
            {
                if (obj != null) obj.SetActive(true);
            }
        }

        // --- 4. 플레이어 위치 이동 ---
        if (newPlayerPosition != null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                player.transform.position = newPlayerPosition.position;
                player.transform.rotation = newPlayerPosition.rotation;
            }
        }

        // --- 5. 화면 페이드 인 ---
        if (UIManager.Instance != null)
        {
            yield return UIManager.Instance.OnFade(null, 1, 0, fadeInDuration);
        }

        // --- 6. 완료 처리 ---

        if (playerMovement != null)
        {
            playerMovement.SetMiniGameInputBlocked(false);
            Debug.Log("[SceneTransitionTutorial] Player input re-enabled.");
        }

        Debug.Log("[SceneTransitionTutorial] 연출 시퀀스 완료.");
        isCompleted = true;
    }

    public override void Execute(TutorialController controller)
    {
        if (isCompleted)
        {
            controller.SetNextTutorial();
        }
    }

    public override void Exit()
    {
        if (playerMovement != null)
        {
            playerMovement.SetMiniGameInputBlocked(false);
        }

        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }
}