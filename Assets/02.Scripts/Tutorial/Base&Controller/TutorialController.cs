using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TutorialController : MonoBehaviour
{
    public static TutorialController Instance { get; private set; }

    [SerializeField]
    private List<TutorialBase> tutorials;
    [SerializeField]
    private string nextSceneName = "";
    // [SerializeField] // 이제 인스펙터에서 수동으로 할당할 필요가 없습니다.
    private GameObject black; // 자동으로 찾을 GameObject

    private TutorialBase currentTutorial = null;
    [SerializeField] private int currentIndex = -1;
    public int CurrentIndex => currentIndex;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        // "Black" GameObject를 계층 구조에서 자동으로 찾습니다.
        // "Player UI Canvas" -> "Black" 경로를 가정합니다.
        GameObject playerUICanvas = GameObject.Find("Player UI Canvas");
        if (playerUICanvas != null)
        {
            // 수정된 부분: "Player UI Canvas" 바로 아래에서 "Black"을 찾습니다.
            Transform blackTransform = playerUICanvas.transform.Find("Black");
            if (blackTransform != null)
            {
                black = blackTransform.gameObject;
                Debug.Log("[TutorialController] 'Black' 오브젝트를 성공적으로 찾았습니다. (Player UI Canvas/Black)");
            }
            else
            {
                Debug.LogWarning("[TutorialController] 'Player UI Canvas' 아래에서 'Black' 오브젝트를 찾을 수 없습니다.");
            }
        }
        else
        {
            Debug.LogWarning("[TutorialController] 'Player UI Canvas' 오브젝트를 찾을 수 없습니다. 'Black' 오브젝트를 자동으로 찾을 수 없습니다.");
        }


        // 'black' 오브젝트가 찾아졌으면 비활성화합니다.
        if (black != null)
        {
            black.SetActive(false);
        }
        else
        {
            Debug.LogError("[TutorialController] 'Black' 오브젝트를 찾지 못했습니다. Start에서 비활성화할 수 없습니다.");
        }

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
            StartCoroutine(CompletedAllTutorials());
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
            StartCoroutine(CompletedAllTutorials());
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

    public IEnumerator CompletedAllTutorials()
    {
        currentTutorial = null;
        Debug.Log("Complete All");

        if (!string.IsNullOrEmpty(nextSceneName))
        {
            // SceneLoader.Instance가 있다면 사용, 없다면 Debug.Log만 출력
            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.LoadScene(nextSceneName);
            }
            else
            {
                Debug.LogWarning($"[TutorialController] SceneLoader.Instance를 찾을 수 없습니다. '{nextSceneName}' 씬으로 이동할 수 없습니다.");
            }
            yield return new WaitForSeconds(1f);
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
            StartCoroutine(CompletedAllTutorials());
            return;
        }

        currentIndex = index;
        currentTutorial = tutorials[currentIndex];
        currentTutorial.Enter();
    }
}
