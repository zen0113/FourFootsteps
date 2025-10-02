using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class NamedUnityEvent
{
    public string eventName;
    public UnityEngine.Events.UnityEvent unityEvent;
}

// UnityEvent 방식 (Inspector에서 직접 설정)
public class UnityEventCodeExecutorTutorial : TutorialBase
{
    [SerializeField] private List<NamedUnityEvent> executedEvents = new List<NamedUnityEvent>();

    [SerializeField] private bool isNextImmediate = true;

    public override void Enter()
    {
        Debug.Log($"[UnityEventCodeExecutorTutorial] {executedEvents.Count}개의 이벤트를 실행합니다.");

        foreach (var namedEvent in executedEvents)
        {
            if (namedEvent.unityEvent != null)
            {
                Debug.Log($"[UnityEventCodeExecutorTutorial] 이벤트 실행: {namedEvent.eventName}");
                namedEvent.unityEvent.Invoke();
            }
        }
        if(isNextImmediate) TutorialController.Instance.SetNextTutorial();
    }

    public override void Execute(TutorialController controller)
    {
        // 필요시 구현
    }

    public override void Exit()
    {
        Debug.Log("[UnityEventCodeExecutorTutorial] UnityEvent 코드 실행 튜토리얼 종료");
    }
}