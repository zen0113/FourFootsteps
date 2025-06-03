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
    public TextMeshProUGUI loadedName;
    public TextMeshProUGUI warningText;

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
        if (warningText != null) warningText.text = "";
    }

    private void LoadPlayerName()
    {
        nameOfPlayer = GameManager.Instance.GetVariable(variableKey)?.ToString() ?? "";
        UpdateLoadedName();
    }

    private void UpdateLoadedName()
    {
        if (loadedName != null)
            loadedName.text = nameOfPlayer;
    }

    private bool IsValidName(string name)
    {
        return name.Length >= 2 && name.Length <= 10;
    }
}
