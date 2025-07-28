public class DialogueLine
{
    public string SpeakerID { get; private set; }
    public string Script { get; private set; }
    public string TextEffect { get; private set; }
    public string ImageID { get; private set; }
    public string SoundID { get; private set; }
    public string CutSceneID { get; private set; }
    public string Next { get; private set; }

    // initialize function
    public DialogueLine(
        string speakerID,
        string script,
        string textEffect,
        string imageID,
        string soundID,
        string cutSceneID,
        string next)
    {
        SpeakerID = speakerID;
        Script = script;
        TextEffect = textEffect;
        ImageID = imageID;
        SoundID = soundID;
        CutSceneID = cutSceneID;
        Next = next;
    }

    public string GetScript()
    {
        return TextFormat(Script);
    }

    private string TextFormat(string text)
    {
        string newText = text;
        bool textEffect = false;
        int startIndex = -1, endIndex = -1;
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '[')
            {
                textEffect = true;
                startIndex = i;
            }
            if (textEffect && text[i] == ']')
            {
                endIndex = i;

                newText = text.Substring(0, startIndex);
                newText += "<color=red>";
                newText += text.Substring(startIndex + 1, endIndex - startIndex - 1);
                newText += "</color>";
                newText += text.Substring(endIndex + 1);

                text = newText;
            }
        }
        return newText;
    }
}