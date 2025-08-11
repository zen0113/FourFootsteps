using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomEventObject : EventObject
{
    [Header("Random Dialogue Settings")]
    [SerializeField]
    private List<string> randomDialogueIds = new List<string>();

    // 플레이어가 오브젝트 범위 내에 있는지 확인하는 변수
    private bool _isPlayerInRange = false;

    protected new void Update()
    {
        // 다이얼로그 진행 중일 때는 조사 금지
        if (DialogueManager.Instance.isDialogueActive)
            return;

        // 회상 씬에서 조사 불가능하면 리턴
        if (!CanInteractInRecallScene())
            return;

        // 조사 조건: 플레이어가 범위 내에 있고 E키를 눌렀을 때
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

    // 회상 씬에서 조사 가능한지 확인하는 메서드
    private bool CanInteractInRecallScene()
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

    // 플레이어가 트리거에 들어왔을 때
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // 회상 씬에서 조사 불가능하면 리턴
            if (!CanInteractInRecallScene())
                return;

            if (gameObject.GetComponent<SpriteGlow.SpriteGlowEffect>() != null)
                spriteGlowEffect.enabled = true;

            _isPlayerInRange = true; // 플레이어가 범위에 들어옴
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // 회상 씬에서 조사 불가능하면 리턴
            if (!CanInteractInRecallScene())
            {
                // 조사 불가능한 상태가 되면 상호작용 해제
                if (gameObject.GetComponent<SpriteGlow.SpriteGlowEffect>() != null)
                    spriteGlowEffect.enabled = false;
                _isPlayerInRange = false;
                return;
            }

            if (gameObject.GetComponent<SpriteGlow.SpriteGlowEffect>() != null)
                spriteGlowEffect.enabled = true;

            _isPlayerInRange = true;
        }
    }

    // 플레이어가 트리거에서 나갔을 때
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (gameObject.GetComponent<SpriteGlow.SpriteGlowEffect>() != null)
                spriteGlowEffect.enabled = false;

            _isPlayerInRange = false; // 플레이어가 범위에서 벗어남
        }
    }
}
