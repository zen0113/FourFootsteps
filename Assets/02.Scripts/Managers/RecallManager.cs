using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RecallManager : MonoBehaviour
{
    // 임시임!!
    public static RecallManager Instance { get; private set; }

    [SerializeField]
    private GameObject InteractKeyGroup;


    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    // Start is called before the first frame update
    void Start()
    {
        InteractKeyGroup.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetInteractKeyGroup(bool isActive)
    {
        //if((bool)GameManager.Instance.GetVariable("CanInvesigatingRecallObject")
        //    && isActive)
        InteractKeyGroup.SetActive(isActive);
    }

}
