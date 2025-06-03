using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wormhole : EventObject, IResultExecutable
{
    private bool isShowed = false;

    [SerializeField] private FollowCamera followCamera;

    private float targetValue = 0.125f;

    private void Awake()
    {
        followCamera = FindObjectOfType<FollowCamera>();
        ResultManager.Instance.RegisterExecutable("WormholeActivation", this);

        gameObject.SetActive(false);
    }

    //private void Start()
    //{
    //    ResultManager.Instance.RegisterExecutable("WormholeActivation", this);
    //}


    public new void Investigate()
    {
        base.Investigate();
    }

    public void ExecuteAction()
    {
        if (isShowed)
            return;

        gameObject.SetActive(true);
        SoundPlayer.Instance.UISoundPlay(Constants.Sound_WormholeActived);

        StartCoroutine(ActiveWormhole());
    }

    // 카메라 이동+웜홀 활성화(이펙트 재생)+처음 발견 스크립트+카메라 복귀
    private IEnumerator ActiveWormhole()
    {

        // 카메라 속도 값 느리게 하여 카메라 이동 연출
        followCamera.smoothSpeedX = 0.01f;
        followCamera.target = gameObject.transform;

        yield return new WaitForSeconds(1.5f);
        DialogueManager.Instance.StartDialogue("Reminiscence1_006");
        //StartCoroutine(DialogueManager.Instance.StartDialogue("Reminiscence1_006", 2f));

        while (DialogueManager.Instance.isDialogueActive)
            yield return null;

        // 카메라 위치 플레이어로 복귀
        followCamera.target = GameObject.FindWithTag("Player").transform;

        yield return new WaitForSeconds(3f);

        // 카메라 속도 값 복구
        StartCoroutine(SmoothChangeValueCoroutine());

        isShowed = true;
    }

    /*
     * Smooth Speed X 원래 값은 0.125
     * 회상 씬에서 저 웜홀 대상으로 카메라 이동할 때만 이동 시키기 전에 잠시 Smooth Speed X 값을 0.01 으로 설정하고
     * 다시 Human으로 카메라 대상 복귀하면 다 복귀 후 값을 0.125로 바꾸기
     */
    private IEnumerator SmoothChangeValueCoroutine()
    {
        float duration = 1.5f;
        float startValue = followCamera.smoothSpeedX;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            followCamera.smoothSpeedX = Mathf.Lerp(startValue, targetValue, t);
            yield return null; // 다음 프레임까지 기다림
        }

        // 정확히 타겟 값으로 보정
        followCamera.smoothSpeedX = targetValue;

    }
}
