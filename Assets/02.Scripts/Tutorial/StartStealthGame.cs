using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartStealthGame : TutorialBase
{
    // 은신 미니게임 세팅 및 시작
    [Header("Refs")]
    [SerializeField] private FollowCamera followCamera;
    [SerializeField] private GameObject Kids_worldImage;
    private Animator playerAnimator;
    private MonoBehaviour playerController;

    [Header("UI")]
    [SerializeField] private GameObject StealthCanvas;
    [SerializeField] private GameObject StealthUICanvas;

    private float originalXValue = 4f;

    private void Awake()
    {
        FindPlayerController();
        playerAnimator = playerController
           ? playerController.GetComponentInChildren<Animator>(true)
           : null;

        if (!playerAnimator)
            Debug.LogError($"[{name}] Animator를 찾지 못했습니다. (자식 오브젝트까지 확인 필요)", this);
    }
    public override void Enter()
    {
        followCamera = FindObjectOfType<FollowCamera>();
        followCamera.smoothSpeedX = originalXValue;
        Kids_worldImage.SetActive(false);
        StealthCanvas.SetActive(true);
        StealthUICanvas.SetActive(true);
        KidWatcher.Instance.StartStealthGame();
        PlayerCatMovement.Instance.SetMiniGameInputBlocked(false);
        CatStealthController.Instance.isPlaying = true;
        playerAnimator.Rebind();    // 애니메이터의 모든 파라미터/상태 초기화
        playerAnimator.Update(0f);
    }

    public override void Execute(TutorialController controller)
    {
    }

    public override void Exit()
    {
        Debug.Log("은신 게임 시작");
    }

    private void FindPlayerController()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (!playerObject)
        {
            Debug.LogWarning($"[{name}] 'Player' 태그 오브젝트를 찾지 못했습니다.", this);
            return;
        }

        playerController = playerObject.GetComponent<PlayerCatMovement>();

        if (!playerController)
            Debug.LogWarning($"[{name}] <PlayerCatMovement> 컴포넌트를 Player에서 찾지 못했습니다.", this);
        //else
        //    Debug.Log($"[{name}] Player 찾기 성공: {playerObject.name} <PlayerCatMovement>", this);
    }
}
