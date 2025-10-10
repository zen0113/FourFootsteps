using UnityEngine;
using System.Collections;

/// <summary>
/// 모든 고양이가 설득되었는지 계속 확인하는 튜토리얼 단계입니다.
/// 조건이 충족되면 지정된 다이얼로그를 출력하고 다음 튜토리얼로 넘어갑니다.
/// </summary>
public class CheckAllCatsTutorial : TutorialBase
{
    [Header("설정")]
    [SerializeField]
    private string completionDialogueID = "탈출시작대사"; // 모든 고양이 설득 시 출력할 다이얼로그 ID

    private bool isConditionMet = false; // 조건 충족 여부를 확인하는 플래그

    public override void Enter()
    {
        Debug.Log("[Tutorial] 모든 고양이 설득 여부 확인 시작.");
        isConditionMet = false; // 튜토리얼 진입 시 플래그 초기화
    }

    // 매 프레임 실행되며 조건을 확인합니다.
    public override void Execute(TutorialController controller)
    {
        // 이미 조건이 충족되었다면 다시 확인하지 않습니다.
        if (isConditionMet)
        {
            return;
        }

        var gm = GameManager.Instance;

        // GameManager에서 각 고양이의 설득 상태(boolean) 변수를 가져옵니다.
        bool allPersuaded = (bool)gm.GetVariable("Ttoli_Persuaded") &&
                              (bool)gm.GetVariable("Leo_Persuaded") &&
                              (bool)gm.GetVariable("Bogsil_Persuaded") &&
                              (bool)gm.GetVariable("Miya_Persuaded");

        // 만약 모든 고양이가 설득되었다면
        if (allPersuaded)
        {
            Debug.Log("[Tutorial] 모든 고양이 설득 완료! 완료 시퀀스를 시작합니다.");
            isConditionMet = true; // 중복 실행을 막기 위해 플래그를 true로 설정
            StartCoroutine(CompletionSequence(controller));
        }
    }

    // 조건 충족 시 실행될 코루틴 (다이얼로그 재생 후 다음 단계로)
    private IEnumerator CompletionSequence(TutorialController controller)
    {
        // 설정된 완료 다이얼로그가 있다면 출력하고 끝날 때까지 기다립니다.
        if (!string.IsNullOrEmpty(completionDialogueID))
        {
            DialogueManager.Instance.StartDialogue(completionDialogueID);
            yield return new WaitUntil(() => !DialogueManager.Instance.isDialogueActive);
        }

        // 다음 튜토리얼 단계로 넘어갑니다.
        controller.SetNextTutorial();
    }

    public override void Exit()
    {
        Debug.Log("[Tutorial] 모든 고양이 설득 여부 확인 종료.");
    }
}