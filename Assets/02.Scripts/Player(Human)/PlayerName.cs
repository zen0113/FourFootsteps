using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerName : MonoBehaviour
{
    public enum PlayerType
    {
        Human,
        Cat
    }

    [SerializeField] private PlayerType playerType;
    public string nameOfPlayer;
    public string saveName;

    [Header("UI Components")]
    public TMP_InputField inputField;
    public TextMeshProUGUI warningText;
    public TextMeshProUGUI loadedNameText;
    public TextMeshProUGUI CheckNameText;

    [Header("Panel Components")]
    public GameObject SetName_Panel;
    public GameObject CheckName_Panel;


    private string variableKey => playerType == PlayerType.Human ? "PlayerName" : "YourCatName";

    void Start()
    {
        LoadPlayerName();
        if (warningText != null) warningText.text = "";
    }

    public void SetName()
    {
        if (inputField == null) return;

        saveName = inputField.text.Trim();

        if (!IsValidName(saveName))
        {
            if (warningText != null)
                warningText.text = "이름은 2~10자 이내로 입력하세요.";
            return;
        }

        GameManager.Instance.SetVariable(variableKey, saveName);
        nameOfPlayer = saveName;
        UpdateLoadedName();
        CheckSetName();

        if (warningText != null) warningText.text = "";
    }

    private void LoadPlayerName()
    {
        nameOfPlayer = GameManager.Instance.GetVariable(variableKey)?.ToString() ?? "";
        UpdateLoadedName();
    }

    private void UpdateLoadedName()
    {
        if (loadedNameText != null && playerType == PlayerType.Cat)
            loadedNameText.text = nameOfPlayer;
    }

    private bool IsValidName(string name)
    {
        return name.Length >= 2 && name.Length <= 10;
    }

    private void CheckSetName()
    {
        SetName_Panel.SetActive(false);
        CheckName_Panel.SetActive(true);

        //“ㅁㅁ”으로 확정하시겠습니까?
        string checkGuide = "{nameOfPlayer}(으)로 확정하시겠습니까?";
        checkGuide= KoreanJosa.Apply(
            checkGuide,
            ("nameOfPlayer", nameOfPlayer)
        );

        CheckNameText.text = checkGuide;
    }

    // 이름 체크 패널에서 아니오 버튼 누르면
    // 이름 설정 패널 다시 뜨고 이름 다시 쓰기 편하게 초기화됨
    public void RenameButton()
    {
        SetName_Panel.SetActive(true);
        CheckName_Panel.SetActive(false);

        //if (playerType == PlayerType.Cat)
        //    loadedNameText.text = nameOfPlayer;

        inputField.text = null;
        saveName = null;
    }

    // 프롤로그의 고양이 이름 설정 후 예 버튼
    public void NextPrologueStep()
    {
        if (playerType == PlayerType.Human) return;

        StartCoroutine(SetNameCanvas(false));
    }

    public IEnumerator SetNameCanvas(bool toVisible)
    {
        if (playerType == PlayerType.Human) yield break;

        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        float duration = 1f;

        if (toVisible)
        {
            gameObject.SetActive(toVisible);
            yield return UIManager.Instance.FadeCanvasGroup(canvasGroup, toVisible, duration);
        }
        else
        {
            yield return UIManager.Instance.FadeCanvasGroup(canvasGroup, toVisible, duration);
            StartCoroutine(PrologueManager.Instance.ProceedToNextStep());
            gameObject.SetActive(toVisible);
        }

    }

}
