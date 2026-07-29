using UnityEngine;
using TMPro;

public class SmoothAlphaFlash : MonoBehaviour
{
    private CanvasGroup captionText;

    [SerializeField]
    private float flashDuration = 1f; // Time in seconds for one full cycle

    void Awake()
    {
        captionText = GetComponent<CanvasGroup>();
    }

    void Update()
    {
        float pulse = Mathf.PingPong(Time.time / flashDuration, 1f);

        float targetAlpha = Mathf.Lerp(0.5f, 0.875f, pulse);

        captionText.alpha = targetAlpha;
    }
}