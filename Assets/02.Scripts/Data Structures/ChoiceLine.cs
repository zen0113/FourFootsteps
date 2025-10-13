public class ChoiceLine
{
    public string Script { get; private set; }
    public string Next { get; private set; }
    public string IsGoodChoice { get; private set; }
    public int TutorialIndex { get; private set; } //  건너뛸 튜토리얼 인덱스 (-1이면 다음 단계)
    public int RequiredResponsibility { get; set; }

    // initialize function
    public ChoiceLine(string script, string next, string isGoodchoice,int tutorialIndex = -1, int requiredResponsibility = 0)
    {
        Script = script;
        Next = next;
        IsGoodChoice = isGoodchoice;
        TutorialIndex = tutorialIndex;
        RequiredResponsibility = requiredResponsibility;
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