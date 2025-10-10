using UnityEngine;

/// <summary>
/// 튜토리얼 시작 시 지정된 게임 오브젝트 목록에 포함된 모든 BoxCollider2D를 활성화하는 튜토리얼 스크립트입니다.
/// </summary>
public class ActivateCollidersTutorial : TutorialBase
{
    [Header("설정")]
    [Tooltip("BoxCollider2D를 활성화할 게임 오브젝트들의 목록")]
    [SerializeField] private GameObject[] targetObjects;

    // 튜토리얼 단계가 시작될 때 호출됩니다.
    public override void Enter()
    {
        Debug.Log("[ActivateCollidersTutorial] 콜라이더 활성화 튜토리얼 시작.");

        // targetObjects 배열이 비어있지 않은지 확인합니다.
        if (targetObjects == null || targetObjects.Length == 0)
        {
            Debug.LogWarning("[ActivateCollidersTutorial] 활성화할 대상 오브젝트가 지정되지 않았습니다.");
            return;
        }

        // 목록에 있는 각 게임 오브젝트를 순회합니다.
        foreach (var obj in targetObjects)
        {
            if (obj != null)
            {
                // 게임 오브젝트에서 BoxCollider2D 컴포넌트를 찾습니다.
                BoxCollider2D boxCollider = obj.GetComponent<BoxCollider2D>();

                // BoxCollider2D가 존재하면 활성화(enabled = true)합니다.
                if (boxCollider != null)
                {
                    boxCollider.enabled = true;
                    Debug.Log($"'{obj.name}' 오브젝트의 BoxCollider2D를 활성화했습니다.");
                }
                else
                {
                    Debug.LogWarning($"'{obj.name}' 오브젝트에서 BoxCollider2D를 찾을 수 없습니다.");
                }
            }
        }
    }

    // 이 튜토리얼은 즉시 실행되므로, Execute가 호출되자마자 다음 튜토리얼로 넘어갑니다.
    public override void Execute(TutorialController controller)
    {
        Debug.Log("[ActivateCollidersTutorial] 콜라이더 활성화 완료, 다음 튜토리얼로 진행합니다.");
        controller.SetNextTutorial();
    }

    // 튜토리얼 단계가 종료될 때 호출됩니다. (특별한 정리 작업은 필요 없음)
    public override void Exit()
    {
        Debug.Log("[ActivateCollidersTutorial] 콜라이더 활성화 튜토리얼 종료.");
    }
}