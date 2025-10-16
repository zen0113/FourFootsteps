using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 도로 횡단 미니게임의 전체 흐름을 관리하는 매니저
/// 차량 스폰, 까마귀 공격, 게임 시작/종료를 담당
/// </summary>
public class RoadCrossingManager : MonoBehaviour
{
    [Header("Stage Settings")]
    [Tooltip("게임 시작 위치")]
    public Transform startPosition;
    
    [Tooltip("게임 목표 위치 (도달 시 클리어)")]
    public Transform goalPosition;
    
    [Tooltip("도로 영역 최소 범위")]
    public Vector2 roadMinBounds = new Vector2(-5f, -5f);
    
    [Tooltip("도로 영역 최대 범위")]
    public Vector2 roadMaxBounds = new Vector2(5f, 5f);

    [Header("Lane Settings")]
    [Tooltip("각 차선의 위치를 나타내는 Transform 배열")]
    public Transform[] lanes;
    
    [Tooltip("씬 뷰에서 차선 위치 시각화")]
    public bool showLaneGizmos = true;

    [Header("Vehicle Settings")]
    [Tooltip("차량 프리팹")]
    public GameObject vehiclePrefab;
    
    [Tooltip("차량 생성 간격 (초)")]
    public float vehicleSpawnInterval = 2f;
    
    [Tooltip("차량 생성 시작 대기 시간")]
    public float initialSpawnDelay = 1f;

    [Header("Crow Attack Settings")]
    [Tooltip("까마귀 자동 스폰 사용 (트리거 방식 사용 시 체크 해제)")]
    public bool useAutoCrowSpawn = false;
    
    [Tooltip("까마귀 프리팹 (경고 표시 포함)")]
    public GameObject crowPrefab;
    
    [Tooltip("까마귀 공격 간격 (초)")]
    public float crowAttackInterval = 5f;
    
    [Tooltip("까마귀 공격 시작 대기 시간")]
    public float crowAttackStartDelay = 3f;
    
    [Tooltip("경고 표시가 나타날 Y 위치 (플레이어가 있는 도로 높이)")]
    public float warningYPosition = 0f;
    
    [Tooltip("플레이어 앞쪽 최소 거리")]
    public float minDistanceAhead = 5f;
    
    [Tooltip("플레이어 앞쪽 최대 거리")]
    public float maxDistanceAhead = 10f;

    [Header("Player Settings")]
    [Tooltip("플레이어 오브젝트")]
    public GameObject player;

    // 내부 변수
    private bool isGameActive = false;
    private Coroutine vehicleSpawnCoroutine;
    private Coroutine crowAttackCoroutine;

    void Start()
    {
        // 플레이어가 지정되지 않았다면 자동으로 찾기
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
        }

        StartGame();
    }

    /// <summary>
    /// 미니게임 시작
    /// </summary>
    public void StartGame()
    {
        if (isGameActive) return;

        isGameActive = true;

        // 플레이어를 시작 위치로 이동
        if (player != null && startPosition != null)
        {
            player.transform.position = startPosition.position;
        }

        // 플레이어 입력 차단 해제 (미니게임 중에는 이동 가능)
        var catMovement = player.GetComponent<PlayerCatMovement>();
        if (catMovement != null)
        {
            catMovement.SetMiniGameInputBlocked(false);
        }

        // 차량 스폰 시작
        if (vehicleSpawnCoroutine != null) StopCoroutine(vehicleSpawnCoroutine);
        vehicleSpawnCoroutine = StartCoroutine(VehicleSpawnLoop());

        // 까마귀 공격 시작
        if (crowAttackCoroutine != null) StopCoroutine(crowAttackCoroutine);
        crowAttackCoroutine = StartCoroutine(CrowAttackLoop());

        Debug.Log("[RoadCrossing] 미니게임 시작!");
    }

    /// <summary>
    /// 미니게임 종료
    /// </summary>
    public void EndGame()
    {
        if (!isGameActive) return;

        isGameActive = false;

        // 모든 코루틴 정지
        if (vehicleSpawnCoroutine != null) StopCoroutine(vehicleSpawnCoroutine);
        if (crowAttackCoroutine != null) StopCoroutine(crowAttackCoroutine);

        Debug.Log("[RoadCrossing] 미니게임 종료!");
    }

    /// <summary>
    /// 차량 생성 루프
    /// </summary>
    IEnumerator VehicleSpawnLoop()
    {
        yield return new WaitForSeconds(initialSpawnDelay);

        while (isGameActive)
        {
            SpawnVehicle();
            yield return new WaitForSeconds(vehicleSpawnInterval);
        }
    }

    /// <summary>
    /// 랜덤 차선에 차량 생성
    /// </summary>
    void SpawnVehicle()
    {
        if (lanes == null || lanes.Length == 0)
        {
            Debug.LogWarning("[RoadCrossing] 차선이 설정되지 않았습니다!");
            return;
        }

        if (vehiclePrefab == null)
        {
            Debug.LogWarning("[RoadCrossing] 차량 프리팹이 설정되지 않았습니다!");
            return;
        }

        // 랜덤 차선 선택
        int randomIndex = Random.Range(0, lanes.Length);
        Transform selectedLane = lanes[randomIndex];

        // 차량 생성
        GameObject vehicle = Instantiate(vehiclePrefab, selectedLane.position, Quaternion.identity);
        
        // 차량에 차선 X좌표 전달
        RoadVehicle vehicleScript = vehicle.GetComponent<RoadVehicle>();
        if (vehicleScript != null)
        {
            vehicleScript.SetLaneX(selectedLane.position.x);
        }
    }

    /// <summary>
    /// 까마귀 공격 루프
    /// </summary>
    IEnumerator CrowAttackLoop()
    {
        yield return new WaitForSeconds(crowAttackStartDelay);

        while (isGameActive)
        {
            yield return new WaitForSeconds(crowAttackInterval);
            ExecuteCrowAttack();
        }
    }

    /// <summary>
    /// 까마귀 공격 실행 (까마귀 프리팹이 경고와 낙하를 모두 처리)
    /// </summary>
    void ExecuteCrowAttack()
    {
        if (crowPrefab == null)
        {
            Debug.LogWarning("[RoadCrossing] 까마귀 프리팹이 설정되지 않았습니다!");
            return;
        }

        if (player == null)
        {
            Debug.LogWarning("[RoadCrossing] 플레이어를 찾을 수 없습니다!");
            return;
        }

        // 플레이어의 현재 X 위치
        float playerX = player.transform.position.x;
        
        // 플레이어 앞쪽(오른쪽)으로 랜덤 거리만큼 떨어진 X 좌표 계산
        float randomDistance = Random.Range(minDistanceAhead, maxDistanceAhead);
        float targetX = playerX + randomDistance;
        
        // 도로 범위를 벗어나지 않도록 제한
        targetX = Mathf.Clamp(targetX, roadMinBounds.x, roadMaxBounds.x);
        
        // 경고가 표시될 위치 (플레이어가 있는 도로 높이)
        Vector2 warningPosition = new Vector2(targetX, warningYPosition);

        // 까마귀 생성 (경고 표시를 도로 위치에 생성)
        Instantiate(crowPrefab, warningPosition, Quaternion.identity);
        
        Debug.Log($"[RoadCrossing] 까마귀 경고 생성 - 플레이어 X: {playerX}, 목표 X: {targetX} (앞쪽 +{randomDistance}), Y: {warningYPosition}");
    }

    /// <summary>
    /// 플레이어가 목표 지점에 도달했는지 체크
    /// </summary>
    void Update()
    {
        if (!isGameActive || player == null || goalPosition == null) return;

        // 목표 지점과의 거리 체크
        float distance = Vector2.Distance(player.transform.position, goalPosition.position);
        
        if (distance < 1f) // 1유닛 이내에 도달하면 클리어
        {
            OnGameClear();
        }
    }

    /// <summary>
    /// 게임 클리어 처리
    /// </summary>
    void OnGameClear()
    {
        Debug.Log("[RoadCrossing] 미니게임 클리어!");
        EndGame();
        
        // 여기에 클리어 후 처리 추가 가능
        // 예: 다음 씬으로 이동, UI 표시 등
    }

    /// <summary>
    /// 씬 뷰에서 차선과 도로 영역 시각화
    /// </summary>
    void OnDrawGizmos()
    {
        // 도로 영역 표시 (초록색)
        Gizmos.color = Color.green;
        Vector3 center = new Vector3(
            (roadMinBounds.x + roadMaxBounds.x) * 0.5f,
            (roadMinBounds.y + roadMaxBounds.y) * 0.5f,
            0f
        );
        Vector3 size = new Vector3(
            roadMaxBounds.x - roadMinBounds.x,
            roadMaxBounds.y - roadMinBounds.y,
            0f
        );
        Gizmos.DrawWireCube(center, size);

        // 차선 위치 표시 (노란색)
        if (showLaneGizmos && lanes != null)
        {
            Gizmos.color = Color.yellow;
            foreach (Transform lane in lanes)
            {
                if (lane != null)
                {
                    Gizmos.DrawWireSphere(lane.position, 0.3f);
                    Gizmos.DrawLine(lane.position, lane.position + Vector3.down * 20f);
                }
            }
        }

        // 시작/목표 위치 표시
        if (startPosition != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(startPosition.position, 0.5f);
        }

        if (goalPosition != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(goalPosition.position, 0.5f);
        }
    }
}