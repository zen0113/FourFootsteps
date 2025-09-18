using System.Collections.Generic;
//using UnityEngine;

public class Choice
{
    public string ChoiceID { get; private set; }
    public List<ChoiceLine> Lines { get; private set; }

    // initialize function
    public Choice(string choiceId)
    {
        ChoiceID = choiceId;
        Lines = new List<ChoiceLine>();
    }

    public void AddLine(string script, string next, string isGoodchoice, int tutorialIndex = -1)
    {
        Lines.Add(new ChoiceLine(script, next, isGoodchoice, tutorialIndex));
    }
}