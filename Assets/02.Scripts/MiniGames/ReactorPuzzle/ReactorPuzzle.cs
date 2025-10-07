using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 원자로 메모리 퍼즐 미니게임
/// 왼쪽 패널에 나타나는 초록 고양이 패턴을 기억하고
/// 오른쪽 패널에서 순서대로 클릭하는 게임
/// </summary>
public class ReactorPuzzle : MonoBehaviour
{
    // ==================== UI 레퍼런스 ====================
    
    [Header("UI References")]
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
    public float showDuration = 0.8f;
    
    [Tooltip("이미지 표시 후 다음 이미지까지의 대기 시간 (초)")]
    public float delayBetween = 0.3f;
    
    [Tooltip("제한 시간 (초)")]
    public float timeLimit = 60f;
    
    [Tooltip("시간 종료 후 초록 신호 표시 시간 (초)")]
    public float warningDuration = 2f;

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

    // ==================== 난이도 설정 ====================
    
    /// <summary>각 단계별 정답 개수 [1단계, 2단계, 3단계, 4단계, 5단계]</summary>
    private int[] answerCounts = { 2, 3, 4, 5, 5 };
    
    /// <summary>각 단계별 페이크 개수 [1단계는 0개, 2단계부터 등장]</summary>
    private int[] fakeCounts = { 0, 1, 1, 2, 2 };

    // ==================== Unity 생명주기 ====================

    /// <summary>
    /// 게임 시작 시 호출
    /// 오른쪽 패널의 버튼들에 클릭 이벤트를 연결하고 게임 시작
    /// </summary>
    void Start()
    {
        // 게임 상태 초기화 (씬 시작 시마다 처음부터)
        InitializeGame();
    }
    
    /// <summary>
    /// 게임 전체 초기화 (씬 시작 시 호출)
    /// </summary>
    void InitializeGame()
    {
        // 진행 표시 동그라미 5개 모두 초기 설정 (항상 보이게)
        InitializeProgressIndicator();
        
        // 타이머 UI 초기화
        InitializeTimer();
        
        // 오른쪽 패널의 각 버튼에 클릭 이벤트 연결
        for (int i = 0; i < rightButtons.Length; i++)
        {
            int index = i;  // 클로저 문제 방지용 로컬 변수
            rightButtons[i].onClick.AddListener(() => OnCellClick(index));
        }

        // 게임 시작 + 타이머 시작
        StartGame();
        StartTimer();
    }

    // ==================== 게임 흐름 제어 ====================

    /// <summary>
    /// 게임을 처음부터 시작
    /// 1단계부터 시작하고 단계 표시를 초기화
    /// </summary>
    void StartGame()
    {
        currentStage = 1;
        UpdateStageIndicator();
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
        UpdateProgressIndicator();  // 진행 상황 표시 초기화
        SetRightPanelInteractable(true);  // 오른쪽 패널 활성화
        
        // 타이머는 Start()에서 한 번만 시작되므로 여기서는 시작하지 않음
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
            // 5단계도 2번 나오도록
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
        // 현재 입력해야 할 정답 위치
        int correctIndex = currentAnswer[answerIndex];

        // === 정답 확인 ===
        if (clickedIndex == correctIndex)
        {
            // 정답!
            StartCoroutine(FlashCell(rightButtons[clickedIndex].GetComponent<Image>(), correctColor));
            answerIndex++;  // 다음 정답으로 넘어감
            UpdateProgressIndicator();  // 진행 상황 업데이트

            Debug.Log($"정답! ({answerIndex}/{currentAnswer.Count})");

            // 이번 단계의 모든 정답을 입력했는지 확인
            if (answerIndex >= currentAnswer.Count)
            {
                // 타이머는 계속 실행 중이므로 중지하지 않음
                StartCoroutine(OnStageComplete());
            }
        }
        else
        {
            // 오답!
            // 타이머는 계속 실행 중이므로 중지하지 않음
            StartCoroutine(FlashCell(rightButtons[clickedIndex].GetComponent<Image>(), wrongColor));
            StartCoroutine(OnStageFailed());
        }
    }

    // ==================== 단계 완료/실패 처리 ====================

    /// <summary>
    /// 현재 단계를 클리어했을 때 호출되는 코루틴
    /// - 5단계 클리어 시: 게임 완료 + 타이머 중지
    /// - 그 외: 다음 단계로 진행
    /// </summary>
    IEnumerator OnStageComplete()
    {
        SetRightPanelInteractable(false);  // 입력 차단
        Debug.Log($"Stage {currentStage} 클리어!");

        yield return new WaitForSeconds(1f);  // 잠시 대기

        // 마지막 단계(5단계)를 클리어했는지 확인
        if (currentStage >= 5)
        {
            StopTimer();  // 게임 완전 클리어 시에만 타이머 중지
            Debug.Log("===== 게임 클리어! =====");
            // TODO: 게임 완료 처리 (보상, 애니메이션 등)
        }
        else
        {
            // 다음 단계로 진행 (타이머는 계속 실행)
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
        SetRightPanelInteractable(false);  // 입력 차단
        Debug.Log("오답! 1단계부터 재시작");

        yield return new WaitForSeconds(1.5f);  // 실패 피드백 시간

        // 1단계로 리셋 (타이머는 계속 실행)
        currentStage = 1;
        UpdateStageIndicator();
        StartCoroutine(PlayStage());
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
    /// 빨간 신호 + 시간 텍스트 OFF → 초록 신호 ON → 종료 처리
    /// </summary>
    IEnumerator OnTimeUp()
    {
        isTimerRunning = false;
        SetRightPanelInteractable(false);  // 입력 차단
        
        Debug.Log("⏰ 제한 시간 종료!");
        
        // UI 변경: 빨간 신호 + 시간 텍스트 OFF, 초록 신호 ON
        topLight.enabled = false;
        timeText.enabled = false;
        
        middleLight.enabled = true;
        
        // 초록 신호 표시 시간 대기
        yield return new WaitForSeconds(warningDuration);
        
        // 초록 신호 OFF
        middleLight.enabled = false;
        
        // 제한 시간 종료 처리 함수 호출
        OnTimeLimitExpired();
    }

    /// <summary>
    /// 제한 시간 종료 처리
    /// 1단계부터 재시작
    /// </summary>
    void OnTimeLimitExpired()
    {
        Debug.Log("🚨 제한 시간 종료! 게임 오버.");
        
        // TODO: 게임 오버 처리 (팝업, 재시작 버튼 등)
        // 현재는 로그만 출력
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
            if (i < currentStage)
            {
                // 완료된 단계 - 초록 동그라미 스프라이트 적용
                stageIndicators[i].sprite = litCircleSprite;
            }
            else
            {
                // 미완료 단계 - 회색 동그라미 스프라이트 적용
                stageIndicators[i].sprite = unlitCircleSprite;
            }
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

            // 현재 입력한 개수보다 작으면 초록색
            if (i < answerIndex)
            {
                // 이미 입력한 정답 - 초록 동그라미
                progressIndicators[i].sprite = litCircleSprite;
            }
            else
            {
                // 아직 입력하지 않은 정답 - 회색 동그라미
                progressIndicators[i].sprite = unlitCircleSprite;
            }
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
        yield return new WaitForSeconds(0.3f);  // 0.3초 대기
        cellImage.color = original;        // 원래 색상으로 복구
    }
}