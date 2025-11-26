using UnityEngine;
using System.Collections;

public class GeneratorMinigameTutorial : TutorialBase
{
    [Header("연결 컴포넌트")]
    [SerializeField] private GeneratorMinigameUI minigameUI;
    [SerializeField] private GeneratorInteraction generatorInteraction;

    [Header("시작 시 설정")]
    [SerializeField] private GameObject objectToActivateOnStart;

    [Header("다이얼로그 ID 설정")]
    [SerializeField] private string startDialogueID = "GENERATOR_START"; // 미니게임 시작 전 다이얼로그
    [SerializeField] private string successDialogueID = "GENERATOR_SUCCESS"; // 성공 시 다이얼로그
    [SerializeField] private string failureDialogueID = "GENERATOR_FAIL";   // 실패 시 다이얼로그

    [Header("성공 시 설정")]
    [SerializeField] private GameObject objectToDeactivateOnSuccess; // 성공 시 비활성화할 오브젝트

    private bool isCompleted = false;

    // 튜토리얼 단계 진입 시 호출
    public override void Enter()
    {
        Debug.Log("[GeneratorMinigameTutorial] 발전기 미니게임 튜토리얼 시작");
        isCompleted = false;

        if (objectToActivateOnStart != null)
        {
            objectToActivateOnStart.SetActive(true);
        }

        // 필수 컴포넌트 확인
        if (minigameUI == null || generatorInteraction == null)
        {
            Debug.LogError("[GeneratorMinigameTutorial] MinigameUI 또는 GeneratorInteraction이 할당되지 않았습니다!");
            return;
        }

        // 플레이어가 상호작용(E키)할 때까지 대기
        generatorInteraction.OnInteract += HandleInteraction;
    }

    // 플레이어가 E키를 눌렀을 때 호출될 함수
    private void HandleInteraction()
    {
        // 중복 호출을 막기 위해 이벤트 구독 해제
        generatorInteraction.OnInteract -= HandleInteraction;

        // 미니게임 전체 시퀀스(대사 -> 게임 -> 결과 대사) 시작
        StartCoroutine(MinigameSequence());
    }

    // 미니게임의 전체 흐름을 관리하는 코루틴
    private IEnumerator MinigameSequence()
    {
        // 1. 시작 대사 출력
        if (!string.IsNullOrEmpty(startDialogueID))
        {
            DialogueManager.Instance.StartDialogue(startDialogueID);
            // 대사가 끝날 때까지 대기
            yield return new WaitUntil(() => !DialogueManager.Instance.isDialogueActive);
        }

        // 2. 미니게임 시작
        // 미니게임이 끝나면 HandleMinigameComplete 함수를 호출하도록 설정
        minigameUI.StartMinigame(HandleMinigameComplete);
    }

    // 미니게임 완료 시 호출될 콜백 함수
    private void HandleMinigameComplete(bool success)
    {
        if (success)
        {
            // 성공 시퀀스 시작
            StartCoroutine(SuccessSequence());
        }
        else
        {
            // 실패 시퀀스 시작
            StartCoroutine(FailureSequence());
        }
    }

    // 성공 시 처리 (성공 대사 -> 오브젝트 비활성화 -> 튜토리얼 완료)
    private IEnumerator SuccessSequence()
    {
        // 성공 대사 출력
        if (!string.IsNullOrEmpty(successDialogueID))
        {
            DialogueManager.Instance.StartDialogue(successDialogueID);
            yield return new WaitUntil(() => !DialogueManager.Instance.isDialogueActive);
        }

        // 지정된 오브젝트 비활성화
        if (objectToDeactivateOnSuccess != null)
        {
            objectToDeactivateOnSuccess.SetActive(false);
            Debug.Log($"[GeneratorMinigameTutorial] 성공! '{objectToDeactivateOnSuccess.name}' 오브젝트를 비활성화합니다.");
        }

        // 튜토리얼 완료 상태로 변경
        isCompleted = true;
    }

    // 실패 시 처리 (실패 대사 -> 재시도 가능하도록 설정)
    private IEnumerator FailureSequence()
    {
        // 실패 대사 출력
        if (!string.IsNullOrEmpty(failureDialogueID))
        {
            DialogueManager.Instance.StartDialogue(failureDialogueID);
            yield return new WaitUntil(() => !DialogueManager.Instance.isDialogueActive);
        }

        // 플레이어가 다시 상호작용(E키)할 수 있도록 이벤트를 다시 구독
        Debug.Log("[GeneratorMinigameTutorial] 실패! 다시 시도할 수 있습니다.");
        generatorInteraction.OnInteract += HandleInteraction;
    }


    // 매 프레임 튜토리얼의 상태를 확인
    public override void Execute(TutorialController controller)
    {
        // isCompleted가 true가 되면 다음 튜토리얼로 진행
        if (isCompleted)
        {
            controller.SetNextTutorial();
        }
    }

    // 튜토리얼 단계 종료 시 호출
    public override void Exit()
    {
        if (objectToActivateOnStart != null)
        {
            objectToActivateOnStart.SetActive(false);
        }

        // 안전하게 이벤트 구독 해제
        if (generatorInteraction != null)
        {
            generatorInteraction.OnInteract -= HandleInteraction;
        }
        Debug.Log("[GeneratorMinigameTutorial] 발전기 미니게임 튜토리얼 종료");
    }
}