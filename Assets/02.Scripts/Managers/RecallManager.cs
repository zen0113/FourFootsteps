using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RecallManager : MonoBehaviour
{
    public static RecallManager Instance { get; private set; }

    [SerializeField]
    private GameObject InteractKeyGroup;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // RecallManager가 존재하는 씬은 회상 씬으로 설정
            GameManager.Instance.SetVariable("isRecalling", true);
            SaveManager.Instance.SaveGameData();
        }
        else
            Destroy(gameObject);
    }

    void Start()
    {
        InteractKeyGroup.SetActive(false);
    }

    void OnDestroy()
    {
        // RecallManager가 파괴될 때 회상 상태 해제
        if (Instance == this && GameManager.Instance != null)
        {
            GameManager.Instance.SetVariable("isRecalling", false);
            GameManager.Instance.SetVariable("CanInvesigatingRecallObject", false);
            SaveManager.Instance.SaveGameData();
            Instance = null;
        }
    }

    // 회상 속에서 선택지 선택 후 행동 전까지는 조사 오브젝트 조사 못하게 제한함.
    public void SetInteractKeyGroup(bool isActive)
    {
        // 조사 가능한 상태일 때만 InteractKeyGroup 활성화
        if ((bool)GameManager.Instance.GetVariable("CanInvesigatingRecallObject") && isActive)
        {
            InteractKeyGroup.SetActive(true);
        }
        else
        {
            InteractKeyGroup.SetActive(false);
        }
    }

    // 회상 오브젝트 조사 가능 상태 설정
    public void SetCanInvestigateRecallObject(bool canInvestigate)
    {
        GameManager.Instance.SetVariable("CanInvesigatingRecallObject", canInvestigate);
        SaveManager.Instance.SaveGameData();

        // 조사 가능 상태가 변경되면 InteractKeyGroup도 업데이트
        SetInteractKeyGroup(canInvestigate);
    }
}