using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMoveTutorial : TutorialBase
{
    [SerializeField] private Transform target;
    [SerializeField] private FollowCamera followCamera;
    [SerializeField] private float targetValue = 0.01f;

    // 해당 튜토리얼 과정을 시작할 때 1회 호출
    public override void Enter()
    {
        followCamera = FindObjectOfType<FollowCamera>();
        StartCoroutine(SmoothChangeValueCoroutine(targetValue));
        followCamera.target = target;

        // 카메라 이동 동안 플레이어 입력 차단
        PlayerCatMovement.Instance.SetMiniGameInputBlocked(true);
    }

    // 해당 튜토리얼 과정을 진행하는 동안 매 프레임 호출

    public override void Execute(TutorialController controller)
    {
    }

    // 해당 튜토리얼 과정을 종료할 때 1회 호출

    public override void Exit()
    {
    }


    private IEnumerator SmoothChangeValueCoroutine(float targetValue)
    {
        float duration = 1.5f;

        followCamera.smoothSpeedX = targetValue;

        yield return new WaitForSeconds(duration);

        Debug.Log("Camera Move end");
        Debug.Log("SetNextTutorial() Executed");
        //  플레이어 입력 차단 해제
        PlayerCatMovement.Instance.SetMiniGameInputBlocked(false);
        TutorialController.Instance.SetNextTutorial();
    }
}
