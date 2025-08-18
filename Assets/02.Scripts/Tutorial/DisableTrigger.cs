using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisableTrigger : TutorialBase
{
    [Header("플레이어 컨트롤러 설정")]
    [SerializeField] private PlayerHumanMovement playerController;

    [Header("튜토리얼 충돌 오브젝트 및 UI 오브젝트")]
    [SerializeField] private Transform triggerObject;

    [Header("옵션: 충돌 시 오브젝트 비활성화")]
    [SerializeField] private bool disableOnTrigger = true; // false로 하면 충돌해도 유지됨

    private bool isTriggerActivated = false;

    public override void Enter()
    {
        if (triggerObject != null)
        {
            triggerObject.gameObject.SetActive(true);

            // triggerObject가 Trigger로 설정되어 있는지 확인
            Collider2D triggerCollider = triggerObject.GetComponent<Collider2D>();
            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true; // 충돌 감지 대상은 반드시 Trigger여야 함
                Debug.Log($"TriggerObject 설정 완료: {triggerObject.name} (IsTrigger: {triggerCollider.isTrigger})");
            }
            else
            {
                Debug.LogError($"TriggerObject에 Collider2D가 없습니다: {triggerObject.name}", triggerObject);
            }
        }
        else
        {
            Debug.LogError("triggerObject가 할당되지 않았습니다!", gameObject);
        }

        isTriggerActivated = false; // 새로 Enter 할 때 초기화

        // 플레이어 설정 확인 및 로그 출력
        if (playerController != null)
        {
            Collider2D playerCollider = playerController.GetComponent<Collider2D>();
            Rigidbody2D playerRigidbody = playerController.GetComponent<Rigidbody2D>();

            Debug.Log($"플레이어 설정: Collider(IsTrigger: {playerCollider?.isTrigger}), Rigidbody: {playerRigidbody != null}, Tag: {playerController.tag}");

            // 플레이어가 올바르게 설정되어 있는지 확인
            if (playerCollider == null)
            {
                Debug.LogError("플레이어에 Collider2D가 없습니다!", playerController);
            }
            if (playerRigidbody == null)
            {
                Debug.LogWarning("플레이어에 Rigidbody2D가 없습니다. 충돌 감지가 제대로 작동하지 않을 수 있습니다!", playerController);
            }
            if (playerCollider != null && playerCollider.isTrigger)
            {
                Debug.LogWarning("플레이어의 isTrigger가 true로 설정되어 있습니다. false로 변경하는 것을 권장합니다!", playerController);
            }
        }
    }

    public override void Execute(TutorialController controller)
    {
        if (playerController == null)
        {
            Debug.LogError("playerController가 할당되지 않았습니다!", gameObject);
            return;
        }

        // 충돌이 감지되면 다음 튜토리얼로 진행
        if (isTriggerActivated)
        {
            Debug.Log("조건 충족 (플레이어 충돌 감지), 다음 튜토리얼로 진행.");
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

    // 이 스크립트를 triggerObject에 붙이거나, triggerObject가 이 메서드를 호출하도록 설정
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (triggerObject == null || playerController == null)
        {
            Debug.LogError("triggerObject 또는 playerController가 할당되지 않았습니다!", gameObject);
            return;
        }

        // 플레이어와 충돌했는지 확인 (여러 방법)
        bool isPlayerCollision = false;

        // 방법 1: Transform 비교
        if (collision.transform == playerController.transform)
        {
            isPlayerCollision = true;
            Debug.Log("플레이어 충돌 감지 (Transform 비교): " + collision.name);
        }
        // 방법 2: 태그 비교
        else if (collision.CompareTag("Player"))
        {
            isPlayerCollision = true;
            Debug.Log("플레이어 충돌 감지 (Tag 비교): " + collision.name);
        }
        // 방법 3: 컴포넌트 확인
        else if (collision.GetComponent<PlayerHumanMovement>() != null)
        {
            isPlayerCollision = true;
            Debug.Log("플레이어 충돌 감지 (컴포넌트 확인): " + collision.name);
        }

        if (isPlayerCollision)
        {
            Debug.Log("충돌 감지 성공! 플레이어가 " + triggerObject.name + "와 충돌했습니다!");
            isTriggerActivated = true;
        }
        else
        {
            Debug.Log("충돌했지만 플레이어가 아닙니다: " + collision.name + " (Tag: " + collision.tag + ")");
        }
    }

    // 디버깅용: 현재 상태 확인
    [ContextMenu("Debug Current State")]
    public void DebugCurrentState()
    {
        Debug.Log("=== DisableTrigger 디버그 정보 ===");
        Debug.Log($"isTriggerActivated: {isTriggerActivated}");
        Debug.Log($"triggerObject: {(triggerObject != null ? triggerObject.name : "null")}");
        Debug.Log($"playerController: {(playerController != null ? playerController.name : "null")}");

        if (triggerObject != null)
        {
            var col = triggerObject.GetComponent<Collider2D>();
            Debug.Log($"TriggerObject Collider: {col != null}, IsTrigger: {col?.isTrigger}");
        }

        if (playerController != null)
        {
            var col = playerController.GetComponent<Collider2D>();
            var rb = playerController.GetComponent<Rigidbody2D>();
            Debug.Log($"Player Collider: {col != null}, IsTrigger: {col?.isTrigger}");
            Debug.Log($"Player Rigidbody: {rb != null}");
        }
    }
}