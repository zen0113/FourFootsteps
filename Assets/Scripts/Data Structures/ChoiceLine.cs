public class ChoiceLine
{
    public string Script { get; private set; }
    public string Next { get; private set; }

    // initialize function
    public ChoiceLine(string script, string next)
    {
        Script = script;
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