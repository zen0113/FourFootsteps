using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RiminiTrigger : TutorialBase
{
    [Header("플레이어 컨트롤러 설정")]
    [SerializeField]
    private PlayerHumanMovement playerController;

    [Header("튜토리얼 충돌 오브젝트 및 Ui 오브젝트")]
    [SerializeField]
    private Transform triggerObject;


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
        isTrigger = false; // 새로 Enter 할 때 isTrigger 상태 초기화
    }

    public override void Execute(TutorialController controller)
    {
        if (playerController == null)
        {
            Debug.LogError("playerController가 할당되지 않았습니다!", gameObject);
            return;
        }

        // 이 스크립트가 붙은 오브젝트가 플레이어를 따라다니도록 설정
        transform.position = playerController.transform.position;


        if (isTrigger)
        {
            Debug.Log("조건 충족 (isTrigger && keyPressed), 다음 튜토리얼로 진행.");
            controller.SetNextTutorial();
        }
    }

    public override void Exit()
    {
        Debug.Log("Tutorial Exit: " + gameObject.name);
        if (triggerObject != null)
        {
            triggerObject.gameObject.SetActive(false);
        }
    }

    // 이 스크립트가 붙어있는 오브젝트의 Collider2D가 triggerObject의 Collider2D와 충돌했을 때 호출
    private void OnTriggerEnter2D(Collider2D collision)
    {

        if (triggerObject == null)
        {
            Debug.LogError("triggerObject가 할당되지 않아 충돌 비교를 할 수 없습니다!", gameObject);
            return;
        }

        // collision.transform이 Inspector에서 할당한 triggerObject의 transform과 같은지 확인
        if (collision.transform == triggerObject)
        {
            Debug.Log("충돌 감지 성공: " + triggerObject.name + " 와(과) 충돌!");
            isTrigger = true;
        }
    }
}
