using UnityEngine;

public class BirdAutoMoveTutorial : TutorialBase
{
    [Header("이동 대상 설정")]
    [SerializeField] private BirdAutoMover birdMover;
    [SerializeField] private Transform destinationPoint;

    public override void Enter()
    {
        if (birdMover != null && destinationPoint != null)
        {
            birdMover.OnMovementStarted += HandleMovementStart; // 이동 시작 이벤트 등록
            birdMover.StartMoving(destinationPoint);
        }
        else
        {
            Debug.LogError("[BirdAutoMoveTutorial] 새 또는 목적지가 할당되지 않았습니다.");
        }
    }

    private void HandleMovementStart()
    {
        Debug.Log("[BirdAutoMoveTutorial] 새가 이동을 시작했습니다. 다음 튜토리얼로 진행합니다.");
        TutorialController controller = FindObjectOfType<TutorialController>();
        controller?.SetNextTutorial();
    }

    public override void Execute(TutorialController controller)
    {
        // 이제 필요 없음
    }

    public override void Exit()
    {
        // 콜백 해제 (중복 방지)
        if (birdMover != null)
            birdMover.OnMovementStarted -= HandleMovementStart;
    }
}