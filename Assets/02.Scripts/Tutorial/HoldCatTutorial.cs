using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HoldCatTutorial : TutorialBase
{
    private PlayerHumanMovement playerMovement;

    public override void Enter()
    {
        // 플레이어 참조 찾기
        playerMovement = FindObjectOfType<PlayerHumanMovement>();

        // 즉시 플레이어 상태를 '고양이 들기'로 변경
        if (playerMovement != null)
        {
            playerMovement.SetPlayerHoldingCat(true);
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