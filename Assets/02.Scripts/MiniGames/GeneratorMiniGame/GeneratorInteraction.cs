using UnityEngine;
using System;

/// <summary>
/// 발전기 오브젝트와의 상호작용을 감지하고 이벤트를 발생시키는 역할만 담당.
/// 실제 미니게임 시작 로직은 튜토리얼 스크립트로 이전됨.
/// </summary>
public class GeneratorInteraction : MonoBehaviour
{
    [Header("트리거 조건 오브젝트")]
    [Tooltip("이 오브젝트가 활성화되어 있을 때만 E키 입력을 받습니다")]
    [SerializeField] private GameObject triggerObject;

    // 플레이어가 상호작용했을 때 발생시킬 이벤트
    public event Action OnInteract;

    private bool canInteract = true;

    void Update()
    {
        // 상호작용이 불가능한 상태이거나, 조건 오브젝트가 비활성화 상태이면 return
        if (!canInteract || (triggerObject != null && !triggerObject.activeSelf))
        {
            return;
        }

        // E키를 눌렀을 때
        if (Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("[GeneratorInteraction] 상호작용 키 (E) 입력 감지됨. OnInteract 이벤트 발생!");
            // 구독된 모든 곳에 이벤트 알림
            OnInteract?.Invoke();
        }
    }

    // 미니게임이 완료되면 다시 상호작용할 수 없도록 외부에서 호출
    public void DisableInteraction()
    {
        canInteract = false;
        if (triggerObject != null)
        {
            triggerObject.SetActive(false);
        }
        Debug.Log("[GeneratorInteraction] 상호작용 비활성화됨.");
    }
}