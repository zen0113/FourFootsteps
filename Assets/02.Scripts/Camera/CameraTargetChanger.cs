using UnityEngine;

public class CameraTargetChanger : TutorialBase
{
    [Header("카메라 타겟 설정")]
    public Transform newTarget;

    [Header("카메라 오프셋 설정")]
    public bool changeOffsetY = false;
    public float newOffsetY = 2f;

    [Header("참조")]
    public FollowCamera followCamera;

    public override void Enter()
    {
        // followCamera가 인스펙터에서 할당되지 않았다면 메인 카메라에서 찾습니다.
        if (followCamera == null)
        {
            followCamera = Camera.main.GetComponent<FollowCamera>();
        }

        if (followCamera == null)
        {
            Debug.LogError("[CameraTargetChanger] FollowCamera를 찾을 수 없습니다. 메인 카메라에 FollowCamera 컴포넌트가 있는지 확인해주세요.");
            return;
        }

        // 타겟 변경
        if (newTarget != null)
        {
            Debug.Log($"[CameraTargetChanger] 카메라 타겟을 '{newTarget.name}'(으)로 변경합니다.");
            followCamera.target = newTarget;
        }

        // Y축 오프셋 변경 (changeOffsetY가 true인 경우에만 실행)
        if (changeOffsetY)
        {
            Debug.Log($"[CameraTargetChanger] 카메라 Y 오프셋을 '{newOffsetY}'(으)로 변경합니다.");
            Vector2 newOffset = followCamera.offset;
            newOffset.y = newOffsetY;
            followCamera.offset = newOffset;
        }

        TutorialController tutorialController = FindObjectOfType<TutorialController>();
        if (tutorialController != null)
        {
            tutorialController.SetNextTutorial();
        }
        else
        {
            Debug.LogWarning("[CameraTargetChanger] TutorialController를 찾을 수 없어 다음 튜토리얼로 진행할 수 없습니다.");
        }
    }


    public override void Execute(TutorialController controller)
    {
        
    }


    public override void Exit()
    {
        
    }
}