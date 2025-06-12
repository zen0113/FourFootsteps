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
    }

    private int previousTextLength = 0;

    void Update()
    {
        if (inputField.isFocused)
        {
            int currentLength = inputField.text.Length;

            if (currentLength > previousTextLength)
            {
                typingSound.pitch = Random.Range(minPitch, maxPitch);
                typingSound.Play();
            }else if(currentLength < previousTextLength)
            {
                typingSound.pitch = Random.Range(minPitch, maxPitch);
                typingSound.Play();
            }

            previousTextLength = currentLength;
        }
    }

}