using UnityEngine;

public class CatArrivalLoopTutorial : TutorialBase
{
    [SerializeField] private AnimationLooper animationLooper;
    [SerializeField] private float loopDuration = 3.0f;

    private float timer;

    public override void Enter()
    {
        timer = 0f;

        if (animationLooper != null)
        {
            // 이미 루프 중이 아니면 시작
            if (!animationLooper.IsLooping)
            {
                animationLooper.StartAnimationLoop();
            }
        }
        else
        {
            Debug.LogError("[CatArrivalLoopTutorial] AnimationLooper가 없습니다.");
        }
    }

    public override void Execute(TutorialController controller)
    {
        timer += Time.deltaTime;

        if (timer >= loopDuration)
        {
            controller.SetNextTutorial();
        }
    }

    public override void Exit()
    {
        // ❗ 일부러 StopAnimationLoop() 호출 안 함
        // → 튜토리얼 종료 후에도 애니메이션 루프 지속
    }
}
