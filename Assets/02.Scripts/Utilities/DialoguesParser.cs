using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialoguesParser
{
    // CSV files
    private TextAsset dialoguesCSV = Resources.Load<TextAsset>("Datas/dialogues");
    private TextAsset choicesCSV = Resources.Load<TextAsset>("Datas/choices");
    //private TextAsset imagePathsCSV = Resources.Load<TextAsset>("Datas/image paths");
    //private TextAsset backgroundsCSV = Resources.Load<TextAsset>("Datas/backgrounds");

    // Data Structure
    private Dictionary<string, Dialogue> dialogues = new Dictionary<string, Dialogue>();
    private Dictionary<string, Choice> choices = new Dictionary<string, Choice>();
    //private Dictionary<string, ImagePath> imagePaths = new Dictionary<string, ImagePath>();
    //private Dictionary<string, ImagePath> backgrounds = new Dictionary<string, ImagePath>();

    private string Escaper(string originalString)
    {
        string modifiedString = originalString.Replace("\\n", "\n");
        modifiedString = modifiedString.Replace("`", ",");
        // csv파일이기 때문에 스크립트 작성 시, ","이거를 "`"이거로 작성 후 메소드에서 치환
        // 스크립트 중 "\n"을 "\\n"으로 써둔 것도 같은 이유. 
        modifiedString = modifiedString.Replace("", "");

        return modifiedString;
    }

    public Dictionary<string, Dialogue> ParseDialogues()
    {
        string[] lines = dialoguesCSV.text.Split('\n');

        string lastDialogueID = "";

        for (int i = 1; i < lines.Length; i++)
        {
            string[] fields = lines[i].Split(',');

            if ((string.IsNullOrWhiteSpace(lines[i])) || (fields[0] == "" && fields[1] == "")) continue;

            try
            {
                string dialogueID = fields[0].Trim();
                if (string.IsNullOrWhiteSpace(dialogueID)) dialogueID = lastDialogueID;
                else lastDialogueID = dialogueID;

                string speakerID = Escaper(fields[1].Trim());
                bool bubbleMode = (Escaper(fields[2].Trim()) == "TRUE") ? true : false;
                string script = Escaper(fields[3].Trim());
                string textEffect = Escaper(fields[4].Trim());
                string imageID = fields[5].Trim();
                string emotionalState = fields[6].Trim();
                string soundID = fields[7].Trim();
                string cutSceneID = fields[8].Trim();
                string next = fields[9].Trim();

                if (!dialogues.ContainsKey(dialogueID))
                {
                    Dialogue dialogue = new Dialogue(dialogueID);
                    dialogues[dialogueID] = dialogue;
                }

                dialogues[dialogueID].AddLine(speakerID, bubbleMode, script, textEffect, imageID, emotionalState, soundID, cutSceneID, next);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"CSV 파싱 중 오류 발생 (줄 번호 {i + 1}): {e.Message}\n내용: {lines[i]}");
            }
        }

        return dialogues;
    }

    public Dictionary<string, Choice> ParseChoices()
    {
        string[] lines = choicesCSV.text.Split('\n');

        string lastChoiceID = "";

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            string[] fields = lines[i].Split(',');

            try
            {
                string choiceID = fields[0].Trim();
                if (string.IsNullOrWhiteSpace(choiceID)) choiceID = lastChoiceID;
                else lastChoiceID = choiceID;

                string script = Escaper(fields[1].Trim());
                string next = fields[2].Trim();
                string isGoodChoice = fields[3].Trim();

                // TutorialIndex 파싱 (5번째 열, 비어있으면 -1)
                int tutorialIndex = -1;
                if (fields.Length > 4 && !string.IsNullOrWhiteSpace(fields[4]))
                {
                    if (!int.TryParse(fields[4].Trim(), out tutorialIndex))
                    {
                        Debug.LogWarning($"[DialoguesParser] 잘못된 TutorialIndex 형식: {fields[4]} (Choice ID: {choiceID})");
                        tutorialIndex = -1; // 파싱 실패 시 기본값으로 설정
                    }
                }

                // 7번째 열(fields[6])에서 필요 책임지수 값을 읽어옵니다.
                int requiredResponsibility = 0;
                if (fields.Length > 6 && !string.IsNullOrWhiteSpace(fields[6])) // 인덱스를 5 -> 6으로 변경
                {
                    if (!int.TryParse(fields[6].Trim(), out requiredResponsibility)) // 인덱스를 5 -> 6으로 변경
                    {
                        Debug.LogWarning($"[DialoguesParser] 잘못된 RequiredResponsibility 형식: {fields[6]} (Choice ID: {choiceID})");
                        requiredResponsibility = 0; // 파싱 실패 시 기본값으로 설정
                    }
                }


                if (!choices.ContainsKey(choiceID))
                {
                    choices[choiceID] = new Choice(choiceID);
                }

                // AddLine 메서드에 requiredResponsibility 인자 추가
                choices[choiceID].AddLine(script, next, isGoodChoice, tutorialIndex, requiredResponsibility);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"CSV 파싱 중 오류 발생 (줄 번호 {i + 1}): {e.Message}\n내용: {lines[i]}");
            }
        }

        return choices;
    }

    //public Dictionary<string, ImagePath> ParseImagePaths()
    //{
    //    string[] lines = imagePathsCSV.text.Split('\n');

    //    for (int i = 1; i < lines.Length; i++)
    //    {
    //        if (string.IsNullOrWhiteSpace(lines[i])) continue;

    //        string[] fields = lines[i].Split(',');

    //        string imageID = fields[0].Trim();
    //        string girlPath = fields[1].Trim();
    //        string boyPath = fields[2].Trim();
    //        girlPath = $"Characters/{girlPath}";
    //        boyPath = $"Characters/{boyPath}";
    //        ImagePath imagePath = new ImagePath(
    //            imageID,
    //            girlPath,
    //            boyPath
    //        );
    //        imagePaths[imageID] = imagePath;
    //    }

    //    return imagePaths;
    //}

    //public Dictionary<string, ImagePath> ParseBackgrounds()
    //{
    //    string[] lines = backgroundsCSV.text.Split('\n');

    //    for (int i = 1; i < lines.Length; i++)
    //    {
    //        if (string.IsNullOrWhiteSpace(lines[i])) continue;

    //        string[] fields = lines[i].Split(',');

    //        string imageID = fields[0].Trim();
    //        string girlPath = fields[1].Trim();
    //        string boyPath = fields[2].Trim();
    //        girlPath = $"Background Images/{girlPath}";
    //        boyPath = $"Background Images/{boyPath}";
    //        ImagePath imagePath = new ImagePath(
    //            imageID,
    //            girlPath,
    //            boyPath
    //        );
    //        backgrounds[imageID] = imagePath;
    //    }

    //    return backgrounds;
    //}

    //public Dictionary<string, Script> ParseScripts()
    //{
    //    string[] lines = scriptsCSV.text.Split("EOL");

    //    for (int i = 1; i < lines.Length; i++)
    //    {
    //        if (string.IsNullOrWhiteSpace(lines[i])) continue;

    //        string[] fields = lines[i].Split(',');

    //        string scriptID = fields[0].Trim().Trim('\n');
    //        string engScript = Escaper(fields[1].Trim());
    //        string korScript = Escaper(fields[2].Trim());
    //        string jpMScript = Escaper(fields[3].Trim());
    //        string jpWScript = Escaper(fields[4].Trim());
    //        string placeholder = Escaper(fields[5].Trim());

    //        Script script = new Script(
    //            scriptID,
    //            engScript,
    //            korScript,
    //            jpMScript,
    //            jpWScript,
    //            placeholder
    //        );
    //        scripts[scriptID] = script;
    //    }

    //    return scripts;
    //}
}
