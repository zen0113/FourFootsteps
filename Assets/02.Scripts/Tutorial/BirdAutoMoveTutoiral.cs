using UnityEngine;

public class BirdAutoMoveTutorial : TutorialBase
{
    [Header("이동 대상 설정")]
    [SerializeField] private BirdAutoMover birdMover;
    [SerializeField] private Transform destinationPoint;
    [SerializeField] private bool IsInfiniteFlying = false;

    [Header("튜토리얼 진행 조건")]
    [Tooltip("체크하면 새의 이동이 끝난 후 다음 튜토리얼로 넘어갑니다.")]
    [SerializeField] private bool waitForMovementEnd = false; 

    public override void Enter()
    {
        if (birdMover != null && destinationPoint != null)
        {
            if (waitForMovementEnd)
            {
                // 이동이 끝나야 다음으로 진행
                birdMover.OnMovementFinished += HandleMovementEnd;
            }
            else
            {
                // 이동이 시작되면 바로 다음으로 진행 (기본값)
                birdMover.OnMovementStarted += HandleMovementStart;
            }

            birdMover.StartMoving(destinationPoint);
            birdMover.isInfiniteFlying = IsInfiniteFlying;
        }
        else
        {
            Debug.LogError("[BirdAutoMoveTutorial] 새 또는 목적지가 할당되지 않았습니다.");
        }
    }

    // 이동이 시작될 때 호출되는 함수
    private void HandleMovementStart()
    {
        Debug.Log("[BirdAutoMoveTutorial] 새가 이동을 시작했습니다. 다음 튜토리얼로 진행합니다.");
        ProceedToNextTutorial();
    }

    private void HandleMovementEnd()
    {
        Debug.Log("[BirdAutoMoveTutorial] 새가 이동을 마쳤습니다. 다음 튜토리얼로 진행합니다.");
        ProceedToNextTutorial();
    }

    private void ProceedToNextTutorial()
    {
        TutorialController controller = FindObjectOfType<TutorialController>();
        controller?.SetNextTutorial();
    }

    public override void Execute(TutorialController controller)
    {
        // 이 튜토리얼은 이벤트 기반으로 동작하므로 Execute는 비워둡니다.
    }

    public override void Exit()
    {
        if (birdMover != null)
        {
            birdMover.OnMovementStarted -= HandleMovementStart;
            birdMover.OnMovementFinished -= HandleMovementEnd;
        }
    }
}