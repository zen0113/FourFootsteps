using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TutorialController : MonoBehaviour
{
    [SerializeField]
    private List<TutorialBase> tutorials;
    [SerializeField]
    private string nextSceneName = "";

    private TutorialBase currentTutorial = null;
    private int currentIndex = -1;

    private void Start()
    {
        SetNextTutorial();
    }

    private void Update()
    {
        if (currentTutorial != null)
        {
            currentTutorial.Execute(this);
        }
    }

    public void SetNextTutorial()
    {
        // 현재 튜토리얼의 Exit() 메소드 호출
        if (currentTutorial != null)
        {
            currentTutorial.Exit();
        }

        // 마지막 튜토리얼을 진행했다면 CompletedAllTutorials() 메소드 호출
        if (currentIndex >= tutorials.Count - 1)
        {
            CompletedAllTutorials();
            return;
        }

        // 다음 튜토리얼 과정을 currentTutorial로 등록
        currentIndex++;
        currentTutorial = tutorials[currentIndex];

        // 새로 바뀐 튜토리얼의 Enter() 메소드 호출
        currentTutorial.Enter();
    }

    // 특정 인덱스의 튜토리얼로 건너뛰기
    public void JumpToTutorial(int tutorialIndex)
    {
        // 현재 튜토리얼 종료
        if (currentTutorial != null)
        {
            currentTutorial.Exit();
        }

        // 유효한 인덱스인지 확인
        if (tutorialIndex < 0 || tutorialIndex >= tutorials.Count)
        {
            Debug.LogError($"[TutorialController] 유효하지 않은 튜토리얼 인덱스: {tutorialIndex}. 튜토리얼을 종료합니다.");
            CompletedAllTutorials();
            return;
        }

        // 이전 인덱스 저장 (디버깅용)
        int previousIndex = currentIndex;

        // 지정된 인덱스로 점프
        currentIndex = tutorialIndex;
        currentTutorial = tutorials[currentIndex];

        // 새로운 튜토리얼 시작
        currentTutorial.Enter();

        Debug.Log($"[TutorialController] 튜토리얼 인덱스 {previousIndex}에서 {tutorialIndex}로 건너뛰었습니다.");
    }

    public void CompletedAllTutorials()
    {
        currentTutorial = null;
        // 행동 양식이 여러 종류가 되었을 때 코드 추가 작성
        // 현재는 씬 전환
        Debug.Log("Complete All");
        if (!nextSceneName.Equals(""))
        {
            //SceneManager.LoadScene(nextSceneName);
        }
    }

    [ContextMenu("Show Tutorial List")]
    public void ShowTutorialList()
    {
        for (int i = 0; i < tutorials.Count; i++)
        {
            Debug.Log($"인덱스 {i}: {tutorials[i].name} ({tutorials[i].GetType().Name})");
        }
    }

    public void SetTutorialByIndex(int index)
    {
        if (currentTutorial != null)
            currentTutorial.Exit();

        if (index < 0 || index >= tutorials.Count)
        {
            Debug.LogWarning($"잘못된 튜토리얼 인덱스: {index}");
            CompletedAllTutorials();
            return;
        }

        currentIndex = index;
        currentTutorial = tutorials[currentIndex];
        currentTutorial.Enter();
    }
}

