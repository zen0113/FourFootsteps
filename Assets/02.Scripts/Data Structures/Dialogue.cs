using System.Collections.Generic;

public class Dialogue
{
    public string DialogueID { get; private set; }
    public int CurrentLineIndex { get; private set; }
    public List<DialogueLine> Lines { get; private set; }

    // initialize function
    public Dialogue(string dialogueID, int currentLineIndex = 0)
    {
        DialogueID = dialogueID;
        CurrentLineIndex = currentLineIndex;
        Lines = new List<DialogueLine>();
    }

    public void AddLine(
        string speakerID,
        string script,
        string textEffect,
        string imageID,
        string soundID,
        string cutSceneID,
        string next)
    {
        Lines.Add(new DialogueLine(speakerID, script, textEffect, imageID, soundID, cutSceneID, next));
    }

    public void SetCurrentLineIndex(int currentLineIndex)
    {
        CurrentLineIndex = currentLineIndex;
    }

    
}
