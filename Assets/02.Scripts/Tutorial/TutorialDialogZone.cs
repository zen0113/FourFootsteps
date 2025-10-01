using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 설정한 콜라이더존에 들어왔을 시, 다이얼로그 1회 재생(기본)
public class TutorialDialogZone : TutorialBase
{
    [Header("대화 ID")]
    public string dialogueID;
    [SerializeField] private bool disableAfter = true;
    private bool isExecuted = false;

    private bool choiceSelected = false;
    private int selectedTutorialIndex = -1;
    private TutorialController tutorialController;

    public override void Enter()
    {
        if (string.IsNullOrEmpty(dialogueID))
        {
            Debug.LogWarning("[RecallDialog] dialogueID가 비어 있습니다.");
            return;
        }
        choiceSelected = false;
        selectedTutorialIndex = -1;
        tutorialController = FindObjectOfType<TutorialController>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isExecuted) return;
        if (!other.CompareTag("Player")) return;

        isExecuted = true;

        DialogueManager.Instance.StartDialogue(dialogueID);
        StartCoroutine(WaitForDialogueEnd());
    }


    public override void Execute(TutorialController controller)
    {
        // 실행 중 별도 로직 없음
    }

    public override void Exit()
    {
        StopAllCoroutines();
        if (disableAfter) gameObject.SetActive(false);
    }

    private IEnumerator WaitForDialogueEnd()
    {
        yield return new WaitUntil(() => DialogueManager.Instance.isDialogueActive);
        yield return new WaitUntil(() => !DialogueManager.Instance.isDialogueActive);

    }

    // DialogueManager에서 호출되는 메서드
    public void OnChoiceSelectedInTutorial(int tutorialIndex)
    {
        choiceSelected = true;
        selectedTutorialIndex = tutorialIndex;

        if (tutorialController == null)
            tutorialController = FindObjectOfType<TutorialController>();

        if (selectedTutorialIndex == -2)
        {
            Debug.Log("[RecallDialog] 선택지로 인해 튜토리얼 종료.");
            tutorialController?.CompletedAllTutorials();
        }
        else if (selectedTutorialIndex == -3)
        {
            Debug.Log("[RecallDialog] 선택지로 인해 튜토리얼 진행 중단. 현재 단계에서 대기.");
            // 아무 동작도 하지 않음 (튜토리얼 진행하지 않음)
        }
        else if (selectedTutorialIndex >= 0)
        {
            Debug.Log($"[RecallDialog] 선택지 선택됨. 튜토리얼 인덱스 {selectedTutorialIndex}로 점프.");
            tutorialController?.JumpToTutorial(selectedTutorialIndex);
        }
        else
        {
            Debug.Log("[RecallDialog] 선택지 선택됨. 다음 튜토리얼로 진행.");
            tutorialController?.SetNextTutorial();
        }
    }

}
