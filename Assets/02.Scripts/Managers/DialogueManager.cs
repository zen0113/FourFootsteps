using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Constants;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    // Dialogue UI
    [Header("Dialogue UI")]
    public DialogueType dialogueType = DialogueType.PLAYER_TALKING; // 사용할 대화창 종류
    public GameObject[] dialogueSet;
    public TextMeshProUGUI[] speakerTexts;
    public TextMeshProUGUI[] scriptText;
    public Image[] cutSceneImages;
    public Image[] characterImages;
    public Transform[] choicesContainer;
    public GameObject choicePrefab;
    public GameObject[] skipText;

    // 타자 효과 속도
    [Header("Typing Speed")]
    public float typeSpeed = 0.05f;

    // 글자 흔들리는 효과
    [Header("Text Shake")]
    public float shakeAmount = 1.0f;
    public float shakeSpeed = 30.0f;
    //private bool isTextShaking = false;

    // 자료 구조
    public Dictionary<string, Dialogue> dialogues = new Dictionary<string, Dialogue>();
    private Dictionary<string, Choice> choices = new Dictionary<string, Choice>();

    // 상태 변수
    private string currentDialogueID = "";
    public bool isDialogueActive = false;
    private bool isTyping = false;
    private bool isAuto = false;
    private bool isFast = false;
    private string fullSentence;
    private bool isAutoDelayed = false; // 2초 지난 후 자동으로 넘겨짐
    private bool isFadeOut = false; // AutoDelayed이랑 주로 같이 쓰이며 글자가 투명하게 사라짐
    [SerializeField] private bool isCutsceneFadingToBlack = false;

    private float fadeTime = 0.5f;

    // Dialogue Queue
    private Queue<string> dialogueQueue = new Queue<string>();

    // Bubble Dialogue
    [Header("Bubble Dialogue Function")]
    public Transform playerTransform;   // 플레이어 위치
    public Transform npcTransform;      // npc 위치
    public Vector3 bubbleOffset = new Vector3(0, 2.0f, 0);


    void Awake()
    {
        if (Instance == null)
        {
            DialoguesParser dialoguesParser = new DialoguesParser();
            dialogues = dialoguesParser.ParseDialogues();
            choices = dialoguesParser.ParseChoices();

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        if (dialogueType.ToInt() >= 0 && dialogueType.ToInt() < dialogueSet.Length)
        {
            dialogueSet[dialogueType.ToInt()].SetActive(false);
        }
        else
        {
            Debug.LogWarning("Index out of range: " + dialogueType.ToInt());
        }

    }

    private void Update()
    {
        // 선택지가 있는 상태면 키 입력 받지 못하게 함.
        if (!isDialogueActive || choicesContainer[dialogueType.ToInt()].childCount > 0)
            return;

        // 다이얼로그 출력될 때 space bar 누르면 스크립트 넘겨짐
        if (Input.GetKeyDown(KeyCode.Space))
            OnDialoguePanelClick();
    }

    private void LateUpdate()
    {
        // 플레이어 말풍선
        if(dialogueType== DialogueType.PLAYER_BUBBLE && dialogueSet[dialogueType.ToInt()].activeSelf)
        {
            dialogueSet[dialogueType.ToInt()].transform.position = 
                Camera.main.WorldToScreenPoint(playerTransform.position + bubbleOffset);
        }

        // NPC 말풍선
        if (dialogueType == DialogueType.NPC_BUBBLE && dialogueSet[dialogueType.ToInt()].activeSelf)
        {
            dialogueSet[dialogueType.ToInt()].transform.position = 
                Camera.main.WorldToScreenPoint(npcTransform.position + bubbleOffset);
        }

    }

    Transform FindSpeakerByName(string name)
    {
        GameObject speaker = GameObject.Find(name);
        if (speaker != null) return speaker.transform;
        Debug.LogWarning("Speaker를 찾을 수 없습니다: " + name);
        return null;
    }

    // ---------------------------------------------- Dialogue methods ----------------------------------------------
    public void StartDialogue(string dialogueID)
    {
        if (isDialogueActive)  // 이미 대화가 진행중이면 큐에 넣음
        {
            Debug.Log($"dialogue ID: {dialogueID} queued!");

            dialogueQueue.Enqueue(dialogueID);
            return;
        }

        isDialogueActive = true;

        // 대사가 2개 이상이라면 skip 버튼 활성화
        if (dialogues[dialogueID].Lines.Count > 1)
            foreach (GameObject skip in skipText)
                skip.SetActive(true);

        dialogues[dialogueID].SetCurrentLineIndex(0);
        currentDialogueID = dialogueID;
        DialogueLine initialDialogueLine = dialogues[dialogueID].Lines[0];

        DisplayDialogueLine(initialDialogueLine);
    }

    // 대사 출력을 second초 후에 출력을 시작함.
    // 기본값 second 0으로 넣기
    public IEnumerator StartDialogue(string dialogueID, float second = 0f)
    {
        yield return new WaitForSeconds(second);

        if (isDialogueActive)  // 이미 대화가 진행중이면 큐에 넣음
        {
            Debug.Log($"dialogue ID: {dialogueID} queued!");

            dialogueQueue.Enqueue(dialogueID);
            yield break;
        }

        isDialogueActive = true;

        // 대사가 2개 이상이라면 skip 버튼 활성화
        if (dialogues[dialogueID].Lines.Count > 1)
            foreach (GameObject skip in skipText)
                skip.SetActive(true);

        dialogues[dialogueID].SetCurrentLineIndex(0);
        currentDialogueID = dialogueID;
        DialogueLine initialDialogueLine = dialogues[dialogueID].Lines[0];

        DisplayDialogueLine(initialDialogueLine);
    }

    private void ClearPreviousChoices()
    {
        if (choicesContainer.Length > dialogueType.ToInt())
            foreach (Transform child in choicesContainer[dialogueType.ToInt()])
                Destroy(child.gameObject);
    }

    private void SetupCanvasAndSpeakerText(DialogueLine dialogueLine)
    {
        ChangeDialogueCanvas(dialogueLine.SpeakerID, dialogueLine.BubbleMode);

        // Deactivate all canvases and then activate the selected one.
        foreach (GameObject canvas in dialogueSet)
            if (canvas)
                canvas.SetActive(false);
        dialogueSet[dialogueType.ToInt()].SetActive(true);

        // Update speaker text
        foreach (TextMeshProUGUI speakerText in speakerTexts)
        {
            switch (dialogueLine.SpeakerID)
            {
                case "Player":
                    speakerText.text = GameManager.Instance.GetVariable("PlayerName").ToString();
                    break;

                case "YourCat":
                    speakerText.text = GameManager.Instance.GetVariable("YourCatName").ToString();
                    break;

                case "InnerThoughts":
                //case "PlayerBubble":
                    speakerText.text = "";
                    break;

                default:
                    speakerText.text = dialogueLine.SpeakerID;
                    break;
            }
            if(dialogueLine.BubbleMode)
                speakerText.text = "";
        }
    }

    private string ProcessTextEffect(DialogueLine dialogueLine, out bool auto, out bool fast)
    {
        auto = false;
        fast = false;

        var sentence = dialogueLine.GetScript();

        if (dialogueLine.TextEffect.Length > 0)
        {
            var effects = dialogueLine.TextEffect.Split('/');
            foreach (var effect in effects)
                switch (effect)
                {
                    case "RED":
                        // 문장 전체를 빨갛게 함
                        sentence = $"<color=red>{sentence}</color>";
                        break;
                    case "AUTO":
                        auto = true;
                        isAutoDelayed = false;
                        foreach (GameObject skip in skipText)
                            skip.SetActive(false);
                        break;
                    case "AUTO_DELAYED":
                        // 대사 다 출력 후 몇초 대기 후 다음 대사 자동 출력
                        auto = true;
                        isAutoDelayed = true;
                        foreach (GameObject skip in skipText)
                            skip.SetActive(false);
                        break;
                    case "FADE_OUT":
                        // 대사 다 출력 후 다음 대사로 넘어가기 전에 투명하게 텍스트가 사라짐.
                        isFadeOut = true;
                        foreach (GameObject skip in skipText)
                            skip.SetActive(false);
                        break;
                    case "FAST":
                        fast = true;
                        break;
                    case "TRUE":
                        var playerName = (string)GameManager.Instance.GetVariable("PlayerName");
                        var yourCatName = (string)GameManager.Instance.GetVariable("YourCatName");
                        if (sentence.Contains("{PlayerName}"))
                            sentence = sentence.Replace("{PlayerName}", playerName);
                        else if (sentence.Contains("{YourCatName}"))
                            sentence = sentence.Replace("{YourCatName}", yourCatName);
                        break;
                    case "SHAKE":
                        StartCoroutine(TextShakeEffectCoroutine(dialogueType.ToInt()));
                        break;
                }
        }
        return sentence;
    }

    private void UpdateCutScene(DialogueLine dialogueLine)
    {
        var cutSceneID = dialogueLine.CutSceneID;

        foreach (var currentCutSceneImage in cutSceneImages)
        {
            if (string.IsNullOrWhiteSpace(cutSceneID))
                currentCutSceneImage.color = new Color(1, 1, 1, 0);
            else
            {
                switch (cutSceneID)
                {
                    case "BLACK":
                        // CutScene Image의 Color 값을 WHITE에서 BLACK으로 서서히 바꿈
                        StartCoroutine(FadeToBlack(currentCutSceneImage, 2f));
                        break;

                    case "WHITE":
                        // CutScene Image의 위에 전체 화면을 덮는 Image 객체 생성(하얀색에 알파값은 0)
                        // 알파값 0->1되면 Image 객체 삭제
                        break;

                    default:
                        var cutSceneSprite = Resources.Load<Sprite>($"Art/CutScenes/{cutSceneID}");
                        currentCutSceneImage.sprite = cutSceneSprite;
                        currentCutSceneImage.color = new Color(1, 1, 1, 1);
                        break;
                }
            }
        }    
    }

    // 컷씬 이미지를 Black으로 바꿈
    IEnumerator FadeToBlack(Image targetImage, float duration)
    {
        isCutsceneFadingToBlack = true;
        Color startColor = Color.white;
        Color endColor = Color.black;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            targetImage.color = Color.Lerp(startColor, endColor, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        targetImage.color = endColor; // 보정
        isCutsceneFadingToBlack = false;
    }


    private IEnumerator TextShakeEffectCoroutine(int dialogueType)
    {
        //isTextShaking = true;
        float elapsed = 0f;

        while (true)
        {
            if (Input.GetMouseButtonDown(0))
                break;

            scriptText[dialogueType].ForceMeshUpdate();
            var textInfo = scriptText[dialogueType].textInfo;

            for (int i = 0; i < textInfo.characterCount; i++)
            {
                if (!textInfo.characterInfo[i].isVisible)
                    continue;

                int vertexIndex = textInfo.characterInfo[i].vertexIndex;
                int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;
                Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;

                Vector3 offset = new Vector3(
                    Mathf.Sin(Time.time * shakeSpeed + i) * shakeAmount,
                    Mathf.Cos(Time.time * shakeSpeed + i) * shakeAmount,
                    0);

                for (int j = 0; j < 4; j++)
                {
                    vertices[vertexIndex + j] += offset;
                }
            }

            // Apply changes
            for (int i = 0; i < textInfo.meshInfo.Length; i++)
            {
                var meshInfo = textInfo.meshInfo[i];
                meshInfo.mesh.vertices = meshInfo.vertices;
                scriptText[dialogueType].UpdateGeometry(meshInfo.mesh, i);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 흔들기 종료 후 텍스트 초기화
        scriptText[dialogueType].ForceMeshUpdate(); // 원래 버텍스로 재설정
        //isTextShaking = false;
    }

    private void PlayDialogueSound(DialogueLine dialogueLine)
    {
        //if (string.IsNullOrWhiteSpace(dialogueLine.SoundID))
        //    return;
        //var soundID = "Sound_" + dialogueLine.SoundID;
        //var soundNum = (int)typeof(Constants).GetField(soundID).GetValue(null);
        //SoundPlayer.Instance.UISoundPlay_LOOP(soundNum, true);
    }

    private void UpdateCharacterImages(DialogueLine dialogueLine)
    {
        if (dialogueType == DialogueType.PLAYER_BUBBLE || dialogueType == DialogueType.MONOLOG)
            return;

        var imageID = dialogueLine.ImageID;

        if (string.IsNullOrWhiteSpace(imageID))
        {
            foreach (var characterImage in characterImages)
                characterImage.color = new Color(1, 1, 1, 0);
            return;
        }

        var characterSprite = Resources.Load<Sprite>($"Art/CharacterPortrait/{imageID}");

        characterImages[dialogueType.ToInt()].color = new Color(1, 1, 1, 1);
        characterImages[dialogueType.ToInt()].sprite = characterSprite;
        characterImages[dialogueType.ToInt()].gameObject.SetActive(true);
    }


    private void DisplayDialogueLine(DialogueLine dialogueLine)
    {
        ClearPreviousChoices();
        SetupCanvasAndSpeakerText(dialogueLine);

        // Process placeholders and get final sentence.
        string sentence = ProcessTextEffect(dialogueLine, out bool auto, out bool fast);
        isAuto = auto;
        isFast = fast;

        isTyping = true;
        StartCoroutine(TypeSentence(sentence));

        UpdateCutScene(dialogueLine);
        PlayDialogueSound(dialogueLine);
        UpdateCharacterImages(dialogueLine);
    }

    private void ChangeDialogueCanvas(string speaker, bool bubbleMode)
    {
        //if (dialogueType == DialogueType.CENTER)
        //    dialogueType = DialogueType.PLAYER_TALKING;

        if (bubbleMode)
        {
            // 말풍선 모드
            if (speaker == "Player")
            {
                dialogueType = DialogueType.PLAYER_BUBBLE;
                playerTransform = FindSpeakerByName(speaker);
            }
            else
            {
                dialogueType = DialogueType.NPC_BUBBLE;
                npcTransform = FindSpeakerByName(speaker);
            }
        }
        else
        {
            switch (speaker)
            {
                case "Player":
                    dialogueType = DialogueType.PLAYER_TALKING;
                    break;

                // 회상 마지막에 독백
                case "Monolog":
                    dialogueType = DialogueType.MONOLOG;
                    break;

                case "InnerThoughts":
                    dialogueType = DialogueType.PLAYER_THINKING;
                    break;

                default:
                    dialogueType = DialogueType.NPC;
                    break;
            }
        }
            
    }

    public void EndDialogue()
    {
        // 타자 소리 정지
        SoundPlayer.Instance.UISoundPlay_LOOP(0, false);

        isDialogueActive = false;
        dialogueSet[dialogueType.ToInt()].SetActive(false);
        //foreach (Image characterImage in characterImages)
        //    characterImage.gameObject.SetActive(false);
        if (dialogueQueue.Count > 0)  // 큐에 다이얼로그가 들어있으면 다시 대화 시작
        {
            string queuedDialogueID = dialogueQueue.Dequeue();
            StartDialogue(queuedDialogueID);

            return;
        }
    }

    public void SkipButtonClick()
    {
        if (isCutsceneFadingToBlack) return;

        StopAllCoroutines();

        // 타자 소리 정지
        SoundPlayer.Instance.UISoundPlay_LOOP(0, false);

        dialogues[currentDialogueID].SetCurrentLineIndex(dialogues[currentDialogueID].Lines.Count - 2);
        StartCoroutine(SkipDialogue());
    }

    private IEnumerator SkipDialogue()
    {
        ProceedToNext();

        while (!isTyping) yield return null;

        CompleteSentence();
        if (isFast)
        {
            typeSpeed *= 1.75f; // 타이핑 속도 되돌려 놓기
            isFast = false;
        }
        if (isAuto) isAuto = false;

        OnDialoguePanelClick();
    }

    // ---------------------------------------------- Script methods ----------------------------------------------
    private void ProceedToNext()
    {
        int currentDialogueLineIndex = dialogues[currentDialogueID].CurrentLineIndex;
        string next = dialogues[currentDialogueID].Lines[currentDialogueLineIndex].Next;

        if (EventManager.Instance.events.ContainsKey(next))  // Event인 경우
        {
            EndDialogue();
            EventManager.Instance.CallEvent(next);
        }
        if (dialogues.ContainsKey(next))  // Dialogue인 경우
        {
            EndDialogue();
            StartDialogue(next);
        }
        else if (string.IsNullOrWhiteSpace(next))  // 빈칸인 경우 다음 줄(대사)로 이동
        {
            currentDialogueLineIndex++;

            if (currentDialogueLineIndex >= dialogues[currentDialogueID].Lines.Count)
            {
                EndDialogue();  // 더 이상 DialogueLine이 존재하지 않으면 대화 종료
                return;
            }
            else if (currentDialogueLineIndex == dialogues[currentDialogueID].Lines.Count - 1)
            {
                foreach (GameObject skip in skipText) skip.SetActive(false); //  다이얼로그의 마지막 대사는 스킵 불가능
            }
            dialogues[currentDialogueID].SetCurrentLineIndex(currentDialogueLineIndex);
            DialogueLine nextDialogueLine = dialogues[currentDialogueID].Lines[currentDialogueLineIndex];
            DisplayDialogueLine(nextDialogueLine);
        }
        else if (choices.ContainsKey(next)) // Choice인 경우
        {
            DisplayChoices(next);
        }
    }

    IEnumerator TypeSentence(string sentence)
    {
        scriptText[dialogueType.ToInt()].text = "";
        fullSentence = sentence;

        // <color=red> 같은 글씨 효과들은 출력되지 않도록 변수 설정
        var isEffect = false;
        var effectText = "";

        // FAST 인 경우 두배의 속도로 타이핑
        if (isFast) typeSpeed /= 1.75f;

        // 타자 루프 사운드 시작
        SoundPlayer.Instance.UISoundPlay_LOOP(0, true);

        foreach (char letter in sentence.ToCharArray())
        {
            if (letter == '<')
            {
                effectText = ""; // effectText 초기화
                isEffect = true;
            }
            else if (letter == '>') // > 가 나오면 scriptText에 한번에 붙인다
            {
                effectText += letter;
                scriptText[dialogueType.ToInt()].text += effectText;
                isEffect = false;
                continue;
            }

            if (isEffect) // < 가 나온 이후부터는 effectText에 붙인다
            {
                effectText += letter;
                continue;
            }

            scriptText[dialogueType.ToInt()].text += letter;
            SoundPlayer.Instance.UISoundPlay(0);
            yield return new WaitForSeconds(typeSpeed);
        }

        // 타자 루프 사운드 정지
        SoundPlayer.Instance.UISoundPlay_LOOP(0, false);

        isTyping = false;
        //if (teddyBearIcons.Length > dialogueType.ToInt()) teddyBearIcons[dialogueType.ToInt()].SetActive(true);

        if (isFast)
        {
            typeSpeed *= 1.75f; // 타이핑 속도 되돌려 놓기
            isFast = false;
        }
        if (isAuto)
        {
            while (isTyping) yield return null;
            //yield return new WaitForSeconds(0.25f);

            // AUTO 타입에 따라 분기
            if (isAutoDelayed)
                yield return new WaitForSeconds(2f);  // 2초 대기 후 넘어감

            if (isFadeOut)
            {
                // FadeOut 코루틴이 완료될 때까지 기다림
                yield return StartCoroutine(FadeInOutScriptText(1f, 0f));
            }

            isAuto = false;
            isAutoDelayed = false;
            isFadeOut = false;

            OnDialoguePanelClick(); // 자동으로 넘어감

            foreach (GameObject skip in skipText) skip.SetActive(true);

            // script text 알파값 원상복구
            if (dialogueType == DialogueType.PLAYER_TALKING || dialogueType == DialogueType.NPC)
                scriptText[dialogueType.ToInt()].color = Color.black;
        }
    }
    // 스크립트 Fade In / Out 코루틴
    // start:0, end:1 -> 투명했던 text가 점점 불투명해짐
    // start:1, end:0 -> 불투명했던 text가 점점 투명해짐
    private IEnumerator FadeInOutScriptText(float start, float end)
    {
        if (dialogueType != DialogueType.MONOLOG)
            yield break;

        float current = 0, percent = 0;

        while (percent < 1 && fadeTime != 0)
        {
            current += Time.deltaTime;
            percent = current / fadeTime;

            scriptText[dialogueType.ToInt()].color = new(1, 1, 1, Mathf.Lerp(start, end, percent));

            yield return null;
        }
    }


    public void OnDialoguePanelClick()
    {
        if (!isDialogueActive || isAuto || isCutsceneFadingToBlack) return;

        if (isTyping)
        {
            CompleteSentence();
        }
        else
        {
            ProceedToNext();
        }
    }

    private void CompleteSentence()
    {
        StopAllCoroutines();

        // 타자 소리 정지
        SoundPlayer.Instance.UISoundPlay_LOOP(0, false);

        scriptText[dialogueType.ToInt()].text = fullSentence;
        isTyping = false;
    }

    // ---------------------------------------------- Choice methods ----------------------------------------------
    //private void DisplayChoices(string choiceID)
    //{
    //    if (choicesContainer.Length <= dialogueType.ToInt()) return;

    //    foreach (Transform child in choicesContainer[dialogueType.ToInt()])
    //    {
    //        Destroy(child.gameObject);
    //    }

    //    List<ChoiceLine> choiceLines = choices[choiceID].Lines;

    //    foreach (ChoiceLine choiceLine in choiceLines)
    //    {
    //        var choiceButton = Instantiate(choicePrefab, choicesContainer[dialogueType.ToInt()]).GetComponent<Button>();
    //        var choiceText = choiceButton.GetComponentInChildren<TextMeshProUGUI>();

    //        choiceText.text = choiceLine.GetScript();
    //        choiceButton.onClick.AddListener(() => OnChoiceSelected(choiceLine.Next));
    //    }
    //}

    //private void OnChoiceSelected(string next)
    //{
    //    if (dialogues.ContainsKey(next))
    //    {
    //        EndDialogue();
    //        StartDialogue(next);
    //    }
    //    else if (EventManager.Instance.events.ContainsKey(next))
    //    {
    //        EventManager.Instance.CallEvent(next);
    //    }

    //    foreach (Transform child in choicesContainer[dialogueType.ToInt()])
    //    {
    //        Destroy(child.gameObject);
    //    }

    //}
    private void DisplayChoices(string choiceID)
    {
        if (choicesContainer.Length <= dialogueType.ToInt()) return;
        foreach (Transform child in choicesContainer[dialogueType.ToInt()])
        {
            Destroy(child.gameObject);
        }
        List<ChoiceLine> choiceLines = choices[choiceID].Lines;
        foreach (ChoiceLine choiceLine in choiceLines)
        {
            var choiceButton = Instantiate(choicePrefab, choicesContainer[dialogueType.ToInt()]).GetComponent<Button>();
            var choiceText = choiceButton.GetComponentInChildren<TextMeshProUGUI>();
            choiceText.text = choiceLine.GetScript();
            choiceButton.onClick.AddListener(() => OnChoiceSelected(choiceLine)); // ChoiceLine 전체를 전달
        }
    }

    private void OnChoiceSelected(ChoiceLine choiceLine)
    {
        Debug.Log($"[DialogueManager] 선택지 선택됨: Script={choiceLine.GetScript()}, Next={choiceLine.Next}, TutorialIndex={choiceLine.TutorialIndex}");

        // 선택지 UI 정리
        foreach (Transform child in choicesContainer[dialogueType.ToInt()])
        {
            Destroy(child.gameObject);
        }

        // 대화 종료
        EndDialogue();

        // 현재 활성화된 TutorialDialog에게 선택된 튜토리얼 인덱스 전달
        TutorialDialog currentTutorialDialog = FindObjectOfType<TutorialDialog>();
        if (currentTutorialDialog != null)
        {
            Debug.Log($"[DialogueManager] TutorialDialog로 튜토리얼 인덱스 {choiceLine.TutorialIndex} 전달");
            currentTutorialDialog.OnChoiceSelectedInTutorial(choiceLine.TutorialIndex);
        }
        else
        {
            Debug.LogWarning("[DialogueManager] 활성화된 TutorialDialog를 찾을 수 없습니다.");
        }

        if (choiceLine.TutorialIndex >= 0)
        {
            TutorialController controller = FindObjectOfType<TutorialController>();
            if (controller != null)
            {
                controller.SetTutorialByIndex(choiceLine.TutorialIndex);
            }
            return;
        }
    }
}