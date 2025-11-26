using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

/// <summary>
/// 원자로 메모리 퍼즐 미니게임
/// 왼쪽 패널에 나타나는 초록 고양이 패턴을 기억하고
/// 오른쪽 패널에서 순서대로 클릭하는 게임
/// </summary>
public class ReactorPuzzle : MonoBehaviour
{
    // ==================== UI 레퍼런스 ====================
    public event Action<bool> OnPuzzleEnd; // true = 성공, false = 실패
    public event Action<bool> OnAnswerSelected; // true: 정답, false: 오답
    public event Action OnStageCleared;

    [Header("UI References")]
    [SerializeField] private GameObject leftPanel;
    [SerializeField] private GameObject rightPanel;
    [Tooltip("왼쪽 패널의 9개 셀 (패턴을 보여주는 용도)")]
    public Image[] leftCells;

    [Tooltip("오른쪽 패널의 9개 버튼 (플레이어가 클릭하는 용도)")]
    public Button[] rightButtons;

    [Tooltip("상단의 5개 단계 표시 불빛")]
    public Image[] stageIndicators;

    [Tooltip("오른쪽 패널의 진행 상황 표시 불빛 (5개)")]
    public Image[] progressIndicators;

    // ==================== 타이머 UI ====================
    [Header("Timer UI")]
    [Tooltip("상단 빨간 신호등 (제한 시간 내)")]
    public Image topLight;

    [Tooltip("중간 초록 신호등 (시간 종료 시)")]
    public Image middleLight;

    [Tooltip("남은 시간 텍스트 (맨 아래 표시)")]
    public Text timeText;

    // ==================== 게임 스프라이트 ====================
    [Header("Game Sprites")]
    [Tooltip("초록 고양이 스프라이트 (정답)")]
    public Sprite greenCatSprite;

    [Tooltip("빨간 고양이 스프라이트 (페이크)")]
    public Sprite redCatSprite;

    [Tooltip("초록 강아지 스프라이트 (페이크)")]
    public Sprite greenDogSprite;

    // ==================== 단계 표시 스프라이트 ====================
    [Header("Stage Indicator Sprites")]
    [Tooltip("완료된 단계를 나타내는 초록 동그라미 스프라이트")]
    public Sprite litCircleSprite;

    [Tooltip("미완료 단계를 나타내는 회색 동그라미 스프라이트")]
    public Sprite unlitCircleSprite;

    // ==================== 색상 설정 ====================
    [Header("Feedback Colors")]
    [Tooltip("버튼의 기본 색상")]
    public Color defaultColor = Color.white;

    [Tooltip("정답 클릭 시 표시할 색상")]
    public Color correctColor = Color.green;

    [Tooltip("오답 클릭 시 표시할 색상")]
    public Color wrongColor = Color.red;

    // ==================== 타이밍 설정 ====================
    [Header("Timing")]
    [Tooltip("이미지가 화면에 표시되는 시간 (초)")]
    public float showDuration = 0.6f;

    [Tooltip("이미지 표시 후 다음 이미지까지의 대기 시간 (초)")]
    public float delayBetween = 0.2f;

    [Tooltip("제한 시간 (초)")]
    public float timeLimit = 60f;

    [Tooltip("피드백 표시 시간 (초)")]
    public float feedbackDuration = 0.2f;

    [Tooltip("단계 완료 후 대기 시간 (초)")]
    public float stageCompleteDuration = 0.5f;

    [Tooltip("실패 후 재시작 대기 시간 (초)")]
    public float failureDuration = 1f;

    // ==================== 게임 상태 변수 ====================

    /// <summary>현재 진행 중인 단계 (1~5)</summary>
    private int currentStage = 1;

    /// <summary>현재 단계의 정답 위치 리스트 (0~8 인덱스)</summary>
    private List<int> currentAnswer = new List<int>();

    /// <summary>플레이어가 현재 입력해야 할 정답의 인덱스</summary>
    private int answerIndex = 0;

    /// <summary>남은 시간</summary>
    private float remainingTime;

    /// <summary>타이머 실행 중인지 여부</summary>
    private bool isTimerRunning = false;

    /// <summary>타이머 코루틴 참조</summary>
    private Coroutine timerCoroutine;

    /// <summary>입력 처리 중 플래그 (중복 입력 방지)</summary>
    private bool isProcessingInput = false;

    /// <summary>게임 로직이 활성화된 상태인지 확인하는 플래그</summary>
    private bool isGameActive = false;

    // ==================== 난이도 설정 ====================

    /// <summary>각 단계별 정답 개수 [1단계, 2단계, 3단계, 4단계, 5단계]</summary>
    private int[] answerCounts = { 2, 3, 4, 5, 5 };

    /// <summary>각 단계별 페이크 개수 [1단계는 0개, 2단계부터 등장]</summary>
    private int[] fakeCounts = { 0, 1, 1, 2, 2 };

    // ==================== Unity 생명주기 ====================

    void Awake()
    {
        for (int i = 0; i < rightButtons.Length; i++)
        {
            rightButtons[i].onClick.RemoveAllListeners();
            int index = i;
            rightButtons[i].onClick.AddListener(() => OnCellClick(index));
        }
    }

    /// <summary>
    /// [명령 1] 게임을 초기 상태로 준비 (패널 숨김 등)
    /// </summary>
    public void PrepareForTutorial()
    {
        gameObject.SetActive(true); // 캔버스 자체를 활성화
        currentStage = 1;
        answerIndex = 0;
        isProcessingInput = false; // 입력 플래그 초기화
        InitializeProgressIndicator();
        UpdateStageIndicator();
        ClearAllCells();
        SetRightPanelInteractable(false);

        if (leftPanel != null) leftPanel.SetActive(false);
        if (rightPanel != null) rightPanel.SetActive(false);
    }

    /// <summary>
    /// [명령 2] 타이머 UI를 보여주고 타이머 시작
    /// </summary>
    public void ShowAndStartTimer()
    {
        InitializeTimer();
        StartTimer();
    }

    /// <summary>
    /// [명령 3] 실제 패턴 암기/입력 단계를 시작 (패널 보이기)
    /// </summary>
    public void StartPatternPhase()
    {
        if (leftPanel != null) leftPanel.SetActive(true);
        if (rightPanel != null) rightPanel.SetActive(true);

        isGameActive = true;
        StartCoroutine(PlayStage());
    }

    /// <summary>
    /// 현재 단계를 진행하는 코루틴
    /// 1. 패턴 생성 -> 2. 화면 초기화 -> 3. 패턴 표시 -> 4. 플레이어 입력 대기
    /// </summary>
    IEnumerator PlayStage()
    {
        Debug.Log($"=== Stage {currentStage} 시작 ===");

        // 1. 이번 단계의 정답 패턴 생성
        GeneratePattern();

        // 2. 양쪽 패널 초기화 (이전 단계 흔적 제거)
        ClearAllCells();
        SetRightPanelInteractable(false);  // 패턴 보여주는 동안 입력 금지

        // 3. 왼쪽 패널에 패턴 순차적으로 표시
        yield return ShowPattern();

        // 4. 플레이어 입력 대기 상태로 전환
        answerIndex = 0;  // 입력 인덱스 초기화
        isProcessingInput = false;  // 새 단계 시작 시 입력 플래그 초기화
        UpdateProgressIndicator();  // 진행 상황 표시 초기화
        SetRightPanelInteractable(true);  // 오른쪽 패널 활성화

        // 타이머는 ShowAndStartTimer()에서 한 번만 시작되므로 여기서는 시작하지 않음
    }

    // ==================== 패턴 생성 ====================

    /// <summary>
    /// 현재 단계에 맞는 랜덤 패턴 생성
    /// - 정답 위치: currentAnswer 리스트에 저장
    /// - 페이크 위치: 화면에만 표시하고 저장하지 않음
    /// </summary>
    void GeneratePattern()
    {
        currentAnswer.Clear();  // 이전 정답 리스트 초기화
        HashSet<int> usedIndices = new HashSet<int>();  // 중복 위치 방지용

        // 배열 인덱스 계산 (stage 1 = index 0)
        int stageIdx = currentStage - 1;
        int answerCount = answerCounts[stageIdx];  // 이번 단계 정답 개수
        int fakeCount = fakeCounts[stageIdx];      // 이번 단계 페이크 개수

        // 정답 위치들을 랜덤으로 생성
        for (int i = 0; i < answerCount; i++)
        {
            int randomIdx = GetRandomUnused(usedIndices);
            currentAnswer.Add(randomIdx);
            usedIndices.Add(randomIdx);
        }

        // 페이크 위치들도 생성 (2단계부터)
        // 페이크는 표시만 하고 따로 저장하지 않음
        for (int i = 0; i < fakeCount; i++)
        {
            int randomIdx = GetRandomUnused(usedIndices);
            usedIndices.Add(randomIdx);
        }
    }

    /// <summary>
    /// 아직 사용되지 않은 랜덤 인덱스를 반환 (0~8 범위)
    /// </summary>
    /// <param name="used">이미 사용된 인덱스들의 집합</param>
    /// <returns>사용 가능한 랜덤 인덱스</returns>
    int GetRandomUnused(HashSet<int> used)
    {
        int random;
        do
        {
            random = Random.Range(0, 9);  // 0~8 중 랜덤 선택
        }
        while (used.Contains(random));  // 이미 사용된 인덱스면 다시 뽑기

        return random;
    }

    // ==================== 패턴 표시 ====================

    /// <summary>
    /// 왼쪽 패널에 패턴을 순차적으로 표시하는 코루틴
    /// - 정답(초록 고양이)과 페이크(빨간 고양이/초록 강아지)를 동시에 표시
    /// - 각 패턴마다 일정 시간 표시 후 사라짐
    /// </summary>
    IEnumerator ShowPattern()
    {
        int stageIdx = currentStage - 1;
        int fakeCount = fakeCounts[stageIdx];

        // 페이크를 표시할 순서를 랜덤으로 결정
        List<int> fakeShowIndices = new List<int>();
        if (fakeCount > 0)
        {
            // 페이크 개수만큼 순서 선택 (각 순서마다 페이크 등장)
            int showCount = fakeCount;

            // 0 ~ (정답 개수-1) 범위에서 랜덤하게 선택
            List<int> availableIndices = new List<int>();
            for (int i = 0; i < currentAnswer.Count; i++)
            {
                availableIndices.Add(i);
            }

            // 페이크를 보여줄 순서 선택
            for (int i = 0; i < Mathf.Min(showCount, currentAnswer.Count); i++)
            {
                int randomIdx = Random.Range(0, availableIndices.Count);
                fakeShowIndices.Add(availableIndices[randomIdx]);
                availableIndices.RemoveAt(randomIdx);
            }
        }

        int fakeCounter = 0; // 현재까지 표시한 페이크 횟수

        // 정답들을 하나씩 순차적으로 표시
        for (int i = 0; i < currentAnswer.Count; i++)
        {
            if (!isGameActive)
            {
                Debug.Log("ShowPattern 중단: isGameActive가 false입니다.");
                yield break; // 코루틴 즉시 종료
            }

            int answerPos = currentAnswer[i];

            // === 정답 이미지 표시 ===
            leftCells[answerPos].sprite = greenCatSprite;
            leftCells[answerPos].enabled = true;
            leftCells[answerPos].color = Color.white;  // Alpha 값을 255로 (불투명)

            // === 페이크 이미지 표시 (랜덤 순서) ===
            List<int> fakePositions = new List<int>();

            if (fakeShowIndices.Contains(i) && fakeCounter < fakeCount)
            {
                // 이미 사용된 위치들을 제외하고 페이크 위치 선택
                HashSet<int> usedInThisFrame = new HashSet<int> { answerPos };

                // 모든 정답 위치도 제외
                for (int j = 0; j < currentAnswer.Count; j++)
                    usedInThisFrame.Add(currentAnswer[j]);

                // 5단계: 빨간 고양이 + 초록 강아지 동시 표시
                if (currentStage == 5)
                {
                    // 빨간 고양이
                    int redCatPos = GetRandomUnused(usedInThisFrame);
                    usedInThisFrame.Add(redCatPos);
                    fakePositions.Add(redCatPos);
                    leftCells[redCatPos].sprite = redCatSprite;
                    leftCells[redCatPos].enabled = true;
                    leftCells[redCatPos].color = Color.white;

                    // 초록 강아지
                    int greenDogPos = GetRandomUnused(usedInThisFrame);
                    usedInThisFrame.Add(greenDogPos);
                    fakePositions.Add(greenDogPos);
                    leftCells[greenDogPos].sprite = greenDogSprite;
                    leftCells[greenDogPos].enabled = true;
                    leftCells[greenDogPos].color = Color.white;
                }
                // 2~4단계: 각 순서마다 1개씩 랜덤 타입으로 표시
                else
                {
                    int fakePos = GetRandomUnused(usedInThisFrame);
                    fakePositions.Add(fakePos);

                    // 50% 확률로 빨간 고양이 or 초록 강아지
                    Sprite fakeSprite = Random.value > 0.5f ? redCatSprite : greenDogSprite;
                    leftCells[fakePos].sprite = fakeSprite;
                    leftCells[fakePos].enabled = true;
                    leftCells[fakePos].color = Color.white;
                }

                fakeCounter++;
            }

            // 이미지 표시 시간 대기
            yield return new WaitForSeconds(showDuration);

            // === 이미지 숨기기 ===
            leftCells[answerPos].enabled = false;
            leftCells[answerPos].color = new Color(1, 1, 1, 0);  // 다시 투명하게

            foreach (int fakePos in fakePositions)
            {
                leftCells[fakePos].enabled = false;
                leftCells[fakePos].color = new Color(1, 1, 1, 0);  // 다시 투명하게
            }

            // 다음 이미지 표시 전 대기
            yield return new WaitForSeconds(delayBetween);
        }
    }

    // ==================== 플레이어 입력 처리 ====================

    /// <summary>
    /// 플레이어가 오른쪽 패널의 셀을 클릭했을 때 호출
    /// 정답 여부를 확인하고 그에 따른 처리 수행
    /// </summary>
    /// <param name="clickedIndex">클릭한 셀의 인덱스 (0~8)</param>
    void OnCellClick(int clickedIndex)
    {
        // 중복 입력 방지
        if (isProcessingInput) return;

        // 현재 입력해야 할 정답 위치
        int correctIndex = currentAnswer[answerIndex];

        // === 정답 확인 ===
        if (clickedIndex == correctIndex)
        {
            // 정답!
            isProcessingInput = true;
            StartCoroutine(HandleCorrectAnswer(clickedIndex));
        }
        else
        {
            // 오답!
            isProcessingInput = true;
            OnAnswerSelected?.Invoke(false);
            StartCoroutine(HandleWrongAnswer(clickedIndex));
        }
    }

    /// <summary>
    /// 정답을 클릭했을 때의 처리 코루틴
    /// </summary>
    IEnumerator HandleCorrectAnswer(int clickedIndex)
    {
        StartCoroutine(FlashCell(rightButtons[clickedIndex].GetComponent<Image>(), correctColor));
        answerIndex++;  // 다음 정답으로 넘어감
        UpdateProgressIndicator();  // 진행 상황 업데이트

        Debug.Log($"정답! ({answerIndex}/{currentAnswer.Count})");

        yield return new WaitForSeconds(feedbackDuration);

        // 이번 단계의 모든 정답을 입력했는지 확인
        if (answerIndex >= currentAnswer.Count)
        {
            yield return OnStageComplete();
        }
        else
        {
            isProcessingInput = false;  // 다음 입력 허용
        }
    }

    /// <summary>
    /// 오답을 클릭했을 때의 처리 코루틴
    /// </summary>
    IEnumerator HandleWrongAnswer(int clickedIndex)
    {
        SetRightPanelInteractable(false);  // 입력 차단
        StartCoroutine(FlashCell(rightButtons[clickedIndex].GetComponent<Image>(), wrongColor));

        yield return new WaitForSeconds(failureDuration);

        yield return OnStageFailed();
    }

    // ==================== 단계 완료/실패 처리 ====================

    /// <summary>
    /// 현재 단계를 클리어했을 때 호출되는 코루틴
    /// - 5단계 클리어 시: 게임 완료 + 타이머 중지
    /// - 그 외: 다음 단계로 진행
    /// </summary>
    IEnumerator OnStageComplete()
    {
        if (!isGameActive)
        {
            Debug.Log("성공 처리 중단됨: 시간이 초과되어 게임이 이미 비활성 상태입니다.");
            yield break; // 성공 코루틴을 즉시 종료합니다.
        }

        SetRightPanelInteractable(false);
        Debug.Log($"Stage {currentStage} 클리어!");

        yield return new WaitForSeconds(stageCompleteDuration);

        if (currentStage == 5)
        {
            // 5단계(마지막) 클리어 시: 최종 성공 신호만 보냅니다.
            isGameActive = false;
            StopTimer();
            Debug.Log("===== 모든 스테이지 클리어! 튜토리얼 성공! =====");
            OnPuzzleEnd?.Invoke(true);
        }
        else
        {
            // 1~4단계 클리어 시: '스테이지 클리어' 신호를 보냅니다.
            OnStageCleared?.Invoke();

            currentStage++;
            UpdateStageIndicator();
            StartCoroutine(PlayStage());
        }
    }

    /// <summary>
    /// 오답을 입력했을 때 호출되는 코루틴
    /// 1단계부터 다시 시작 (타이머는 계속 실행)
    /// </summary>
    IEnumerator OnStageFailed()
    {
        Debug.Log("오답! 1단계부터 재시작");

        // 1단계로 리셋 (타이머는 계속 실행)
        currentStage = 1;
        UpdateStageIndicator();
        StartCoroutine(PlayStage());
        yield break;
    }

    // ==================== 타이머 시스템 ====================

    /// <summary>
    /// 타이머 UI 초기화
    /// 모든 신호등과 텍스트를 숨김
    /// </summary>
    void InitializeTimer()
    {
        topLight.enabled = false;
        middleLight.enabled = false;
        timeText.enabled = false;
    }

    /// <summary>
    /// 타이머 시작
    /// 빨간 신호 + 시간 텍스트 표시
    /// </summary>
    void StartTimer()
    {
        // 이전 타이머가 있으면 중지
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
        }

        remainingTime = timeLimit;
        isTimerRunning = true;

        // UI 설정: 빨간 신호 ON + 시간 텍스트 표시
        topLight.enabled = true;
        middleLight.enabled = false;
        timeText.enabled = true;

        timerCoroutine = StartCoroutine(TimerCoroutine());
    }

    /// <summary>
    /// 타이머 중지
    /// 모든 타이머 UI 숨김
    /// </summary>
    void StopTimer()
    {
        isTimerRunning = false;

        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }

        // 타이머 UI 모두 숨기기
        topLight.enabled = false;
        middleLight.enabled = false;
        timeText.enabled = false;
    }

    /// <summary>
    /// 타이머 실행 코루틴
    /// 매 프레임 시간을 감소시키고 텍스트 업데이트
    /// </summary>
    IEnumerator TimerCoroutine()
    {
        while (isTimerRunning && remainingTime > 0)
        {
            remainingTime -= Time.deltaTime;

            // 시간 텍스트 업데이트 (정수로 올림)
            timeText.text = Mathf.Ceil(remainingTime).ToString("F0");

            yield return null;
        }

        // 시간 종료
        if (isTimerRunning)
        {
            yield return OnTimeUp();
        }
    }

    /// <summary>
    /// 제한 시간 종료 시 호출
    /// </summary>
    private IEnumerator OnTimeUp()
    {
        // 1. "비상 정지" 스위치를 꺼서 모든 게임 로직을 멈추도록 신호를 보냅니다.
        isGameActive = false;
        Debug.Log("⏰ 시간 종료! isGameActive를 false로 설정합니다.");

        // 2. 타이머 플래그를 끕니다.
        isTimerRunning = false;

        // 3. 버튼 입력을 즉시 차단합니다.
        SetRightPanelInteractable(false);
        Debug.Log("모든 버튼을 즉시 비활성화합니다.");

        // 4. '퍼즐 실패' 신호를 보냅니다.
        OnPuzzleEnd?.Invoke(false);

        // 5. 캔버스를 숨기기 전에 3초를 기다립니다.
        Debug.Log("3초 후 퍼즐 캔버스를 비활성화합니다.");
        yield return new WaitForSeconds(3f);

        // 6. 3초 후에 퍼즐 캔버스 전체를 비활성화합니다.
        gameObject.SetActive(false);
    }

    // ==================== UI 유틸리티 함수 ====================

    /// <summary>
    /// 양쪽 패널의 모든 셀을 초기화
    /// - 왼쪽: 이미지 숨김
    /// - 오른쪽: 색상 초기화
    /// </summary>
    void ClearAllCells()
    {
        // 왼쪽 패널 이미지들 숨기기
        foreach (var cell in leftCells)
        {
            cell.enabled = false;
            cell.color = new Color(1, 1, 1, 0);
        }

        // 오른쪽 패널 버튼들 색상 초기화
        foreach (var btn in rightButtons)
            btn.GetComponent<Image>().color = defaultColor;
    }

    /// <summary>
    /// 오른쪽 패널의 버튼 활성화/비활성화
    /// </summary>
    /// <param name="interactable">true: 클릭 가능, false: 클릭 불가</param>
    void SetRightPanelInteractable(bool interactable)
    {
        foreach (var btn in rightButtons)
            btn.interactable = interactable;
    }

    /// <summary>
    /// 상단 단계 표시 불빛 업데이트
    /// 현재 단계까지는 초록 동그라미, 나머지는 회색 동그라미로 표시
    /// </summary>
    void UpdateStageIndicator()
    {
        for (int i = 0; i < stageIndicators.Length; i++)
        {
            stageIndicators[i].sprite = i < currentStage ? litCircleSprite : unlitCircleSprite;
        }
    }

    /// <summary>
    /// 진행 상황 표시 동그라미 초기 설정
    /// 게임 시작 시 5개 모두 회색으로 보이게 설정
    /// </summary>
    void InitializeProgressIndicator()
    {
        // 5개 동그라미 모두 회색으로 초기화하고 항상 보이게
        for (int i = 0; i < progressIndicators.Length; i++)
        {
            progressIndicators[i].sprite = unlitCircleSprite;  // 회색 동그라미
            progressIndicators[i].enabled = true;  // 항상 보이게
        }
    }

    /// <summary>
    /// 오른쪽 패널의 진행 상황 표시 업데이트
    /// 항상 5개 동그라미를 보여주고, 현재 입력한 정답 개수만큼 초록색으로 표시
    /// </summary>
    void UpdateProgressIndicator()
    {
        // 5개 동그라미 모두 항상 표시
        for (int i = 0; i < progressIndicators.Length; i++)
        {
            progressIndicators[i].enabled = true;  // 무조건 항상 보이게!
            progressIndicators[i].sprite = i < answerIndex ? litCircleSprite : unlitCircleSprite;
        }
    }

    /// <summary>
    /// 특정 셀에 색상 깜빡임 효과 적용 (정답/오답 피드백용)
    /// </summary>
    /// <param name="cellImage">깜빡임을 적용할 Image 컴포넌트</param>
    /// <param name="flashColor">깜빡일 색상 (초록 or 빨강)</param>
    IEnumerator FlashCell(Image cellImage, Color flashColor)
    {
        Color original = cellImage.color;  // 원래 색상 저장
        cellImage.color = flashColor;      // 피드백 색상으로 변경
        yield return new WaitForSeconds(feedbackDuration);  // 피드백 시간 대기
        cellImage.color = original;        // 원래 색상으로 복구
    }
}