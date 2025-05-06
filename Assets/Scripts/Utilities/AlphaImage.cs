using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class AlphaImage : MonoBehaviour
{
    public float AlphaThreshold = 0.1f;

    void Start()
    {
        this.GetComponent<Image>().alphaHitTestMinimumThreshold = AlphaThreshold;
    }

}