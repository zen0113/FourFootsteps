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

    private bool isConfirmed = false; // 확인 버튼을 눌렀는지 추적하는 변수
    private bool hasProgressed = false; // 다음 튜토리얼로 진행했는지 확인하는 변수
    private TutorialController tutorialController;


    public override void Enter()
    {
        Debug.Log("[GuideCanvasTutorial] 가이드 캔버스 튜토리얼 시작");

        tutorialController = FindObjectOfType<TutorialController>();

        if (guideCanvasObject == null || confirmButton == null || tutorialController == null)
        {
            Debug.LogError("[GuideCanvasTutorial] 필수 컴포넌트(GuideCanvas, Button, TutorialController)가 할당되지 않았거나 씬에 없습니다!");
            // 문제가 있으면 즉시 다음 단계로 넘어가서 게임이 멈추지 않도록 함
            tutorialController?.SetNextTutorial();
            return;
        }

        // 상태 변수 초기화
        isConfirmed = false;
        hasProgressed = false;

        // 플레이어 이동을 제어
        if (PlayerCatMovement.Instance != null)
        {
            PlayerCatMovement.Instance.SetMiniGameInputBlocked(true);
        }

        // 가이드 캔버스를 활성화
        guideCanvasObject.SetActive(true);

        // 확인 버튼에 클릭 이벤트를 연결
        confirmButton.onClick.RemoveAllListeners();
        confirmButton.onClick.AddListener(OnConfirmClicked);
    }

    public override void Execute(TutorialController controller)
    {

    }

    public override void Exit()
    {
        Debug.Log("[GuideCanvasTutorial] 가이드 캔버스 튜토리얼 종료");

        // 플레이어 이동을 다시 가능
        if (PlayerCatMovement.Instance != null)
        {
            PlayerCatMovement.Instance.SetMiniGameInputBlocked(false);
        }

        // 가이드 캔버스를 비활성화
        if (guideCanvasObject != null)
        {
            guideCanvasObject.SetActive(false);
        }
    }

    private void OnConfirmClicked()
    {
        Debug.Log("[GuideCanvasTutorial] 확인 버튼 클릭됨. 즉시 다음 튜토리얼로 진행합니다.");

        // 버튼을 누르는 즉시 다음 튜토리얼로 진행하도록 직접 명령합니다.
        tutorialController?.SetNextTutorial();
    }
}