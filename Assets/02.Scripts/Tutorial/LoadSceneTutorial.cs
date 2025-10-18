using UnityEngine;

// 이 스크립트가 작동하려면 TutorialController가 사용하던
// 'SceneLoader' 싱글톤이 필요합니다.
// 만약 SceneLoader가 없다면 'using UnityEngine.SceneManagement;'를 추가하고
// 'SceneManager.LoadScene(sceneToLoad);'를 사용해야 합니다.

/// <summary>
/// 이 튜토리얼 단계가 실행되면, 인스펙터에 지정된 'sceneToLoad' 씬을
/// SceneLoader를 통해 직접 로드합니다.
/// 이 튜토리얼은 목록의 마지막에 두는 것이 좋습니다.
/// </summary>
public class LoadSceneTutorial : TutorialBase
{
    [Header("로딩할 씬 설정")]
    [Tooltip("이 튜토리얼 단계에서 로드할 씬의 이름")]
    [SerializeField]
    private string sceneToLoad;

    public override void Enter()
    {
        // 1. 씬 이름이 비어있는지 확인
        if (string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.LogError("[LoadSceneTutorial] 로드할 씬 이름(sceneToLoad)이 비어있습니다! 인스펙터에서 씬 이름을 설정해주세요.");

            // 씬 이름이 없으면 다음 튜토리얼로 넘어가지 않고 여기서 멈춥니다.
            return;
        }

        // 2. 씬 로더가 있는지 확인
        if (SceneLoader.Instance != null)
        {
            // 3. 지정된 씬 로드 실행
            Debug.Log($"[LoadSceneTutorial] '{sceneToLoad}' 씬을 로드합니다.");
            SceneLoader.Instance.LoadScene(sceneToLoad);
        }
        else
        {
            Debug.LogError($"[LoadSceneTutorial] SceneLoader.Instance를 찾을 수 없습니다! '{sceneToLoad}' 씬으로 이동할 수 없습니다.");
        }

        // 참고: 이 스크립트는 TutorialController의 SetNextTutorial()을 호출하지 않습니다.
        // SetNextTutorial()을 호출하면 TutorialController가 'nextSceneName'으로 
        // 씬을 *또* 로드하려고 시도할 수 있기 때문입니다.
        // 이 스크립트는 씬 로드를 직접 실행하고 튜토리얼 흐름을 종료시킵니다.
    }

    public override void Execute(TutorialController controller)
    {
        // 씬 로드가 시작되면(Enter에서) 아무것도 할 필요가 없습니다.
    }

    public override void Exit()
    {
        // 씬이 로드되므로 Exit가 호출될 일이 거의 없습니다.
    }
}