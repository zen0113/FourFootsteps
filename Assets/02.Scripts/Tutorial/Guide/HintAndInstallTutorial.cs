using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HintAndInstallTutorial : TutorialBase
{
    [Header("설치 관련")]
    [SerializeField] private GameObject hintOutline;
    [SerializeField] private GameObject installerPrefab;
    [SerializeField] private List<GameObject> activateObjects = new List<GameObject>();
    [SerializeField] private GameObject hideObject;

    [Header("상호작용 UI")]
    [SerializeField] private InteractionImageDisplay interactionImageDisplay;

    private bool isInstalled = false;
    private bool isInteracting = false;
    private TutorialController tutorialController;

    public override void Enter()
    {
        if (hideObject != null)
        {
            hideObject.SetActive(false);
        }

        hintOutline.SetActive(true);

        // 리스트의 모든 오브젝트 활성화 (아웃라인과 같은 타이밍)
        foreach (GameObject obj in activateObjects)
        {
            if (obj != null)
            {
                obj.SetActive(true);
            }
        }

        isInstalled = false;
        isInteracting = false;
    }

    private void Update()
    {
        if (interactionImageDisplay != null &&
            interactionImageDisplay.IsPlayerInArea() &&
            !isInstalled &&
            !isInteracting &&
            Input.GetKeyDown(KeyCode.E))
        {
            OnPlayerInteract();
        }
    }

    public override void Execute(TutorialController controller)
    {
        // TutorialController 참조 저장
        tutorialController = controller;
    }

    private void OnPlayerInteract()
    {
        isInteracting = true;
        // 설치 처리
        hintOutline.SetActive(false);
        if (installerPrefab != null)
        {
            installerPrefab.SetActive(true);
        }
        isInstalled = true;
        // UI 숨기기
        interactionImageDisplay.ForceHideImage();
        // 다음 튜토리얼로 즉시 이동
        if (tutorialController != null)
        {
            tutorialController.SetNextTutorial();
        }
    }

    public override void Exit()
    {
        hintOutline.SetActive(false);
    }
}