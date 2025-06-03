[System.Serializable]
public class SceneData
{
    public string sceneName;
    public bool isReminiscence;
    public string backgroundMusic;

    public SceneData(string name, bool isReminiscence = false, string bgm = "")
    {
        sceneName = name;
        this.isReminiscence = isReminiscence;
        backgroundMusic = bgm;
    }
}
