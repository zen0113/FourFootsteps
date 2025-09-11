using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NpcAutoMoveTutorial : TutorialBase
{
    [Header("이동 대상 설정")]
    [SerializeField] private List<NpcAutoMover> npcMovers = new List<NpcAutoMover>();
    [SerializeField] private Transform destinationPoint;

    [Header("이동 설정")]
    [SerializeField] private bool isSequential = true; // true: 순차 실행, false: 동시 실행
    [SerializeField] private float sequentialDelay = 2.5f; // 순차 실행 시 간격(초)

    private int arrivedCount = 0; // 도착한 NPC 수 카운트
    private int totalNpcCount = 0; // 전체 NPC 수

    public override void Enter()
    {
        if (npcMovers == null || npcMovers.Count == 0)
        {
            Debug.LogError("[NpcAutoMoveTutorial] NPC 리스트가 비어있습니다.");
            return;
        }

        if (destinationPoint == null)
        {
            Debug.LogError("[NpcAutoMoveTutorial] 목적지가 할당되지 않았습니다.");
            return;
        }

        // null 체크 및 유효한 NPC만 필터링
        npcMovers.RemoveAll(npc => npc == null);
        totalNpcCount = npcMovers.Count;

        if (totalNpcCount == 0)
        {
            Debug.LogError("[NpcAutoMoveTutorial] 유효한 NPC가 없습니다.");
            return;
        }

        arrivedCount = 0;

        // 모든 NPC에 도착 이벤트 등록
        foreach (var npcMover in npcMovers)
        {
            npcMover.OnArrived += HandleArrival;
        }

        // 순차 실행 또는 동시 실행
        if (isSequential)
        {
            StartCoroutine(StartMovingSequentially());
        }
        else
        {
            StartMovingSimultaneously();
        }
    }

    private void StartMovingSimultaneously()
    {
        Debug.Log($"[NpcAutoMoveTutorial] {totalNpcCount}개의 NPC가 동시에 이동을 시작합니다.");

        foreach (var npcMover in npcMovers)
        {
            npcMover.StartMoving(destinationPoint);
        }
    }

    private IEnumerator StartMovingSequentially()
    {
        Debug.Log($"[NpcAutoMoveTutorial] {totalNpcCount}개의 NPC가 {sequentialDelay}초 간격으로 순차 이동을 시작합니다.");

        for (int i = 0; i < npcMovers.Count; i++)
        {
            npcMovers[i].StartMoving(destinationPoint);
            Debug.Log($"[NpcAutoMoveTutorial] NPC {i + 1}/{totalNpcCount} 이동 시작");

            // 마지막 NPC가 아닐 때만 대기
            if (i < npcMovers.Count - 1)
            {
                yield return new WaitForSeconds(sequentialDelay);
            }
        }
    }

    private void HandleArrival()
    {
        arrivedCount++;
        Debug.Log($"[NpcAutoMoveTutorial] NPC 도착: {arrivedCount}/{totalNpcCount}");

        // 모든 NPC가 도착했을 때 다음 튜토리얼로 진행
        if (arrivedCount >= totalNpcCount)
        {
            Debug.Log("[NpcAutoMoveTutorial] 모든 NPC 도착 완료, 다음 튜토리얼로 진행합니다.");
            TutorialController controller = FindObjectOfType<TutorialController>();
            controller?.SetNextTutorial();
        }
    }

    public override void Execute(TutorialController controller)
    {
        // 이제 필요 없음
    }

    public override void Exit()
    {
        // 모든 NPC의 콜백 해제 (중복 방지)
        if (npcMovers != null)
        {
            foreach (var npcMover in npcMovers)
            {
                if (npcMover != null)
                {
                    npcMover.OnArrived -= HandleArrival;
                }
            }
        }
    }
}
