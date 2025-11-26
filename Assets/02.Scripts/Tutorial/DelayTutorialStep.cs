using UnityEngine;
using System.Collections;

/// <summary>
/// 지정된 시간(초)만큼 대기한 후 다음 튜토리얼 단계로 자동으로 넘어갑니다.
/// </summary>
public class DelayTutorialStep : TutorialBase
{
    [Header("지연 설정")]
    [Tooltip("다음 튜토리얼로 넘어가기 전까지 대기할 시간(초)입니다.")]
    [SerializeField] private float delayDuration = 1.0f;

    private Coroutine delayCoroutine;

    // 튜토리얼 단계가 시작되면 호출됩니다.
    public override void Enter()
    {
        // 이전에 실행 중이던 코루틴이 있다면 안전하게 중지합니다.
        if (delayCoroutine != null)
        {
            StopCoroutine(delayCoroutine);
        }

        // 현재 씬의 TutorialController를 찾아 코루틴을 시작합니다.
        TutorialController controller = FindObjectOfType<TutorialController>();
        if (controller != null)
        {
            Debug.Log($"[DelayTutorialStep] {delayDuration}초 대기를 시작합니다.");
            delayCoroutine = StartCoroutine(WaitAndProceed(controller));
        }
        else
        {
            Debug.LogError("[DelayTutorialStep] TutorialController를 찾을 수 없습니다!");
        }
    }

    // 대기 후 다음 단계를 실행하는 코루틴
    private IEnumerator WaitAndProceed(TutorialController controller)
    {
        // 인스펙터에서 설정한 시간만큼 기다립니다.
        yield return new WaitForSeconds(delayDuration);

        Debug.Log($"[DelayTutorialStep] 대기 완료. 다음 튜토리얼로 진행합니다.");

        // 다음 튜토리얼 단계로 넘어갑니다.
        controller.SetNextTutorial();
        delayCoroutine = null;
    }

    public override void Execute(TutorialController controller)
    {
        // 이 단계는 자동으로 진행되므로 Execute 내용은 비워둡니다.
    }

    // 튜토리얼 단계가 종료될 때 호출됩니다.
    public override void Exit()
    {
        // 튜토리얼이 중간에 종료될 경우를 대비해 실행 중인 코루틴을 중지합니다.
        if (delayCoroutine != null)
        {
            StopCoroutine(delayCoroutine);
            delayCoroutine = null;
            Debug.Log("[DelayTutorialStep] 대기 코루틴이 중단되었습니다.");
        }
    }
}