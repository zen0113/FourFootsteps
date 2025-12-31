using UnityEngine;
using TMPro;

public class HeartRateDisplay : MonoBehaviour
{
    [Header("UI 연결")]
    public TextMeshProUGUI bpmText;

    [Header("BPM 설정")]
    public int baseBPM = 80;
    public int variance = 5;
    public float changeSpeed = 2.0f;

    private float currentDisplayBPM;
    private float targetBPM;

    void Start()
    {
        currentDisplayBPM = baseBPM;
        SetNewTarget();

        if (bpmText == null)
        {
            bpmText = GetComponent<TextMeshProUGUI>();
        }
    }

    void Update()
    {
        if (bpmText == null) return;

        currentDisplayBPM = Mathf.MoveTowards(currentDisplayBPM, targetBPM, changeSpeed * Time.deltaTime);
        bpmText.text = ((int)currentDisplayBPM).ToString();

        if (Mathf.Abs(currentDisplayBPM - targetBPM) < 0.1f)
        {
            SetNewTarget();
        }
    }

    void SetNewTarget()
    {
        targetBPM = Random.Range(baseBPM - variance, baseBPM + variance + 1);
    }

    /// <summary>
    /// 고양이가 바뀔 때 호출 - 즉시 새로운 BPM 범위로 리셋
    /// </summary>
    public void SetHeartRateData(int newBaseBPM, int newVariance)
    {
        baseBPM = newBaseBPM;
        variance = newVariance;

        // 현재 표시값을 새로운 기준값으로 즉시 변경
        currentDisplayBPM = newBaseBPM;

        // 새로운 범위 내에서 목표값 설정
        SetNewTarget();

        // 즉시 화면에 반영
        if (bpmText != null)
        {
            bpmText.text = ((int)currentDisplayBPM).ToString();
        }
    }
}