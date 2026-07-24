using System.Collections;
using UnityEngine;

public class SlidingUIObject : MonoBehaviour
{
    [SerializeField]
    private RectTransform rectTransform;

    [Header("Positions")]

    [SerializeField]
    private Vector2 hiddenAnchoredPosition;

    [SerializeField]
    private Vector2 shownAnchoredPosition;

    [Header("Animation")]

    [SerializeField]
    private float duration = 0.45f;

    [SerializeField]
    private AnimationCurve movementCurve =
        AnimationCurve.EaseInOut(
            0f,
            0f,
            1f,
            1f);

    private Coroutine movementCoroutine;

    private void Reset()
    {
        rectTransform =
            GetComponent<RectTransform>();
    }

    private void Awake()
    {
        if (rectTransform == null)
        {
            rectTransform =
                GetComponent<RectTransform>();
        }

        rectTransform.anchoredPosition =
            hiddenAnchoredPosition;
    }

    public void Show()
    {
        AnimateTo(
            shownAnchoredPosition);
    }

    public void Hide()
    {
        AnimateTo(
            hiddenAnchoredPosition);
    }

    public IEnumerator ShowAndWait()
    {
        yield return AnimateRoutine(
            shownAnchoredPosition);
    }

    public IEnumerator HideAndWait()
    {
        yield return AnimateRoutine(
            hiddenAnchoredPosition);
    }

    private void AnimateTo(
        Vector2 target)
    {
        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
        }

        movementCoroutine =
            StartCoroutine(
                AnimateRoutine(target));
    }

    private IEnumerator AnimateRoutine(
        Vector2 target)
    {
        Vector2 start =
            rectTransform.anchoredPosition;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float normalized =
                Mathf.Clamp01(
                    elapsed / duration);

            float curved =
                movementCurve.Evaluate(
                    normalized);

            rectTransform.anchoredPosition =
                Vector2.LerpUnclamped(
                    start,
                    target,
                    curved);

            yield return null;
        }

        rectTransform.anchoredPosition =
            target;

        movementCoroutine = null;
    }
}