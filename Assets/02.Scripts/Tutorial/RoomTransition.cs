using System.Collections;
using UnityEngine;

public class RoomTransitionTutorial : TutorialBase
{
    [Header("방 이동 설정")]
    [SerializeField] private string targetRoomName; // 이동할 방 이름
    [SerializeField] private bool useFadeEffect = true; // 페이드 효과 사용 여부
    [SerializeField] private float fadeTime = 1f; // 페이드 시간

    [Header("아웃라인 설정")]
    [SerializeField] private GameObject outlineObject; // 아웃라인 오브젝트 (선택적)

    [Header("상호작용 UI")]
    [SerializeField] private InteractionImageDisplay interactionImageDisplay;

    private bool isTransitioning = false; // 이동 중 중복 방지
    private bool isInteracting = false;
    private TutorialController tutorialController;
    private PlayerHumanMovement playerMovement; // 플레이어 이동 스크립트 참조

    public override void Enter()
    {
        isTransitioning = false;
        isInteracting = false;

        // 플레이어 이동 스크립트 찾기
        if (playerMovement == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerMovement = player.GetComponent<PlayerHumanMovement>();
            }
        }

        // 아웃라인 활성화 (있는 경우)
        if (outlineObject != null)
        {
            outlineObject.SetActive(true);
        }
    }

    public override void Execute(TutorialController controller)
    {
        // TutorialController 참조 저장
        tutorialController = controller;
    }

    private void Update()
    {
        // E/W 키 입력 체크 (튜토리얼이 활성화된 상태에서만)
        if (interactionImageDisplay != null &&
        interactionImageDisplay.IsPlayerInArea() &&
        !isTransitioning &&
        !isInteracting &&
        (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.W)))
        {
            OnPlayerInteract();
        }
    }

    /// <summary>
    /// 플레이어가 E키로 상호작용했을 때 호출
    /// </summary>
    private void OnPlayerInteract()
    {
        if (string.IsNullOrEmpty(targetRoomName))
        {
            Debug.LogError("Target room name is not set!");
            return;
        }

        isInteracting = true;

        // 아웃라인 비활성화
        if (outlineObject != null)
        {
            outlineObject.SetActive(false);
        }

        // 상호작용 UI 숨기기
        if (interactionImageDisplay != null)
        {
            interactionImageDisplay.ForceHideImage();
        }

        // 방 이동 시작
        StartCoroutine(TransitionToRoomCoroutine());
    }

    /// <summary>
    /// 방 이동을 처리하는 코루틴
    /// </summary>
    private IEnumerator TransitionToRoomCoroutine()
    {
        isTransitioning = true;

        // 플레이어 이동 제한 활성화
        if (playerMovement != null)
        {
            playerMovement.BlockMiniGameInput(true);
        }

        // GameManager의 이동 제한도 설정
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetVariable("CanMoving", false);
        }

        // 필요한 스폰 포인트와 카메라 타겟 찾기
        GameObject playerSpawnPoint = GameObject.Find($"{targetRoomName}_PlayerSpawn");
        GameObject cameraTargetPoint = GameObject.Find($"{targetRoomName}_CameraTarget");

        // 스폰 포인트 유효성 검사
        if (playerSpawnPoint == null)
        {
            Debug.LogError($"Player spawn point not found: {targetRoomName}_PlayerSpawn");
            RestorePlayerMovement(); // 이동 복구
            isTransitioning = false;
            yield break;
        }

        if (cameraTargetPoint == null)
        {
            Debug.LogError($"Camera target point not found: {targetRoomName}_CameraTarget");
            RestorePlayerMovement(); // 이동 복구
            isTransitioning = false;
            yield break;
        }

        // 페이드 아웃
        if (useFadeEffect && UIManager.Instance != null)
        {
            yield return UIManager.Instance.OnFade(null, 0, 1, fadeTime);
        }

        // 플레이어 이동
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            player.transform.position = playerSpawnPoint.transform.position;
            player.transform.rotation = playerSpawnPoint.transform.rotation;
        }
        else
        {
            Debug.LogError("Player object not found with tag 'Player'");
        }

        // 카메라 이동
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.transform.position = cameraTargetPoint.transform.position;
            mainCamera.transform.rotation = cameraTargetPoint.transform.rotation;
        }
        else
        {
            Debug.LogError("Main camera not found");
        }

        // 짧은 대기 (위치 정착을 위해)
        yield return new WaitForSeconds(0.1f);

        // 페이드 인
        if (useFadeEffect && UIManager.Instance != null)
        {
            yield return UIManager.Instance.OnFade(null, 1, 0, fadeTime);
        }

        // 플레이어 이동 제한 해제
        RestorePlayerMovement();

        isTransitioning = false;

        // 방 이동 완료 후 다음 튜토리얼로 이동
        if (tutorialController != null)
        {
            tutorialController.SetNextTutorial();
        }
    }

    /// <summary>
    /// 플레이어 이동을 복구하는 메서드
    /// </summary>
    private void RestorePlayerMovement()
    {
        if (playerMovement != null)
        {
            playerMovement.BlockMiniGameInput(false);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetVariable("CanMoving", true);
        }
    }

    public override void Exit()
    {
        isTransitioning = false;
        isInteracting = false;

        // 아웃라인 비활성화
        if (outlineObject != null)
        {
            outlineObject.SetActive(false);
        }

        // Exit 시에도 이동 제한 해제 (안전장치)
        RestorePlayerMovement();
    }

    /// <summary>
    /// 외부에서 아웃라인 오브젝트를 설정할 때 사용
    /// </summary>
    /// <param name="outline">아웃라인 오브젝트</param>
    public void SetOutlineObject(GameObject outline)
    {
        outlineObject = outline;
    }

    /// <summary>
    /// 외부에서 타겟 방 이름을 설정할 때 사용
    /// </summary>
    /// <param name="roomName">이동할 방 이름</param>
    public void SetTargetRoom(string roomName)
    {
        targetRoomName = roomName;
    }

    /// <summary>
    /// 페이드 효과 사용 여부를 설정
    /// </summary>
    /// <param name="useFade">페이드 효과 사용 여부</param>
    public void SetUseFadeEffect(bool useFade)
    {
        useFadeEffect = useFade;
    }

    /// <summary>
    /// 페이드 시간을 설정
    /// </summary>
    /// <param name="time">페이드 시간 (초)</param>
    public void SetFadeTime(float time)
    {
        fadeTime = Mathf.Max(0.1f, time); // 최소 0.1초
    }

    // 에디터에서 확인용 (디버깅)
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(targetRoomName))
        {
            Debug.LogWarning($"[{gameObject.name}] Target room name is not set!");
        }
    }
}