using TMPro;
using UnityEngine;

public class InputTextTypingPlayer : MonoBehaviour
{
    public TMP_InputField inputField;
    public AudioSource typingSound;
    public float minPitch = 0.95f;
    public float maxPitch = 1.05f;

    private void Start()
    {
        typingSound = GetComponent<AudioSource>();
        // 이름 설정씬 들어오자마자 바로 포커스해줌
        inputField.ActivateInputField();
    }

    //private int previousTextLength = 0;

    //void Update()
    //{
    //    if (inputField.isFocused)
    //    {
    //        int currentLength = inputField.text.Length;

    //        if (currentLength > previousTextLength)
    //        {
    //            typingSound.pitch = Random.Range(minPitch, maxPitch);
    //            typingSound.Play();
    //        }
    //        else if (currentLength < previousTextLength)
    //        {
    //            typingSound.pitch = Random.Range(minPitch, maxPitch);
    //            typingSound.Play();
    //        }

    //        previousTextLength = currentLength;
    //    }
    //}

    void OnGUI()
    {
        Event e = Event.current;
        if (!inputField.isFocused) return;

        // 한글 IME 입력 포함한 키 입력 감지
        if (e.type == EventType.KeyDown && !string.IsNullOrEmpty(e.character.ToString().Trim()))
        {
            typingSound.pitch = Random.Range(minPitch, maxPitch);
            typingSound.Play();
        }
    }
}