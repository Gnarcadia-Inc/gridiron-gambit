using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum PlayResultEntry
{
    Yards,
    Reception,
    Touchdown,
    FieldGoal,
    Redzone,
    ClutchFieldGoalRange,
    ClutchScore,
    Comeback,
    ClutchComeback,
    DivisionRivalryWin,
    StadiumSeriesWin,
    PlayoffWin,
    SuperBowlWin,
    Fumble,
    FirstDown,
    ThirdAndLongConversion,
    FourthDownConversion
}

public class EndScreenSequence : MonoBehaviour
{
    [Header("Main End Screen")]
    [SerializeField]
    private GameObject endScreen;

    [SerializeField]
    private Image endScreenImage;

    [Header("Intro Text")]
    [SerializeField]
    private TMP_Text resultTitleText;

    [SerializeField]
    private TMP_Text playText;

    [SerializeField]
    private TMP_Text playerText;

    [Header("Play Result Entries")]
    [SerializeField]
    private List<PlayResultEntry> playResultEntryList =
        new List<PlayResultEntry>();

    [SerializeField]
    private GameObject playResultEntryPrefab;

    [SerializeField]
    private RectTransform scrollContent;

    [SerializeField]
    private ScrollRect scrollRect;

    [Header("Total")]
    [SerializeField]
    private TMP_Text totalText;

    [SerializeField]
    private float totalRollDuration = 0.25f;

    [Header("Play Again")]
    [SerializeField]
    private Image playAgainButton;

    [SerializeField]
    private Image playAgainFillImage;

    [Header("Timing")]
    [SerializeField]
    private float endScreenFadeDuration = 0.25f;

    [SerializeField]
    private float introTextDuration = 0.75f;

    [SerializeField]
    private float entryInterval = 0.5f;

    [SerializeField]
    private float entryFadeDuration = 0.25f;

    [SerializeField]
    private float totalScaleHalfDuration = 0.125f;

    [SerializeField]
    private float playAgainButtonFadeDuration = 0.25f;

    [SerializeField]
    private float playAgainFillDuration = 4f;

    [Header("Options")]
    [SerializeField]
    private bool clearExistingEntriesBeforePlaying = true;

    /*
     * This is true only during the four-second fill countdown,
     * as requested.
     */
    public bool playAgainAvailableFlag { get; private set; }

    private Coroutine sequenceCoroutine;
    private Vector3 totalTextOriginalScale;
    private float displayedTotal;

    private void Awake()
    {
        if (totalText != null)
        {
            totalTextOriginalScale =
                totalText.rectTransform.localScale;
        }

        playAgainAvailableFlag = false;
    }

    public void PlayEndSequence()
    {
        if (sequenceCoroutine != null)
        {
            StopCoroutine(sequenceCoroutine);
        }

        sequenceCoroutine =
            StartCoroutine(EndSequenceRoutine());
    }

    public void PlayEndSequence(
        List<PlayResultEntry> entries)
    {
        playResultEntryList =
            entries != null
                ? new List<PlayResultEntry>(entries)
                : new List<PlayResultEntry>();

        PlayEndSequence();
    }

    public void StopEndSequence()
    {
        if (sequenceCoroutine != null)
        {
            StopCoroutine(sequenceCoroutine);
            sequenceCoroutine = null;
        }

        playAgainAvailableFlag = false;
    }

    private IEnumerator EndSequenceRoutine()
    {
        playAgainAvailableFlag = false;

        PrepareInitialState();

        /*
         * SECOND 0.0:
         * Activate the complete panel immediately.
         */
        endScreen.SetActive(true);

        Canvas.ForceUpdateCanvases();

        /*
         * SECONDS 0.0–0.25:
         * Fade the main end-screen image from 0 to 0.75.
         */
        yield return FadeImage(
            endScreenImage,
            0f,
            0.75f,
            endScreenFadeDuration);

        /*
         * SECONDS 0.25–1.0:
         * Fade the three intro texts sequentially.
         *
         * With a total duration of 0.75 seconds, each text
         * receives 0.25 seconds.
         */
        float individualTextDuration =
            introTextDuration / 3f;

        yield return FadeText(
            resultTitleText,
            0f,
            1f,
            individualTextDuration);

        yield return FadeText(
            playText,
            0f,
            1f,
            individualTextDuration);

        yield return FadeText(
            playerText,
            0f,
            1f,
            individualTextDuration);

        /*
         * Instantiate each result entry.
         *
         * Each entry occupies one entryInterval:
         *
         * First 0.25 seconds:
         * - Fade entry from 0 to 1.
         * - Smoothly scroll toward the bottom.
         *
         * Remaining time:
         * - Roll the total toward its next value.
         */
        for (int i = 0;
             i < playResultEntryList.Count;
             i++)
        {
            PlayResultEntry entry =
                playResultEntryList[i];

            yield return RevealEntry(entry);
        }

        /*
         * Pulse totalText to twice its normal size and back.
         */
        yield return ScaleTransform(
            totalText.rectTransform,
            totalTextOriginalScale,
            totalTextOriginalScale * 2f,
            totalScaleHalfDuration);

        yield return ScaleTransform(
            totalText.rectTransform,
            totalTextOriginalScale * 2f,
            totalTextOriginalScale,
            totalScaleHalfDuration);

        /*
         * Fade in the Play Again button.
         */
        yield return FadeImage(
            playAgainButton,
            0f,
            1f,
            playAgainButtonFadeDuration);

        /*
         * During this four-second period:
         *
         * - The fill image moves from 1 to 0.
         * - playAgainAvailableFlag remains true.
         */
        playAgainAvailableFlag = true;

        yield return AnimateImageFill(
            playAgainFillImage,
            1f,
            0f,
            playAgainFillDuration);

        playAgainAvailableFlag = false;

        sequenceCoroutine = null;
    }

    private void PrepareInitialState()
    {
        /*
         * The GameObject is activated immediately afterward.
         * Its Image alpha is the only part of the main panel
         * being faded.
         */
        SetImageAlpha(endScreenImage, 0f);

        SetTextAlpha(resultTitleText, 0f);
        SetTextAlpha(playText, 0f);
        SetTextAlpha(playerText, 0f);

        SetImageAlpha(playAgainButton, 0f);

        if (playAgainFillImage != null)
        {
            playAgainFillImage.fillAmount = 1f;
        }

        displayedTotal = 0f;

        if (totalText != null)
        {
            totalText.text =
                FormatTotal(displayedTotal);

            totalText.rectTransform.localScale =
                totalTextOriginalScale;
        }

        if (clearExistingEntriesBeforePlaying)
        {
            ClearExistingEntries();
        }

        if (scrollRect != null)
        {
            /*
             * verticalNormalizedPosition:
             *
             * 1 = top
             * 0 = bottom
             */
            scrollRect.verticalNormalizedPosition = 1f;
        }
    }

    private IEnumerator RevealEntry(
        PlayResultEntry entry)
    {
        GameObject instance =
            Instantiate(
                playResultEntryPrefab,
                scrollContent);

        /*
         * Ensure the prefab root has a CanvasGroup.
         * This affects only this instantiated entry.
         */
        CanvasGroup entryCanvasGroup =
            instance.GetComponent<CanvasGroup>();

        if (entryCanvasGroup == null)
        {
            entryCanvasGroup =
                instance.AddComponent<CanvasGroup>();
        }

        entryCanvasGroup.alpha = 0f;

        Canvas.ForceUpdateCanvases();

        if (scrollContent != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(
                scrollContent);
        }

        Canvas.ForceUpdateCanvases();

        float fadeTime =
            Mathf.Min(
                entryFadeDuration,
                entryInterval);

        float elapsed = 0f;

        float initialScrollPosition =
            scrollRect != null
                ? scrollRect.verticalNormalizedPosition
                : 0f;

        while (elapsed < fadeTime)
        {
            float deltaTime =
                Time.unscaledDeltaTime;

            elapsed += deltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed /
                    Mathf.Max(0.001f, fadeTime));

            float easedProgress =
                SmoothEaseOut(progress);

            entryCanvasGroup.alpha =
                Mathf.Lerp(
                    0f,
                    1f,
                    easedProgress);

            if (scrollRect != null)
            {
                scrollRect.verticalNormalizedPosition =
                    Mathf.Lerp(
                        initialScrollPosition,
                        0f,
                        easedProgress);
            }

            yield return null;
        }

        entryCanvasGroup.alpha = 1f;

        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 0f;
        }

        /*
         * The total starts rolling only after the entry has
         * reached full alpha.
         */
        float nextTotal =
            displayedTotal +
            GetPlayEntryValue(entry);

        float remainingInterval =
            Mathf.Max(
                0f,
                entryInterval - fadeTime);

        float rollDuration =
            Mathf.Min(
                totalRollDuration,
                remainingInterval);

        if (rollDuration > 0f)
        {
            yield return RollTotal(
                displayedTotal,
                nextTotal,
                rollDuration);
        }
        else
        {
            displayedTotal = nextTotal;
            UpdateTotalText();
        }

        /*
         * Preserve the complete 0.5-second pause between
         * instantiations if the total roll is shorter than
         * the remaining interval.
         */
        float unusedInterval =
            remainingInterval - rollDuration;

        if (unusedInterval > 0f)
        {
            yield return new WaitForSecondsRealtime(
                unusedInterval);
        }

        displayedTotal = nextTotal;
        UpdateTotalText();
    }

    private IEnumerator RollTotal(
        float startValue,
        float targetValue,
        float duration)
    {
        if (duration <= 0f)
        {
            displayedTotal = targetValue;
            UpdateTotalText();
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed / duration);

            float easedProgress =
                SmoothEaseOut(progress);

            displayedTotal =
                Mathf.Lerp(
                    startValue,
                    targetValue,
                    easedProgress);

            UpdateTotalText();

            yield return null;
        }

        displayedTotal = targetValue;
        UpdateTotalText();
    }

    private void UpdateTotalText()
    {
        if (totalText != null)
        {
            totalText.text =
                FormatTotal(displayedTotal);
        }
    }

    private static string FormatTotal(
        float value)
    {
        /*
         * Examples:
         *
         * 0      -> "0"
         * 0.1    -> "0.1"
         * 1.25   -> "1.25"
         */
        return value.ToString("0.##");
    }

    /*
     * Replace this method later with your actual scoring logic.
     */
    private float GetPlayEntryValue(
        PlayResultEntry entry)
    {
        return 0.1f;
    }

    private IEnumerator FadeImage(
        Image image,
        float startAlpha,
        float targetAlpha,
        float duration)
    {
        if (image == null)
        {
            yield break;
        }

        SetImageAlpha(image, startAlpha);

        if (duration <= 0f)
        {
            SetImageAlpha(image, targetAlpha);
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed / duration);

            SetImageAlpha(
                image,
                Mathf.Lerp(
                    startAlpha,
                    targetAlpha,
                    SmoothEaseOut(progress)));

            yield return null;
        }

        SetImageAlpha(image, targetAlpha);
    }

    private IEnumerator FadeText(
        TMP_Text text,
        float startAlpha,
        float targetAlpha,
        float duration)
    {
        if (text == null)
        {
            yield break;
        }

        text.alpha = startAlpha;

        if (duration <= 0f)
        {
            text.alpha = targetAlpha;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed / duration);

            text.alpha =
                Mathf.Lerp(
                    startAlpha,
                    targetAlpha,
                    SmoothEaseOut(progress));

            yield return null;
        }

        text.alpha = targetAlpha;
    }

    private IEnumerator ScaleTransform(
        RectTransform target,
        Vector3 startScale,
        Vector3 targetScale,
        float duration)
    {
        if (target == null)
        {
            yield break;
        }

        target.localScale = startScale;

        if (duration <= 0f)
        {
            target.localScale = targetScale;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed / duration);

            target.localScale =
                Vector3.Lerp(
                    startScale,
                    targetScale,
                    SmoothEaseOut(progress));

            yield return null;
        }

        target.localScale = targetScale;
    }

    private IEnumerator AnimateImageFill(
        Image image,
        float startFill,
        float targetFill,
        float duration)
    {
        if (image == null)
        {
            yield break;
        }

        image.fillAmount = startFill;

        if (duration <= 0f)
        {
            image.fillAmount = targetFill;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed / duration);

            /*
             * Linear interpolation usually looks best for
             * countdown timers.
             */
            image.fillAmount =
                Mathf.Lerp(
                    startFill,
                    targetFill,
                    progress);

            yield return null;
        }

        image.fillAmount = targetFill;
    }

    private void ClearExistingEntries()
    {
        if (scrollContent == null)
        {
            return;
        }

        for (int i = scrollContent.childCount - 1;
             i >= 0;
             i--)
        {
            Destroy(
                scrollContent.GetChild(i).gameObject);
        }
    }

    private static void SetImageAlpha(
        Image image,
        float alpha)
    {
        if (image == null)
        {
            return;
        }

        Color color = image.color;
        color.a = Mathf.Clamp01(alpha);
        image.color = color;
    }

    private static void SetTextAlpha(
        TMP_Text text,
        float alpha)
    {
        if (text != null)
        {
            text.alpha =
                Mathf.Clamp01(alpha);
        }
    }

    private static float SmoothEaseOut(
        float progress)
    {
        progress =
            Mathf.Clamp01(progress);

        return
            1f -
            Mathf.Pow(
                1f - progress,
                3f);
    }
}