using UnityEngine;
using System.Collections;

/// <summary>
/// 화면 암전(Fade), 사운드 재생, 카메라 위치 및 타겟 변경을 포함한
/// 복합적인 연출을 실행하는 튜토리얼 스크립트입니다.
/// </summary>
public class CameraTransitionTutorial : TutorialBase
{
    [Header("연출 설정")]
    [SerializeField] private float fadeOutDuration = 1.5f;
    [SerializeField] private float fadeInDuration = 1.5f;

    [Header("효과음 설정")]
    [SerializeField] private AudioClip transitionSound;
    [Tooltip("효과음 재생 후 대기할 시간(초)입니다. 0으로 설정하면 효과음의 전체 길이만큼 자동으로 대기합니다.")]
    [SerializeField] private float soundWaitDuration = 0f;

    [Header("카메라 설정")]
    [Tooltip("연출을 제어할 FollowCamera 스크립트를 직접 연결해주세요.")]
    [SerializeField] private FollowCamera followCamera;
    [Tooltip("카메라가 이동할 목표 위치를 담은 Transform (X, Y 좌표만 사용됩니다)")]
    [SerializeField] private Transform newCameraPosition;
    [Tooltip("연출 후 카메라가 새로 추적할 타겟 Transform")]
    [SerializeField] private Transform newCameraTarget;

    [Header("오브젝트 상태 변경 (선택 사항)")]
    [SerializeField] private GameObject[] objectsToDeactivate;
    [SerializeField] private GameObject[] objectsToActivate;

    // --- 내부 변수 ---
    private bool isCompleted = false;
    private PlayerCatMovement playerMovement;
    private Camera mainCamera;
    private Transform originalCameraTarget;

    public override void Enter()
    {
        Debug.Log("[CameraTransitionTutorial] 연출 시퀀스 시작.");
        isCompleted = false;

        playerMovement = FindObjectOfType<PlayerCatMovement>();

        if (followCamera == null)
        {
            Debug.LogWarning("FollowCamera가 인스펙터에 할당되지 않았습니다. 메인 카메라에서 찾습니다.");
            followCamera = Camera.main.GetComponent<FollowCamera>();
        }

        if (followCamera == null)
        {
            Debug.LogError("FollowCamera 컴포넌트를 찾을 수 없습니다! 인스펙터에 직접 할당해주세요.");
            isCompleted = true;
            return;
        }

        mainCamera = followCamera.GetComponent<Camera>();
        originalCameraTarget = followCamera.target;

        if (playerMovement != null)
        {
            playerMovement.SetMiniGameInputBlocked(true);
            Debug.Log("[CameraTransitionTutorial] Player input blocked.");
        }

        StartCoroutine(TransitionSequence());
    }

    private IEnumerator TransitionSequence()
    {
        // --- 1. 화면 페이드 아웃 ---
        yield return UIManager.Instance.OnFade(null, 0, 1, fadeOutDuration);

        // --- 2. 연출 준비 (카메라 재설정 및 사운드 재생) ---
        followCamera.enabled = false;
        Debug.Log("FollowCamera 비활성화.");

        // --- ✨✨✨ 여기가 핵심 수정 부분입니다! ✨✨✨ ---
        if (newCameraPosition != null)
        {
            // 현재 카메라의 Z 위치를 저장합니다.
            float originalZ = mainCamera.transform.position.z;

            // newCameraPosition의 X, Y 위치와 원래 카메라의 Z 위치를 조합하여 새로운 위치를 만듭니다.
            Vector3 targetPosition = new Vector3(newCameraPosition.position.x, newCameraPosition.position.y, originalZ);

            // 계산된 최종 위치로 카메라를 "텔레포트"시킵니다.
            mainCamera.transform.position = targetPosition;

            // 2D 게임에서는 카메라 회전값을 바꾸면 안 되므로 LookAt 이나 rotation 변경 코드는 제거합니다.
            Debug.Log($"카메라 위치를 ({targetPosition.x}, {targetPosition.y})으로 이동 (Z축 유지).");
        }
        // --- ✨✨✨ 수정 끝 ✨✨✨ ---

        ToggleObjectsActivity();

        if (transitionSound != null)
        {
            AudioSource.PlayClipAtPoint(transitionSound, mainCamera.transform.position);
        }

        // --- 3. 사운드 재생 완료 대기 ---
        float waitTime = soundWaitDuration > 0 ? soundWaitDuration : (transitionSound != null ? transitionSound.length : 0);
        yield return new WaitForSeconds(waitTime);

        // --- 4. 화면 페이드 인 ---
        yield return UIManager.Instance.OnFade(null, 1, 0, fadeInDuration);

        // --- 5. 마무리 ---
        if (newCameraTarget != null)
        {
            followCamera.target = newCameraTarget;
            Debug.Log($"FollowCamera의 타겟을 {newCameraTarget.name}(으)로 변경.");
        }

        followCamera.enabled = true;
        Debug.Log("FollowCamera 다시 활성화.");

        if (playerMovement != null)
        {
            playerMovement.SetMiniGameInputBlocked(false);
            Debug.Log("[CameraTransitionTutorial] Player input re-enabled.");
        }

        Debug.Log("[CameraTransitionTutorial] 연출 시퀀스 완료.");
        isCompleted = true;
    }

    private void ToggleObjectsActivity()
    {
        if (objectsToDeactivate != null)
        {
            foreach (var obj in objectsToDeactivate) { if (obj != null) obj.SetActive(false); }
        }
        if (objectsToActivate != null)
        {
            foreach (var obj in objectsToActivate) { if (obj != null) obj.SetActive(true); }
        }
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
    }
}