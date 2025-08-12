using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wormhole : EventObject, IResultExecutable
{
    private bool isShowed = false;

    [SerializeField] private FollowCamera followCamera;

    [Header("웜홀 대사 키")]
    [SerializeField] private string dialogueKey = "Recall1_008";  // Inspector에서 설정 가능

    private float targetValue = 0.125f;

    private void Awake()
    {
        followCamera = FindObjectOfType<FollowCamera>();
        ResultManager.Instance.RegisterExecutable("WormholeActivation", this);
        gameObject.SetActive(false);
    }

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

    private IEnumerator ActiveWormhole()
    {
        followCamera.smoothSpeedX = 0.01f;
        followCamera.target = gameObject.transform;

        yield return new WaitForSeconds(1.5f);

        // 대사 키를 받아서 시작
        DialogueManager.Instance.StartDialogue(dialogueKey);

        while (DialogueManager.Instance.isDialogueActive)
            yield return null;

        followCamera.target = GameObject.FindWithTag("Player").transform;

        yield return new WaitForSeconds(3f);

        StartCoroutine(SmoothChangeValueCoroutine());

        isShowed = true;
    }

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
            yield return null;
        }

        followCamera.smoothSpeedX = targetValue;
    }
}
