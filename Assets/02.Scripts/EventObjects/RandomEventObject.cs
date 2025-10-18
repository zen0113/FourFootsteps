using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomEventObject : EventObject
{
    [Header("Random Dialogue Settings")]
    [SerializeField]
    private List<string> randomDialogueIds = new List<string>();


    protected new void Update() 
    {
        // 다이얼로그 진행 중일 때는 조사 금지
        if (DialogueManager.Instance.isDialogueActive)
            return;

        // 회상 씬에서 조사 불가능하면 리턴
        if (!CanInteractInRecallScene())
            return;

        if (randomDialogueIds.Count > 0
            && EventManager.Instance
            && _isPlayerInRange 
            && Input.GetKeyDown(KeyCode.E))
        {
            TriggerRandomDialogue();
        }
    }

    // 랜덤 다이얼로그 실행
    private void TriggerRandomDialogue()
    {
        if (randomDialogueIds.Count == 0) return;

        // 랜덤으로 다이얼로그 선택
        int randomIndex = Random.Range(0, randomDialogueIds.Count);
        string selectedDialogueId = randomDialogueIds[randomIndex];

        // 이벤트 실행
        EventManager.Instance.CallEvent(selectedDialogueId);

        Debug.Log($"Random Dialogue Triggered: {selectedDialogueId}");
    }

    protected override bool CanInteractInRecallScene()
    {
        if (RecallManager.Instance == null)
            return true;

        return (bool)GameManager.Instance.GetVariable("CanInvesigatingRecallObject");
    }

    // 다이얼로그 ID 추가
    public void AddDialogueId(string dialogueId)
    {
        if (!randomDialogueIds.Contains(dialogueId))
        {
            randomDialogueIds.Add(dialogueId);
        }
    }

    // 다이얼로그 ID 제거
    public void RemoveDialogueId(string dialogueId)
    {
        randomDialogueIds.Remove(dialogueId);
    }

    // 모든 다이얼로그 ID 설정
    public void SetDialogueIds(List<string> dialogueIds)
    {
        randomDialogueIds = new List<string>(dialogueIds);
    }

}