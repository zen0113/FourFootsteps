using UnityEngine;
using System;

public class PuzzleInteraction : MonoBehaviour
{
    public event Action OnInteract;

    // 이 스크립트가 붙어있는 오브젝트의 Trigger 안에서 E키를 누르면 OnInteract 이벤트 발생
    private bool canInteract = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            canInteract = true;
            // TODO: 여기에 "E키를 눌러 상호작용" 같은 UI를 띄워주는 로직 추가 가능
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            canInteract = false;
            // TODO: 상호작용 UI 숨기는 로직 추가 가능
        }
    }

    private void Update()
    {
        if (canInteract && Input.GetKeyDown(KeyCode.E))
        {
            OnInteract?.Invoke();
        }
    }
}