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

    private string playerPrefsKey => playerType == PlayerType.Human ? "humanName" : "catName";

    // Start is called before the first frame update
    void Start()
    {
        LoadPlayerName();
    }

    // Update is called once per frame
    void Update()
    {
        nameOfPlayer = PlayerPrefs.GetString(playerPrefsKey, "none");
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
            PlayerPrefs.SetString(playerPrefsKey, saveName);
            PlayerPrefs.Save();
        }
    }

    private void LoadPlayerName()
    {
        nameOfPlayer = PlayerPrefs.GetString(playerPrefsKey, "none");
        if (loadedName != null)
        {
            loadedName.text = nameOfPlayer;
        }
    }
}
