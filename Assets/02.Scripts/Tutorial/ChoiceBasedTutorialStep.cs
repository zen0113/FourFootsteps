using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChoiceBasedTutorialStep : TutorialBase
{
    [System.Serializable]
    public class EventObjectData
    {
        [Header("Target Object")]
        public RandomEventObject targetObject;

        [Header("설정할 값들")]
        public string eventId = "EventRecall2_Good_Doctor";
        public List<string> randomDialogueEventIds = new List<string>
        {
            "EventRecall2_Good_Doctor1",
            "EventRecall2_Good_Doctor2",
            "EventRecall2_Good_Doctor3",
            "EventRecall2_Good_Doctor4"
        };
    }

    [Header("튜토리얼 설정")]
    [SerializeField] private float autoNextDelay = 0.2f;

    [Header("이벤트 오브젝트들 설정")]
    [SerializeField] private List<EventObjectData> eventObjects = new List<EventObjectData>();

    private bool hasRequestedNext = false;
    private Coroutine autoNextCoroutine;
    private TutorialController tutorialControllerRef;

    public override void Enter()
    {
        // 리스트의 모든 오브젝트 처리
        for (int i = 0; i < eventObjects.Count; i++)
        {
            ProcessEventObject(eventObjects[i], $"EventObject_{i}");
        }

        // 자동 다음 단계 처리
        SetupAutoNext();
    }

    private void ProcessEventObject(EventObjectData eventData, string objectName)
    {
        if (eventData.targetObject == null)
        {
            Debug.LogWarning($"[ChoiceBasedTutorialStep] {objectName}이(가) 설정되지 않았습니다.");
            return;
        }

        // 설정된 값으로 이벤트 처리
        SetEventValues(eventData.targetObject, eventData.eventId, eventData.randomDialogueEventIds);

        Debug.Log($"[ChoiceBasedTutorialStep] {objectName} 설정 완료 - EventID: {eventData.eventId}, RandomDialogueEventIds: [{string.Join(", ", eventData.randomDialogueEventIds)}]");
    }

    private void SetEventValues(RandomEventObject eventObject, string eventId, List<string> randomDialogueEventIds)
    {
        try
        {
            // EventObject의 부모 클래스에서 eventId 필드 찾기
            System.Type currentType = eventObject.GetType();
            System.Reflection.FieldInfo eventIdField = null;

            // 상속 계층을 따라 올라가면서 eventId 필드 찾기
            while (currentType != null && eventIdField == null)
            {
                eventIdField = currentType.GetField("eventId",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.Instance);

                if (eventIdField == null)
                {
                    currentType = currentType.BaseType;
                }
            }

            if (eventIdField != null)
            {
                eventIdField.SetValue(eventObject, eventId);
                Debug.Log($"[ChoiceBasedTutorialStep] {eventObject.name} - EventID 설정 성공: {eventId}");
            }
            else
            {
                // Reflection으로 찾지 못한 경우, 프로퍼티로 시도
                System.Reflection.PropertyInfo eventIdProperty = null;
                currentType = eventObject.GetType();

                while (currentType != null && eventIdProperty == null)
                {
                    eventIdProperty = currentType.GetProperty("eventId",
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Public |
                        System.Reflection.BindingFlags.Instance);

                    if (eventIdProperty == null)
                    {
                        currentType = currentType.BaseType;
                    }
                }

                if (eventIdProperty != null && eventIdProperty.CanWrite)
                {
                    eventIdProperty.SetValue(eventObject, eventId);
                    Debug.Log($"[ChoiceBasedTutorialStep] {eventObject.name} - EventID 프로퍼티 설정 성공: {eventId}");
                }
                else
                {
                    Debug.LogWarning($"[ChoiceBasedTutorialStep] {eventObject.name}에서 eventId 필드/프로퍼티를 찾을 수 없습니다. EventObject 클래스의 상속 구조를 확인해주세요.");
                }
            }

            // Random Dialogue Event IDs 설정 - 유효성 검사는 RandomEventObject에서 처리
            if (randomDialogueEventIds != null && randomDialogueEventIds.Count > 0)
            {
                eventObject.SetDialogueIds(randomDialogueEventIds);
                Debug.Log($"[ChoiceBasedTutorialStep] {eventObject.name} - RandomDialogueIds 설정 완료: [{string.Join(", ", randomDialogueEventIds)}]");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ChoiceBasedTutorialStep] {eventObject.name} 이벤트 값 설정 중 오류: {e.Message}\n{e.StackTrace}");
        }
    }

    private void SetupAutoNext()
    {
        if (autoNextCoroutine != null)
        {
            StopCoroutine(autoNextCoroutine);
        }

        hasRequestedNext = false;
        tutorialControllerRef = FindObjectOfType<TutorialController>();
        autoNextCoroutine = StartCoroutine(AutoNextStep(autoNextDelay, tutorialControllerRef));
    }

    public override void Execute(TutorialController controller)
    {
        // 필요시 추가 로직
    }

    public override void Exit()
    {
        if (autoNextCoroutine != null)
        {
            StopCoroutine(autoNextCoroutine);
            autoNextCoroutine = null;
        }
    }

    private IEnumerator AutoNextStep(float delay, TutorialController controllerToUse)
    {
        yield return new WaitForSeconds(delay);

        if (!hasRequestedNext)
        {
            hasRequestedNext = true;
            if (controllerToUse != null)
            {
                controllerToUse.SetNextTutorial();
            }
            else
            {
                Debug.LogWarning("[ChoiceBasedTutorialStep] TutorialController 참조를 찾을 수 없습니다.");
            }
        }

        autoNextCoroutine = null;
    }

    private void OnDestroy()
    {
        if (autoNextCoroutine != null)
        {
            StopCoroutine(autoNextCoroutine);
            autoNextCoroutine = null;
        }
    }

    // 에디터에서 이벤트 오브젝트를 추가하는 헬퍼 메소드
    public void AddEventObject(RandomEventObject targetObject, string eventId, List<string> dialogueIds)
    {
        EventObjectData newEventData = new EventObjectData
        {
            targetObject = targetObject,
            eventId = eventId,
            randomDialogueEventIds = new List<string>(dialogueIds)
        };

        eventObjects.Add(newEventData);
    }

    // 런타임에서 이벤트 오브젝트를 제거하는 헬퍼 메소드
    public void RemoveEventObject(RandomEventObject targetObject)
    {
        eventObjects.RemoveAll(data => data.targetObject == targetObject);
    }
}