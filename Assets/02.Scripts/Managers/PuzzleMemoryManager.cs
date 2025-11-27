using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PuzzleMemoryManager : MonoBehaviour
{
    public static PuzzleMemoryManager Instance { get; private set; }


    private Dictionary<string, (string title,string positive, string negative)> memoryDictionary 
        = new Dictionary<string, (string title, string positive, string negative)>();
    // MemoryPuzzleStates 가져와서 비교 후 true면 positive 내용, false면 negative 내용 저장
    private Dictionary<string, (string title, string text)> choiceTextDictionary 
        = new Dictionary<string, (string title, string text)>();

    [Header("Puzzle Home의 puzzle Pieces")]
    [Tooltip("Top Group과 Bottom Group에 있는 것들 순서대로 할당")]
    [SerializeField] Button[] HomePuzzlePiecesBtns;
    [SerializeField] GameObject PuzzleHomeGroup;

    [Header("Puzzle Explanation의 UI들")]
    [Tooltip("Puzzle Piece, Puzzle Piece_white, Puzzle Piece_whiteCover 이 3개 할당")]
    [SerializeField] Image[] puzzleWhiteImages;
    [Tooltip("Puzzle Piece_picture 1개 할당")]
    [SerializeField] Image puzzlePictureImage;
    [Tooltip("Puzzle Piece_Crack 1개 할당")]
    [SerializeField] Image puzzleCrackImage;

    [SerializeField] TextMeshProUGUI TitleText;
    [SerializeField] TextMeshProUGUI explainText;
    [SerializeField] GameObject PuzzleExplanationGroup;

    [Header("Puzzle Sprite Rescources")]
    [SerializeField] Sprite[] whitePuzzleResources;
    [SerializeField] Sprite[] picturePuzzleResources;
    [SerializeField] Sprite[] crackResources;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        //ParseMemoryContents();
    }

    private void Start()
    {
        UpdateHomePuzzleState();
        gameObject.SetActive(false);
    }

    // UI Canvas의 PuzzleBag Button에 연결
    public void OnClickPuzzleBag()
    {
        ParseMemoryContents(); // 최신 이름 기반으로 다시 파싱

        bool isActive = true;
        gameObject.SetActive(isActive);
        UpdateHomePuzzleState();
        var player = GameObject.FindGameObjectWithTag("Player");
        if (PlayerCatMovement.Instance != null)
            PlayerCatMovement.Instance.SetMiniGameInputBlocked(isActive);
        else
        {
            if (player.TryGetComponent<PlayerHumanMovement>(out var human))
                human.BlockMiniGameInput(isActive);
        }
        // bgm 사운드 줄이기
        float changedVolume = SoundPlayer.Instance.GetBGMVolume() * 0.5f;
        SoundPlayer.Instance.ChangeVolume(changedVolume);

        Time.timeScale = 0;
    }

    private void UpdateHomePuzzleState()
    {
        // Dictionary<int, bool>로 캐스팅
        var puzzleStates = GameManager.Instance.GetVariable("MemoryPuzzleStates") as Dictionary<int, bool>;

        int currentCount = (int)GameManager.Instance.GetVariable("CurrentMemoryPuzzleCount");

        // 모든 퍼즐 버튼 조각들을 가져와서 버튼 Interactable를 false로 비활성화
        // 이미지 컬러값 Color disableColor = Color.black;으로 맞춤.
        foreach (var button in HomePuzzlePiecesBtns)
        {
            button.interactable = false;
            button.GetComponent<Image>().color = Color.black;

            // ButtonAnimator의 enableHoverEffect 비활성화 
            button.GetComponent<ButtonAnimator>().EnableHoverEffect = false;
        }

        for (int i = 0; i < currentCount; i++)
        {
            // 퍼즐 버튼 조각들 Interactable를 true로 활성화
            // 버튼 이미지 컬러값 white로 맞춰서 활성 가능함 표시
            HomePuzzlePiecesBtns[i].interactable = true;
            HomePuzzlePiecesBtns[i].GetComponent<Image>().color = Color.white;
            // enableHoverEffect 활성화
            HomePuzzlePiecesBtns[i].GetComponent<ButtonAnimator>().EnableHoverEffect = true;

            // puzzleStates[i] 가져와서 memoryDictionary 에 id 번호에 맞춰서
            // puzzleStates[i]이 true면 긍정 선택지 스크립트,
            // false면 부정 선택지 스크립트를 choiceTextDictionary에 저장.
            string puzzleId = "puzzle_" + (i + 1);
            var state = puzzleStates[i];
            string title = memoryDictionary[puzzleId].title;
            string text = state ? memoryDictionary[puzzleId].positive
                    : memoryDictionary[puzzleId].negative;

            if (choiceTextDictionary.ContainsKey(puzzleId))
                choiceTextDictionary[puzzleId] = (title, text);
            else
                choiceTextDictionary.Add(puzzleId, (title, text));
        }
    }

    // 클릭된 퍼즐 Id에 맞춰서 선택지 스크립트 설정
    public void DisplayPuzzleExplain(int idNum)
    {
        PuzzleHomeGroup.SetActive(false);
        PuzzleExplanationGroup.SetActive(true);

        string puzzleId = "puzzle_" + (idNum + 1);

        TitleText.text = choiceTextDictionary[puzzleId].title;
        explainText.text = choiceTextDictionary[puzzleId].text;

        SetPuzzleImageResource(idNum);
    }


    // 클릭된 퍼즐 Id에 맞춰서 퍼즐 이미지 리소스 설정
    private void SetPuzzleImageResource(int idNum)
    {
        foreach (var whiteImage in puzzleWhiteImages)
        {
            whiteImage.sprite = whitePuzzleResources[idNum];
        }
        puzzlePictureImage.sprite = picturePuzzleResources[idNum];

        var puzzleStates = GameManager.Instance.GetVariable("MemoryPuzzleStates") as Dictionary<int, bool>;
        if (!puzzleStates[idNum])
        {
            puzzleCrackImage.gameObject.SetActive(true);
            puzzleCrackImage.sprite = crackResources[idNum];
        }
        else
        {
            puzzleCrackImage.sprite = null;
            puzzleCrackImage.gameObject.SetActive(false);
        }
    }

    // Puzzle Home의 나가기 버튼
    public void OnClickExitButton()
    {
        bool isActive = false;
        gameObject.SetActive(isActive);

        var player = GameObject.FindGameObjectWithTag("Player");
        if (PlayerCatMovement.Instance != null)
            PlayerCatMovement.Instance.SetMiniGameInputBlocked(isActive);
        else
        {
            if (player.TryGetComponent<PlayerHumanMovement>(out var human))
                human.BlockMiniGameInput(isActive);
        }

        float changedVolume = SoundPlayer.Instance.GetBGMVolume() * 2f;
        SoundPlayer.Instance.ChangeVolume(changedVolume);

        Time.timeScale = 1f;
    }

    // Puzzle Explanation Group의 닫기 버튼
    public void OnClickCloseButton()
    {
        PuzzleExplanationGroup.SetActive(false);
        PuzzleHomeGroup.SetActive(true);
    }

    public void ParseMemoryContents()
    {
        TextAsset puzzlesMemoryCsv = Resources.Load<TextAsset>("Datas/puzzlesMemory");
        if(puzzlesMemoryCsv == null)
        {
            Debug.LogError("Failed to load puzzlesMemory CSV file");
            return;
        }

        string[] lines = puzzlesMemoryCsv.text.Split('\n');
        string previousPuzzleID = "";
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            string[] fields = lines[i].Split(',');

            string puzzleID = fields[0].Trim();
            if (puzzleID == "") puzzleID = previousPuzzleID;
            else previousPuzzleID = puzzleID;

            // 제목
            string titleText = fields[2].Trim();

            // 긍정 선택지 스크립트
            string positiveScript = fields[3].Trim();
            positiveScript = ProcessPlaceholders(positiveScript);

            // 부정 선택지 스크립트
            string negativeScript = fields[4].Trim();
            negativeScript = ProcessPlaceholders(negativeScript);

            if (memoryDictionary.ContainsKey(puzzleID))
            {
                memoryDictionary[puzzleID] = (titleText, positiveScript, negativeScript);
            }
            else memoryDictionary.Add(puzzleID, (titleText,positiveScript, negativeScript));

        }

    }

    // 단순하게 {YourCatName} 부분 바꾸는 것만 추가
    private string ProcessPlaceholders(string originalString)
    {
        string yourCatName = (GameManager.Instance.GetVariable("YourCatName") as string) ?? "";

        string modifiedString = originalString.Replace("\\n", "\n")
                       .Replace("`", ",")
                       .Replace("", "")
                       .Replace("\u0008", "");

        // 한 번에 모든 패턴(괄호/슬래시/단일조사/단독)을 처리
        modifiedString = KoreanJosa.Apply(
            modifiedString,
            ("YourCatName", yourCatName)
        );

        return modifiedString;
    }


    public void TEST_DebugingChoice(int num)
    {
        System.Random rand = new System.Random();

        GameManager.Instance.SetVariable("CurrentMemoryPuzzleCount", num);
        var puzzleStates = GameManager.Instance.GetVariable("MemoryPuzzleStates") as Dictionary<int, bool>;

        int score = 0;
        for (int i = 0; i < num; i++)
        {
            puzzleStates[i] = rand.Next(2) == 0;
            if (puzzleStates[i])
                ++score;
        }
        GameManager.Instance.SetVariable("ResponsibilityScore", score);
        ResultManager.Instance.Test();
    }
}
