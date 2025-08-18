using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 프롤로그의 흐름과 상태를 관리
public class PrologueManager : MonoBehaviour
{
    public static PrologueManager Instance { get; private set; }

    private int currentStep;
    private bool isPrologueFinished;

    [Header("프롤로그 스테이지")]
    [SerializeField] private List<GameObject> prologueStages = new List<GameObject>();

    [Header("이동 대상 설정")]
    [SerializeField] private List<HumanAutoMover> humanMovers = new List<HumanAutoMover>();
    [SerializeField] private List<Transform> destinationPoints=new List<Transform>();

    [Header("Follow Camera 관리")]
    [SerializeField] private FollowCamera PrologueCamera;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        // 변수 시트로 통해 초기화
        currentStep = (int)GameManager.Instance.GetVariable("PrologueStep");
        isPrologueFinished = (bool)GameManager.Instance.GetVariable("isPrologueFinished");

        foreach (var stage in prologueStages)
        {
            stage.SetActive(false);
        }
    }

    void Start()
    {
        StartCoroutine(ProceedToNextStep());
    }

    public IEnumerator ProceedToNextStep()
    {
        if (isPrologueFinished) yield return null;

        float waitingTime = 2f;
        switch (currentStep)
        {
            case 0:
                Debug.Log($"프롤로그 {currentStep}");
                StartCoroutine(ResultManager.Instance.ExecuteResultCoroutine("Result_DialogueFadeIn"));
                EventManager.Instance.CallEvent("EventPrologue");
                break;

            case 1:
                Debug.Log($"프롤로그 {currentStep}");
                yield return new WaitForSeconds(waitingTime);  // 2초 대기
                StartCoroutine(ResultManager.Instance.ExecuteResultCoroutine("Result_DialogueFadeIn"));
                EventManager.Instance.CallEvent("EventPrologue");
                break;

            case 2:
                Debug.Log($"프롤로그 {currentStep}");
                SoundPlayer.Instance.UISoundPlay(Constants.Sound_RoomDoorOpenAndClose);
                yield return new WaitForSeconds(waitingTime);  // 2초 대기
                StartCoroutine(ResultManager.Instance.ExecuteResultCoroutine("Result_DialogueFadeIn"));
                // 집안 보임
                SetPrologueStage(0,true);
                HumanMoverStart(0);
                EventManager.Instance.CallEvent("EventPrologue");
                // 문에 도착 시, 현관문이 크게 닫히는 효과음 재생
                break;

            case 3:
                Debug.Log($"프롤로그 {currentStep}");
                yield return new WaitForSeconds(waitingTime * 2.5f);  // 5초 대기
                StartCoroutine(ResultManager.Instance.ExecuteResultCoroutine("Result_DialogueFadeIn"));
                // 뒷골목에 있는 플레이어
                // 뒷골목 오른쪽에서 왼쪽으로 자동 이동으로 천천히 걸어감.
                // 플레이어 애니메이션 속도 낮추기
                SetPrologueStage(1, true);
                HumanMoverStart(1);
                //HumanMoverStart(1);
                humanMovers[1].animator.speed = 0.4f;
                EventManager.Instance.CallEvent("EventPrologue");
                // 뒷골목 최종 위치 도착 시, 플레이어 무릎 꿇고 이동장 내려놓음.
                break;

            case 4:
                Debug.Log($"프롤로그 {currentStep}");
                EventManager.Instance.CallEvent("EventPrologue");
                // 이동장 다 내려놓으면 Prologue_006 다이얼로그 재생
                // 다 재생되면 화면 어두워짐
                break;

            case 5:
                Debug.Log($"프롤로그 {currentStep}");
                yield return new WaitForSeconds(waitingTime);  // 2초 대기
                SetPrologueStage(1, false);
                StartCoroutine(ResultManager.Instance.ExecuteResultCoroutine("Result_DialogueFadeIn"));
                // 침대에 누운 컷씬 재생
                // Prologue_007 다이얼로그 재생
                EventManager.Instance.CallEvent("EventPrologue");
                break;

            case 6:
                Debug.Log($"프롤로그 {currentStep}");
                yield return new WaitForSeconds(waitingTime);  // 2초 대기
                StartCoroutine(ResultManager.Instance.ExecuteResultCoroutine("Result_DialogueFadeIn"));
                // Prologue_008 다이얼로그 재생
                EventManager.Instance.CallEvent("EventPrologue");
                break;

            case 7:
                Debug.Log($"프롤로그 {currentStep}");
                // 눈 깜빡
                yield return UIManager.Instance.OnFade(UIManager.Instance.dialogueCoverPanel, 0, 1, 1, true, 1, 0);
                    //yield return UIManager.Instance.OnFade(UIManager.Instance.dialogueCoverPanel, 0, 1, 1, true, 0.25f, 0);
                // Prologue_009 다이얼로그 재생
                EventManager.Instance.CallEvent("EventPrologue");
                break;

            case 8:
                Debug.Log($"프롤로그 {currentStep}");
                // 프롤로그 끝!
                // 스테이지1로 이동
                StartCoroutine(ResultManager.Instance.ExecuteResultCoroutine("Result_DialogueFadeIn"));
                Debug.Log("스테이지1로 이동");
                SceneLoader.Instance.LoadScene(GameManager.Instance.GetNextSceneData().sceneName);
                break;
        }

        currentStep++;
        GameManager.Instance.SetVariable("PrologueStep", currentStep);
    }

    public void EndPrologue()
    {
        isPrologueFinished=true;
        GameManager.Instance.SetVariable("isPrologueFinished", isPrologueFinished);
    }

    // 플레이어 자동 이동 관련 메소드
    private void HandleArrival()
    {
        Debug.Log("사람 도착 완료, 다음 프롤로그 진행.");
        if (currentStep == 3)
        {
            SoundPlayer.Instance.UISoundPlay(Constants.Sound_RoomDoorOpenAndClose);
            StartCoroutine(ResultManager.Instance.ExecuteResultCoroutine("Result_DialogueFadeOut"));
        }
        else
        {
            humanMovers[1].SetHumanCrouch(true);
        }
        StartCoroutine(ProceedToNextStep());
        //HumanMoverComplete();
    }

    // humanMover에 도착 이벤트 등록
    private void HumanMoverStart(int index)
    {
        if (humanMovers[index] != null && destinationPoints[index] != null)
        {
            humanMovers[index].OnArrived += HandleArrival;
            humanMovers[index].StartMoving(destinationPoints[index]);
        }else
            Debug.LogError("[HumanAutoMove_Prologue] 사람 또는 목적지가 할당되지 않았습니다.");
    }

    // 자동 이동하는 스테이지 관리
    private void SetPrologueStage(int index, bool isActive)
    {
        if (prologueStages == null)
        {
            Debug.LogError("프롤로그 스테이지 할당 필요");
            return;
        }

        foreach (var stage in prologueStages)
        {
            stage.SetActive(false);
        }

        if (isActive)
        {
            prologueStages[index].SetActive(isActive);
            // follow 카메라 현재 플레이어에 설정
            PrologueCamera.target = humanMovers[index].GetComponent<Transform>();
        }
        else
        {
            prologueStages[index].SetActive(isActive);
        }
    }

    private void HumanMoverComplete()
    {
        // 콜백 해제 (중복 방지)
        foreach (HumanAutoMover humanMover in humanMovers)
        {
            humanMover.OnArrived -= HandleArrival;
        }
    }
}
