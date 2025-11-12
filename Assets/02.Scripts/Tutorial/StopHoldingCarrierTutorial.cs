using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 튜토리얼 시작 시 즉시 '캐리어 들기' 상태를 해제하는 스크립트
public class StopHoldingCarrierTutorial : TutorialBase
{
    private PlayerHumanMovement playerMovement;

    public override void Enter()
    {
        playerMovement = FindObjectOfType<PlayerHumanMovement>();

        // 즉시 플레이어 상태를 '캐리어 없음'으로 변경
        if (playerMovement != null)
        {
            playerMovement.SetPlayerHoldingCarrier(false);
        }
    }

    public override void Execute(TutorialController controller)
    {
        // 상태 변경 후, 바로 다음 튜토리얼로 이동
        if (controller != null)
        {
            controller.SetNextTutorial();
        }
    }

    public override void Exit()
    {
        // 특별히 정리할 내용 없음
    }
}