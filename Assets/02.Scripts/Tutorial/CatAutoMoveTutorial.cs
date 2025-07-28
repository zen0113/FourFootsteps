using UnityEngine;

public class CatAutoMoveTutorial : TutorialBase
{
    [Header("이동 대상 설정")]
    [SerializeField] private CatAutoMover catMover;
    [SerializeField] private Transform destinationPoint;

    public override void Enter()
    {
        if (catMover != null && destinationPoint != null)
        {
            catMover.OnArrived += HandleArrival; // 도착 이벤트 등록
            catMover.StartMoving(destinationPoint);
        }
        else
        {
            Debug.LogError("[CatAutoMoveTutorial] 고양이 또는 목적지가 할당되지 않았습니다.");
        }
    }

    private void HandleArrival()
    {
        Debug.Log("[CatAutoMoveTutorial] 고양이 도착 완료, 다음 튜토리얼로 진행합니다.");
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
        if (catMover != null)
            catMover.OnArrived -= HandleArrival;
    }
}
