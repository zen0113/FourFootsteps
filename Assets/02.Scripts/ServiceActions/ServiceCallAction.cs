// ServiceCallAction.cs
using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "ServiceActions/Service Call")]
public class ServiceCallAction : ServiceAction
{
    [SerializeReference] public CallPayload payload; // Polymorphic(다형성)

    public override void Execute()
    {
        payload?.Execute();
    }
}

// ==== 페이로드 베이스 ====
[System.Serializable]
public abstract class CallPayload
{
    public virtual bool NeedsCoroutine => false;

    // 즉시 실행 경로
    public virtual void Execute() { }
}

// ---- 페이로드 구현들만 추가/수정 ----

// Used SounPlayer Method 
[System.Serializable]
public class PlaySceneBGMNotAutoPayload : CallPayload
{
    public override void Execute()
    {
        var sp = SoundPlayer.Instance ?? Object.FindObjectOfType<SoundPlayer>(true);
        if (sp == null) { Debug.LogWarning("[PlaySceneBGMNotAutoPayload] SoundPlayer not found."); return; }
        sp.PlaySceneBGMNotAuto();
    }
}
[System.Serializable]
public class ChangeSceneBGMToSceneNamePayload : CallPayload
{
    public string SceneName;
    public override void Execute()
    {
        var sp = SoundPlayer.Instance ?? Object.FindObjectOfType<SoundPlayer>(true);
        if (sp == null) { Debug.LogWarning("[ChangeSceneBGMToSceneNamePayload] SoundPlayer not found."); return; }
        if(SceneName=="") { Debug.LogWarning("SceneName is blank."); return; }
        sp.ChangeSceneBGM(SceneName);
    }
}

// Used UIManager Method 
[System.Serializable]
public class StartWhiteOutInPayload : CallPayload
{
    public override void Execute()
    {
        var uiManager = UIManager.Instance ?? Object.FindObjectOfType<UIManager>(true);
        if (uiManager == null) { Debug.LogWarning("[StartWhiteOutInPayload] UIManager not found."); return; }
        uiManager.StartWhiteOutIn(true);
    }
}
[System.Serializable]
public class StartBlackOutInPayload : CallPayload
{
    public override void Execute()
    {
        var uiManager = UIManager.Instance ?? Object.FindObjectOfType<UIManager>(true);
        if (uiManager == null) { Debug.LogWarning("[StartBlackOutInPayload] UIManager not found."); return; }
        uiManager.StartBlackOutIn(true);
    }
}
[System.Serializable]
public class StartBlackInPayload : CallPayload
{
    public override void Execute()
    {
        var uiManager = UIManager.Instance ?? Object.FindObjectOfType<UIManager>(true);
        if (uiManager == null) { Debug.LogWarning("[StartBlackInPayload] UIManager not found."); return; }
        uiManager.StartBlackIn(true);
    }
}
[System.Serializable]
public class SetAllUIFalsePayload : CallPayload
{
    public override void Execute()
    {
        var uiManager = UIManager.Instance ?? Object.FindObjectOfType<UIManager>(true);
        if (uiManager == null) { Debug.LogWarning("[SetAllUIPayload] UIManager not found."); return; }
        uiManager.SetAllUI(false);
    }
}

// Used SceneUITextManager Method 
[System.Serializable]
public class ResetSceneTextStage2Payload : CallPayload
{
    public override void Execute()
    {
        var sceneUiTxtManager = Object.FindObjectOfType<SceneUITextManager>(true);
        if (sceneUiTxtManager == null) { Debug.LogWarning("[ResetSceneTextStage2Payload] SceneUITextManager not found."); return; }
        sceneUiTxtManager.ResetSceneText("뒷골목");
    }
}
[System.Serializable]
public class ResetSceneTextStage3Payload : CallPayload
{
    public override void Execute()
    {
        var sceneUiTxtManager = Object.FindObjectOfType<SceneUITextManager>(true);
        if (sceneUiTxtManager == null) { Debug.LogWarning("[ResetSceneTextStage3Payload] SceneUITextManager not found."); return; }
        sceneUiTxtManager.ResetSceneText("동물 보호소");
    }
}

// Used GameManager Method 
[System.Serializable]
public class StartEndingCreditPayload : CallPayload
{
    public override void Execute()
    {
        var gameManager = GameManager.Instance ?? Object.FindObjectOfType<GameManager>(true);
        if (gameManager == null) { Debug.LogWarning("[StartEndingCreditPayload] SceneUITextManager not found."); return; }
        gameManager.StartEndingCredit();
    }
}