using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class ControllPlayerAnim : TutorialBase
{
    public enum PlayerControllerType { PlayerCatMovement, PlayerHumanMovement }
    public enum AnimationType { Idle, Crouch, Crouching, Dash, Moving, Jump }

    [Header("플레이어 컨트롤러 타입")]
    [SerializeField] private PlayerControllerType playerControllerType = PlayerControllerType.PlayerCatMovement;
    private MonoBehaviour playerController;

    [Header("강제로 전환할 애니메이션 타입")]
    [SerializeField] private AnimationType updateAnimationType = AnimationType.Idle;
    [SerializeField] private bool resetAnimatorParamsNow = false;

    [Header("Animator 상태 경로 (레이어명 포함)")]
    [Tooltip("보통 \"Base Layer.StateName\" 형식")]
    [SerializeField] private string stIdle = "Base Layer.Idle";
    [SerializeField] private string stMove = "Base Layer.Move";
    [SerializeField] private string stDash = "Base Layer.Dash";
    [SerializeField] private string stCrouch = "Base Layer.Crouch";
    [SerializeField] private string stCrouching = "Base Layer.Crouching";
    [SerializeField] private string stJump = "Base Layer.Jump";

    [Header("강제 재생 옵션")]
    [SerializeField] private int playLayer = 0;
    [SerializeField] private float crossFadeTime = 0.08f;
    [SerializeField] private bool disableDriversWhileTutorial = true; // 플레이어 구동 스크립트 잠시 비활성
    [SerializeField] private bool forceNextFrame = true;              // 한 프레임 뒤 강제 전환

    private Animator animator;

    // 디버그용(필요 시 주석 해제해 사용)
    static void DebugAnimator(Animator anim, int layer = 0)
    {
        if (!anim) { Debug.LogError("[ControllPlayerAnim] Animator null"); return; }
        var st = anim.GetCurrentAnimatorStateInfo(layer);
        var g = anim.playableGraph;
        bool graphPlaying = g.IsValid() && g.IsPlaying();
        Debug.Log($"[Anim Debug] L{layer} inTransition={anim.IsInTransition(layer)}, " +
                  $"stateHash={st.shortNameHash}, time={st.normalizedTime:F2}, " +
                  $"layerWeight={anim.GetLayerWeight(layer):F2}, " +
                  $"culling={anim.cullingMode}, speed={anim.speed}, playableGraph.IsPlaying={graphPlaying}");
    }

    // Timeline/Playable을 잠시 분리해두기 위한 보관용
    private List<PlayableDirector> _capturedDirectors;

    private void Awake()
    {
        FindPlayerController();
        animator = playerController
            ? playerController.GetComponentInChildren<Animator>(true)
            : null;

        if (!animator)
            Debug.LogError($"[{name}] Animator를 찾지 못했습니다. (자식 오브젝트까지 확인 필요)", this);
    }

    /// <summary> Player 태그에서 선택된 타입의 컴포넌트를 찾아 할당 </summary>
    private void FindPlayerController()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (!playerObject)
        {
            Debug.LogWarning($"[{name}] 'Player' 태그 오브젝트를 찾지 못했습니다.", this);
            return;
        }

        switch (playerControllerType)
        {
            case PlayerControllerType.PlayerCatMovement:
                playerController = playerObject.GetComponent<PlayerCatMovement>();
                break;
            case PlayerControllerType.PlayerHumanMovement:
                playerController = playerObject.GetComponent<PlayerHumanMovement>();
                break;
        }

        if (!playerController)
            Debug.LogWarning($"[{name}] {playerControllerType} 컴포넌트를 Player에서 찾지 못했습니다.", this);
        else
            Debug.Log($"[{name}] Player 찾기 성공: {playerObject.name} ({playerControllerType})", this);
    }

    /// <summary> 수동 새로고침 </summary>
    public void RefreshPlayerController() => FindPlayerController();

    /// <summary> 타입 변경 후 다시 찾기 </summary>
    public void SetPlayerControllerType(PlayerControllerType newType)
    {
        playerControllerType = newType;
        FindPlayerController();
    }

    public override void Enter()
    {
        GameManager.Instance.SetVariable("CanMoving", false);

        // 1) 드라이버 스크립트 잠시 정지 (애니메이터 파라미터 상쇄 방지)
        if (disableDriversWhileTutorial) ToggleDrivers(false);

        // 2) 타임라인/플레이어블이 Animator를 장악하고 있다면 해제
        CaptureAndReleaseDirectors(animator);

        // 3) 강제 전환 실행
        if (forceNextFrame)
            StartCoroutine(ForcePlayNextFrame(updateAnimationType));
        else
            ForcePlayNow(updateAnimationType);
    }

    public override void Execute(TutorialController controller) { }

    public override void Exit()
    {
        // 필요하면 타임라인 복구(보통은 복구 불요. 컷신 종료면 그대로가 자연스러움)
        RestoreDirectors();

        if (disableDriversWhileTutorial) ToggleDrivers(true);

        if (resetAnimatorParamsNow)
        {
            // Animator 파라미터 초기화
            if (animator)
            {
                animator.Rebind();  // 모든 파라미터/상태를 초기화
                animator.Update(0f);
            }
        }

        GameManager.Instance.SetVariable("CanMoving", true);
    }


    // ------------------------ 강제 전환 로직 ------------------------

    IEnumerator ForcePlayNextFrame(AnimationType t)
    {
        // 같은 프레임의 다른 컴포넌트 업데이트/전이를 모두 끝낸 뒤 실행
        yield return new WaitForEndOfFrame();
        ForcePlayNow(t);
    }

    private void ForcePlayNow(AnimationType t)
    {
        if (!animator) return;

        // Animator 가시화/업데이트 보장
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        animator.updateMode = AnimatorUpdateMode.Normal;
        animator.speed = 1f;
        animator.SetLayerWeight(playLayer, 1f);

        // 혹시 Animator 자체 그래프가 살아 있으면 멈춤(외부 Playable 영향 제거)
        if (animator.playableGraph.IsValid())
            animator.playableGraph.Stop();

        // 상태 이름 얻기
        string state = GetStatePath(t);
        if (string.IsNullOrEmpty(state))
        {
            Debug.LogWarning($"[{name}] 상태 경로가 비었습니다. AnimationType={t}");
            return;
        }

        // 전이조건/파라미터 무시하고 직접 블렌딩(즉시 점프 필요 시 Play 사용)
        animator.CrossFadeInFixedTime(state, crossFadeTime, playLayer, 0f);
        // animator.Play(Animator.StringToHash(state), playLayer, 0f); // 즉시 점프 대안

        // 디버그
        DebugAnimator(animator, playLayer);

        // 다음 튜토리얼로 진행
        FindObjectOfType<TutorialController>()?.SetNextTutorial();
    }

    private string GetStatePath(AnimationType t)
    {
        switch (t)
        {
            case AnimationType.Idle: return stIdle;
            case AnimationType.Moving: return stMove;
            case AnimationType.Dash: return stDash;
            case AnimationType.Crouch: return stCrouch;
            case AnimationType.Crouching: return stCrouching;
            case AnimationType.Jump: return stJump;
            default: return null;
        }
    }

    // ------------------------ 드라이버 토글 ------------------------

    private void ToggleDrivers(bool on)
    {
        if (!playerController) return;

        if (playerController is PlayerCatMovement cat) cat.enabled = on;
        if (playerController is PlayerHumanMovement hum) hum.enabled = on;

        // 필요시 여기서 다른 애니메이터 드라이버/입력 스크립트도 함께 토글
        // 예: var other = GetComponentInChildren<SomeAnimatorDriver>(true); if (other) other.enabled = on;
    }

    // ------------------------ Timeline/Playable 해제 ------------------------

    private void CaptureAndReleaseDirectors(Animator target)
    {
        if (!target) return;

        _capturedDirectors = new List<PlayableDirector>();

        foreach (var dir in FindObjectsOfType<PlayableDirector>(true))
        {
            bool bound = false;

            // 타임라인 출력 바인딩 검사
            if (dir.playableAsset != null)
            {
                foreach (var output in dir.playableAsset.outputs)
                {
                    var binding = dir.GetGenericBinding(output.sourceObject);
                    if (binding == target || binding == (Object)target.gameObject)
                    {
                        bound = true; break;
                    }
                }
            }

            // 그래프 출력 타겟이 Animator인지 검사
            if (!bound && dir.playableGraph.IsValid())
            {
                for (int i = 0; i < dir.playableGraph.GetOutputCount(); i++)
                {
                    var outp = dir.playableGraph.GetOutput(i);

                    if (!outp.IsOutputValid()) continue;

                    // AnimationPlayableOutput으로 캐스팅
                    if (outp.GetPlayableOutputType() == typeof(AnimationPlayableOutput))
                    {
                        var animOut = (AnimationPlayableOutput)outp;
                        if (animOut.GetTarget() == target)
                        {
                            bound = true;
                            break;
                        }
                    }
                }
            }

            if (bound)
            {
                _capturedDirectors.Add(dir);

                // 가장 확실한 해제: Stop() + Evaluate()로 포즈 잔여 제거
                dir.Stop();
                dir.Evaluate();
            }
        }

        // Animator 측 playableGraph도 완전히 멈춰 영향 제거
        if (target.playableGraph.IsValid())
            target.playableGraph.Stop();
    }

    private void RestoreDirectors()
    {
        if (_capturedDirectors == null) return;

        foreach (var dir in _capturedDirectors)
        {
            // 필요 시 재생 복구 (컷신 재개가 목적이면 주석 해제)
            dir.Play();
        }
        _capturedDirectors.Clear();
    }
}
