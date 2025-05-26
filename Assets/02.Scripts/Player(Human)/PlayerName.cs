using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
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
    public Text inputText;
    public Text loadedName;

    private string variableKey => playerType == PlayerType.Human ? "PlayerName" : "YourCatName";

    // Start is called before the first frame update
    void Start()
    {
        LoadPlayerName();
    }

    // Update is called once per frame
    void Update()
    {
        nameOfPlayer = GameManager.Instance.GetVariable(variableKey)?.ToString() ?? "none";
        if (loadedName != null)
        {
            loadedName.text = nameOfPlayer;
        }
    }

    public void SetName()
    {
        if (inputText != null)
        {
            saveName = inputText.text;
            GameManager.Instance.SetVariable(variableKey, saveName);
        }
    }

    private void LoadPlayerName()
    {
        nameOfPlayer = GameManager.Instance.GetVariable(variableKey)?.ToString() ?? "none";
        if (loadedName != null)
        {
            loadedName.text = nameOfPlayer;
        }
    }
}
