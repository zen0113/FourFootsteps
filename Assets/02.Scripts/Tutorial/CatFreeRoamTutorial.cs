using UnityEngine;

public class CatFreeRoamTutorial : TutorialBase
{
    [Header("이동 대상 설정")]
    [SerializeField] private CatFreeRoam catFreeRoam;
    [SerializeField] private Transform destinationPoint;
    [SerializeField] private float arrivalDistance = 2.0f; // 도착 판정 거리를 더 크게 설정

    private bool hasArrived = false;
    private bool isMovingStarted = false;

    public override void Enter()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetVariable("CanMoving", false);
        }
        else
        {
            Debug.LogWarning("[CatFreeRoamTutorial] GameManager.Instance가 null입니다. 플레이어 이동을 제한할 수 없습니다.");
        }

        if (catFreeRoam != null && destinationPoint != null)
        {
            // 자동 배회를 멈추고 특정 지점으로 이동
            catFreeRoam.StopRoaming();
            catFreeRoam.OnStopMoving += HandleStopMoving; // 이동 완료 이벤트 등록
            catFreeRoam.OnStartMoving += HandleStartMoving; // 이동 시작 이벤트 등록

            hasArrived = false;
            isMovingStarted = false;

            catFreeRoam.MoveToPosition(destinationPoint.position);

            // MoveToPosition 호출 후 즉시 이동 시작으로 간주
            isMovingStarted = true;
        }
        else
        {
            Debug.LogError("[CatFreeRoamTutorial] 고양이 또는 목적지가 할당되지 않았습니다.");
        }
    }

    private void HandleStartMoving()
    {
        isMovingStarted = true;
    }

    private void HandleStopMoving()
    {
        Debug.Log("[CatFreeRoamTutorial] 고양이 이동 멈춤 이벤트 발생");

        if (hasArrived) return; // 중복 호출 방지

        // 이동이 시작된 후에만 도착 체크
        if (isMovingStarted)
        {
            CheckArrival();
        }
    }

    private void CheckArrival()
    {
        if (hasArrived) return;

        float distanceToDestination = Vector2.Distance(catFreeRoam.transform.position, destinationPoint.position);

        if (distanceToDestination <= arrivalDistance)
        {
            hasArrived = true;

            TutorialController controller = FindObjectOfType<TutorialController>();
            if (controller != null)
            {
                controller.SetNextTutorial();
            }
            else
            {
                Debug.LogError("[CatFreeRoamTutorial] TutorialController를 찾을 수 없습니다!");
            }
        }
    }

    public override void Execute(TutorialController controller)
    {
        // 매 프레임 상태 체크 및 디버깅
        if (catFreeRoam != null && destinationPoint != null && !hasArrived)
        {
            float distanceToDestination = Vector2.Distance(catFreeRoam.transform.position, destinationPoint.position);

            // 고양이 상태 확인
            var currentState = catFreeRoam.GetCurrentState();

            if (distanceToDestination <= arrivalDistance)
            {
                CheckArrival();
            }
        }
    }

    public override void Exit()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetVariable("CanMoving", true);
        }
        else
        {
            Debug.LogWarning("[CatFreeRoamTutorial] GameManager.Instance가 null입니다. 플레이어 이동 제한을 해제할 수 없습니다.");
        }

        Debug.Log("[CatFreeRoamTutorial] Exit 호출됨");

        // 콜백 해제 (중복 방지)
        if (catFreeRoam != null)
        {
            catFreeRoam.OnStopMoving -= HandleStopMoving;
            catFreeRoam.OnStartMoving -= HandleStartMoving;
        }

        hasArrived = false;
        isMovingStarted = false;
    }
}