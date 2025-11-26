using UnityEngine;

/// <summary>
/// [역할] 플레이어가 트리거 영역 내에 있을 때 지정된 오브젝트(트럭 앞면)를 활성화합니다.
/// </summary>
public class TruckViewTrigger : MonoBehaviour
{
    [Header("활성화할 오브젝트")]
    [Tooltip("플레이어가 트리거 내에 있을 때 활성화될 게임 오브젝트입니다.")]
    [SerializeField] private GameObject truckFrontObject;

    /// <summary>
    /// 컴포넌트가 처음 로드될 때 호출됩니다.
    /// 초기 상태를 설정합니다.
    /// </summary>
    private void Start()
    {
        // 시작할 때는 트럭 앞면이 보이지 않도록 비활성화합니다.
        if (truckFrontObject != null)
        {
            truckFrontObject.SetActive(false);
        }
        else
        {
            Debug.LogError("[TruckViewTrigger] 활성화할 트럭 앞면 오브젝트가 할당되지 않았습니다!");
        }
    }

    /// <summary>
    /// 다른 Collider2D가 이 오브젝트의 트리거에 들어왔을 때 호출됩니다.
    /// </summary>
    /// <param name="other">충돌한 다른 오브젝트의 Collider2D 정보</param>
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 들어온 오브젝트의 태그가 "Player"인지 확인합니다.
        if (other.CompareTag("Player"))
        {
            Debug.Log("플레이어 감지! 트럭 앞면을 활성화합니다.");
            if (truckFrontObject != null)
            {
                truckFrontObject.SetActive(true);
            }
        }
    }

    /// <summary>
    /// 다른 Collider2D가 이 오브젝트의 트리거에서 나갔을 때 호출됩니다.
    /// </summary>
    /// <param name="other">충돌이 끝난 다른 오브젝트의 Collider2D 정보</param>
    private void OnTriggerExit2D(Collider2D other)
    {
        // 나간 오브젝트의 태그가 "Player"인지 확인합니다.
        if (other.CompareTag("Player"))
        {
            Debug.Log("플레이어 이탈! 트럭 앞면을 비활성화합니다.");
            if (truckFrontObject != null)
            {
                truckFrontObject.SetActive(false);
            }
        }
    }
}