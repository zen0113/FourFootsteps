using UnityEngine;

/// <summary>
/// [역할] 고양이가 목적지에 도착하면 차량을 왼쪽으로 출발시키는 튜토리얼 단계입니다.
/// </summary>
public class CatArrivalVehicleStartTutorial : TutorialBase
{
    #region Variables

    [Header("고양이 설정")]
    [SerializeField] private CatAutoMover catMover;
    [SerializeField] private Transform destinationPoint;

    [Header("차량 효과 연결")]
    [SerializeField] private TruckShakeEffect[] truckShakes;
    [SerializeField] private WheelRotation[] wheelRotations;

    // --- [새로 추가된 부분] ---
    [Header("트럭 이동 설정")]
    [Tooltip("실제로 움직일 트럭 오브젝트의 Transform을 연결해주세요.")]
    [SerializeField] private Transform truckTransform; // 움직일 트럭 오브젝트
    [Tooltip("트럭의 이동 속도를 설정합니다.")]
    [SerializeField] private float moveSpeed = 3f; // 트럭의 이동 속도
    // --- [여기까지] ---

    private bool isCompleted = false;
    private bool shouldTruckMove = false; // [새로 추가] 트럭 이동을 제어하는 플래그

    #endregion

    // --- [새로 추가된 부분] ---
    /// <summary>
    /// 매 프레임마다 호출됩니다.
    /// </summary>
    private void Update()
    {
        // 트럭을 움직여야 하는 상태이고, truckTransform이 할당되었다면
        if (shouldTruckMove && truckTransform != null)
        {
            // Vector3.left는 (-1, 0, 0) 방향입니다.
            // Time.deltaTime을 곱해 프레임 속도에 관계없이 부드럽게 이동시킵니다.
            truckTransform.Translate(Vector3.left * moveSpeed * Time.deltaTime);
        }
    }
    // --- [여기까지] ---

    public override void Enter()
    {
        isCompleted = false;
        shouldTruckMove = false; // [수정] 튜토리얼 시작 시 이동 플래그 초기화

        ToggleVehicleEffects(false);

        if (catMover != null && destinationPoint != null)
        {
            catMover.OnArrived += HandleCatArrival;
            catMover.StartMoving(destinationPoint);
        }
        else
        {
            Debug.LogError("[CatArrivalVehicleStartTutorial] 고양이 또는 목적지가 할당되지 않았습니다.");
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
        if (catMover != null)
        {
            catMover.OnArrived -= HandleCatArrival;
        }
    }

    private void HandleCatArrival()
    {
        Debug.Log("[CatArrivalVehicleStartTutorial] 고양이 도착! 차량을 출발시킵니다.");

        // 1. 차량 효과(바퀴 회전 등)를 모두 활성화합니다.
        ToggleVehicleEffects(true);

        // 2. 트럭 이동을 시작하도록 플래그를 true로 변경합니다. [수정]
        shouldTruckMove = true;

        // 3. 튜토리얼 완료 상태로 변경하여 다음 단계로 넘어갈 수 있게 합니다.
        isCompleted = true;
    }

    private void ToggleVehicleEffects(bool isEnabled)
    {
        if (truckShakes != null)
        {
            foreach (var shake in truckShakes)
                if (shake != null) shake.enabled = isEnabled;
        }

        if (wheelRotations != null)
        {
            foreach (var wheel in wheelRotations)
            {
                if (wheel != null)
                {
                    if (isEnabled) wheel.ResumeRotation();
                    else wheel.PauseRotation();
                }
            }
        }
    }
}