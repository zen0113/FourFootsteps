using System.Collections;
using UnityEngine;
using TMPro; // TMP 사용을 위해 필수

public class DisplayCatName : MonoBehaviour
{
    [Header("설정")]
    [Tooltip("GameManager에서 가져올 변수 이름")]
    [SerializeField] private string variableKey = "YourCatName";

    [Tooltip("이 길이보다 길어지면 텍스트를 자르고 ...을 표시합니다.")]
    [SerializeField] private int truncateLength = 4;

    // 변경점: InputField가 아닌 TMP_Text(일반 텍스트)를 사용합니다.
    private TMP_Text targetText;

    private void Awake()
    {
        // 이 오브젝트에 붙어있는 TextMeshPro 컴포넌트를 가져옵니다.
        targetText = GetComponent<TMP_Text>();

        if (targetText == null)
        {
            Debug.LogError($"❌ [DisplayCatName] '{name}' 오브젝트에서 TextMeshPro 컴포넌트를 찾을 수 없습니다!");
        }
    }

    // 타이밍 이슈 방지를 위해 Start에서 코루틴 사용
    private IEnumerator Start()
    {
        yield return null; // 1프레임 대기 (GameManager 로드 대기)
        UpdateNameText();
    }

    private void OnEnable()
    {
        // 켜질 때마다 갱신
        UpdateNameText();
    }

    public void UpdateNameText()
    {
        if (GameManager.Instance == null) return;

        // 1. GameManager에서 이름 가져오기
        string rawName = (GameManager.Instance.GetVariable(variableKey) as string) ?? "";

        // 2. 문자열 정리 및 조사 처리
        string processedName = ProcessPlaceholders(rawName);

        // 3. 길이 체크 및 ... 처리
        if (processedName.Length > truncateLength)
        {
            processedName = processedName.Substring(0, truncateLength) + "...";
        }

        // 4. 텍스트 UI에 할당
        if (targetText != null)
        {
            targetText.text = processedName;

            // 텍스트 변경사항 즉시 반영
            targetText.SetAllDirty();
        }
    }

    private string ProcessPlaceholders(string originalString)
    {
        string modifiedString = originalString.Replace("\\n", "\n")
                                            .Replace("`", ",")
                                            .Replace("\b", ""); // 백스페이스 문자 제거

        string catNameValue = (GameManager.Instance.GetVariable("YourCatName") as string) ?? "";

        modifiedString = KoreanJosa.Apply(
            modifiedString,
            ("YourCatName", catNameValue)
        );

        return modifiedString;
    }
}