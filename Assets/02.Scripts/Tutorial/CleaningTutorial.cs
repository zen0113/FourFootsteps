using UnityEngine;

public class CleaningTutorial : TutorialBase
{
    [Header("튜토리얼 목표")]
    [Tooltip("이 목표 개수만큼 청소를 완료해야 다음으로 넘어갑니다.")]
    [SerializeField] private int requiredCleanCount = 3;

    private TutorialController tutorialController;
    private bool isCompleted = false;

    /// <summary>
    /// 튜토리얼이 시작될 때 호출됩니다.
    /// </summary>
    public override void Enter()
    {
        isCompleted = false;
        // 필요하다면 여기에 "오브젝트 3개를 청소하세요 (0/3)" 같은 UI를 띄우는 코드를 추가할 수 있습니다.
        Debug.Log($"청소 튜토리얼 시작! 목표: {requiredCleanCount}개");
    }

    /// <summary>
    /// 튜토리얼의 메인 로직을 실행합니다. 여기서는 Controller 참조만 저장합니다.
    /// </summary>
    public override void Execute(TutorialController controller)
    {
        tutorialController = controller;
    }

    /// <summary>
    /// 매 프레임 조건을 확인합니다.
    /// </summary>
    private void Update()
    {
        if (!isCompleted && tutorialController != null)
        {
            // GameManager의 GetVariable 함수로 값을 가져와 확인
            int currentCleanCount = (int)GameManager.Instance.GetVariable("CleanedObjectCount");
            if (currentCleanCount >= requiredCleanCount)
            {
                CompleteTutorial();
            }
        }
    }

    /// <summary>
    /// 튜토리얼을 완료하고 다음으로 넘어갑니다.
    /// </summary>
    private void CompleteTutorial()
    {
        isCompleted = true; // 중복 호출 방지
        Debug.Log("청소 튜토리얼 목표 달성!");
        tutorialController.SetNextTutorial();
    }

    /// <summary>
    /// 튜토리얼이 종료될 때 호출됩니다.
    /// </summary>
    public override void Exit()
    {
        // Enter에서 띄웠던 UI가 있다면 여기서 숨깁니다.
    }
}