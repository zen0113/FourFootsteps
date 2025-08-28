using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartStealthGame : TutorialBase
{
    // 은신 미니게임 세팅 및 시작
    [Header("Refs")]
    [SerializeField] private FollowCamera followCamera;
    [SerializeField] private GameObject Kids_worldImage;

    [Header("UI")]
    [SerializeField] private GameObject StealthCanvas;

    private float originalXValue = 0.125f;

    public override void Enter()
    {
        followCamera = FindObjectOfType<FollowCamera>();
        followCamera.smoothSpeedX = originalXValue;
        Kids_worldImage.SetActive(false);
        StealthCanvas.SetActive(true);
        KidWatcher.Instance.StartStealthGame();
        PlayerCatMovement.Instance.SetMiniGameInputBlocked(false);
    }

    public override void Execute(TutorialController controller)
    {
    }

    public override void Exit()
    {
        Debug.Log("은신 게임 시작");
    }
}
