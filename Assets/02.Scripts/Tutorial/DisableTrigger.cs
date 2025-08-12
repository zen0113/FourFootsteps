using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisableTrigger : TutorialBase
{
    [Header("플레이어 컨트롤러 설정")]
    [SerializeField] private PlayerHumanMovement playerController;

    [Header("튜토리얼 충돌 오브젝트 및 Ui 오브젝트")]
    [SerializeField] private Transform triggerObject;

    [Header("옵션: 충돌 시 오브젝트 비활성화")]
    [SerializeField] private bool disableOnTrigger = true; // ❗ false로 하면 충돌해도 유지됨

    public bool isTrigger { set; get; } = false;

    public override void Enter()
    {
        if (triggerObject != null)
        {
            triggerObject.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogError("triggerObject가 할당되지 않았습니다!", gameObject);
        }
        isTrigger = false; // 새로 Enter 할 때 초기화
    }

    public override void Execute(TutorialController controller)
    {
        if (playerController == null)
        {
            Debug.LogError("playerController가 할당되지 않았습니다!", gameObject);
            return;
        }

        // 플레이어 따라다니도록 위치 고정
        transform.position = playerController.transform.position;

        if (isTrigger)
        {
            Debug.Log("조건 충족 (isTrigger), 다음 튜토리얼로 진행.");
            controller.SetNextTutorial();
        }
    }

    public override void Exit()
    {
        Debug.Log("Tutorial Exit: " + gameObject.name);

        // disableOnTrigger가 true인 경우에만 비활성화
        if (triggerObject != null && disableOnTrigger)
        {
            triggerObject.gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (triggerObject == null)
        {
            Debug.LogError("triggerObject가 할당되지 않아 충돌 비교를 할 수 없습니다!", gameObject);
            return;
        }

        if (collision.transform == triggerObject)
        {
            Debug.Log("충돌 감지 성공: " + triggerObject.name + " 와(과) 충돌!");
            isTrigger = true;
        }
    }
}
