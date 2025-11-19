using UnityEngine;
using UnityEngine.UI;

public class GuideCanvasTutorial : TutorialBase
{
    [Header("가이드 UI 설정")]
    [Tooltip("튜토리얼 시작 시 활성화할 가이드 캔버스 오브젝트")]
    [SerializeField] private GameObject guideCanvasObject;

    [Tooltip("가이드를 닫고 다음으로 진행할 확인 버튼")]
    [SerializeField] private Button confirmButton;

    [Header("자동 진행 설정")]
    [Tooltip("확인 버튼 클릭 시 자동으로 다음 튜토리얼로 진행할지 여부")]
    [SerializeField] private bool autoProgressOnConfirm = true;

    private bool isConfirmed = false;
    private bool hasProgressed = false;
    private TutorialController tutorialController;

    public override void Enter()
    {
        Debug.Log("[GuideCanvasTutorial] 가이드 캔버스 튜토리얼 시작");

        tutorialController = FindObjectOfType<TutorialController>();

        if (guideCanvasObject == null || confirmButton == null || tutorialController == null)
        {
            Debug.LogError("[GuideCanvasTutorial] 필수 컴포넌트(GuideCanvas, Button, TutorialController)가 할당되지 않았거나 씬에 없습니다!");
            tutorialController?.SetNextTutorial();
            return;
        }

        // 상태 변수 초기화
        isConfirmed = false;
        hasProgressed = false;

        // 플레이어 움직임 제한 (고양이 & 사람 모두)
        BlockAllPlayerMovement(true);

        // 가이드 캔버스를 활성화
        guideCanvasObject.SetActive(true);

        // 확인 버튼에 클릭 이벤트를 연결
        confirmButton.onClick.RemoveAllListeners();
        confirmButton.onClick.AddListener(OnConfirmClicked);
    }

    public override void Execute(TutorialController controller)
    {
        // 실행 중 로직이 필요하면 여기에 추가
    }

    public override void Exit()
    {
        Debug.Log("[GuideCanvasTutorial] 가이드 캔버스 튜토리얼 종료");

        // 플레이어 움직임 제한 해제
        BlockAllPlayerMovement(false);

        // 가이드 캔버스를 비활성화
        if (guideCanvasObject != null)
        {
            guideCanvasObject.SetActive(false);
        }
    }

    private void OnConfirmClicked()
    {
        Debug.Log("[GuideCanvasTutorial] 확인 버튼 클릭됨. 즉시 다음 튜토리얼로 진행합니다.");
        tutorialController?.SetNextTutorial();
    }

    /// <summary>
    /// 모든 플레이어(고양이, 사람)의 움직임을 제한하거나 해제합니다.
    /// </summary>
    private void BlockAllPlayerMovement(bool isBlocked)
    {
        // 고양이 플레이어 움직임 제어
        if (PlayerCatMovement.Instance != null)
        {
            PlayerCatMovement.Instance.SetMiniGameInputBlocked(isBlocked);
            Debug.Log($"[GuideCanvasTutorial] 고양이 플레이어 입력 차단: {isBlocked}");
        }

        // 사람 플레이어 움직임 제어
        PlayerHumanMovement humanMovement = FindObjectOfType<PlayerHumanMovement>();
        if (humanMovement != null)
        {
            humanMovement.BlockMiniGameInput(isBlocked);
            Debug.Log($"[GuideCanvasTutorial] 사람 플레이어 입력 차단: {isBlocked}");
        }
    }
}