[System.Serializable]
public class SceneData
{
    public string sceneName;
    public bool isRecall;
    public string backgroundMusic;

    public SceneData(string name, bool isReminiscence = false, string bgm = "")
    {
        sceneName = name;
        this.isRecall = isReminiscence;
        backgroundMusic = bgm;
    }
}
