using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Text.RegularExpressions;

public enum PlayResultEntry
{
    Yards,
    Reception,
    Touchdown,
    FieldGoal,
    Redzone,
    ClutchFieldGoalRange,
    Clutch,
    Comeback,
    ClutchComeback,
    DivisionRivalry,
    StadiumSeries,
    Playoffs,
    SuperBowl,
    Fumble,
    FirstDown,
    ThirdAndLongConversion,
    FourthDownConversion,
    Scramble
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
    private float calculatedTotal;

    [SerializeField]
    private FootballGameSetupController gameSetupController;

    [SerializeField]
    private RosterManager rosterManager;

    [SerializeField]
    private BetManager betManager;

    private void Awake()
    {
        if (totalText != null)
        {
            totalTextOriginalScale =
                totalText.rectTransform.localScale;
        }

        playAgainAvailableFlag = false;
    }

    public void PlayEndSequence(FootballPlayOutcome playOutcome)
    {
        if (sequenceCoroutine != null)
        {
            StopCoroutine(sequenceCoroutine);
        }

        sequenceCoroutine =
            StartCoroutine(EndSequenceRoutine(playOutcome));
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

    private IEnumerator EndSequenceRoutine(FootballPlayOutcome playOutcome)
    {
        playAgainAvailableFlag = false;

        PrepareInitialState(playOutcome);

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

            yield return RevealEntry(entry, playOutcome.yards);
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

        //REPLACE THIS WITH JUST GETTING THE PLAYERS LIVE BALANCE
        betManager.IncrementBalance(displayedTotal);

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

    private void PrepareInitialState(FootballPlayOutcome playOutcome)
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
            totalText.text = "$" + displayedTotal.ToString("F2");

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

        playResultEntryList.Clear();
        //USE DETERMINISTIC VALUES AND CHECK THEM AGAINST VISUAL VALUES IN THE FUTURE
        switch (playOutcome.result)
        {
            case FootballPlayResult.Interception:
                resultTitleText.text = "PICKED OFF";
                playerText.text = rosterManager.GetPlayer(playOutcome.opponentTeam, RosterPosition.DB);
                playText.text = playOutcome.yards.ToString("F0") + " YD INT";

                //playResultEntryList.Add();
                break;
            case FootballPlayResult.Incompletion:
                resultTitleText.text = "INCOMPLETE";
                playerText.text = rosterManager.GetPlayer(playOutcome.opponentTeam, RosterPosition.WRB);
                playText.text = "INTENDED FOR";

                //playResultEntryList.Add();
                break;
            case FootballPlayResult.Sack:
                resultTitleText.text = "SACKED";
                playerText.text = rosterManager.GetPlayer(playOutcome.opponentTeam, RosterPosition.DL);
                playText.text = playOutcome.yards.ToString("F0") + " YD SACK";

                //playResultEntryList.Add();
                break;
            case FootballPlayResult.Tackle:
                if (playOutcome.wasPass)
                {
                    resultTitleText.text = "NICE GRAB!";
                    playText.text = playOutcome.yards.ToString("F0") + " YD REC";

                    playResultEntryList.Add(PlayResultEntry.Reception);
                }
                else if (playOutcome.wasRun)
                {
                    resultTitleText.text = "GOOD RUN!";
                    playText.text = playOutcome.yards.ToString("F0") + " YD RUN";
                }
                else if (playOutcome.wasScramble)
                {
                    resultTitleText.text = "GOOD SCRAMBLE!";
                    playText.text = playOutcome.yards.ToString("F0") + " YD RUN";

                    playResultEntryList.Add(PlayResultEntry.Scramble);
                }

                RosterPosition tackledPosition = rosterManager.ConvertOffensiveRoleToRosterPosition(playOutcome.ballCarrierRole);
                playerText.text = rosterManager.GetPlayer(playOutcome.playerTeam, tackledPosition);

                playResultEntryList.Add(PlayResultEntry.Yards);

                if (playOutcome.yards >= gameSetupController.CurrentSituation.yardsToGo)
                {
                    playResultEntryList.Add(PlayResultEntry.FirstDown);

                    if (gameSetupController.CurrentSituation.down == 4)
                    {
                        playResultEntryList.Add(PlayResultEntry.FourthDownConversion);
                    }
                    else if (gameSetupController.CurrentSituation.down == 3 && gameSetupController.CurrentSituation.yardsToGo >= 7)
                    {
                        playResultEntryList.Add(PlayResultEntry.ThirdAndLongConversion);
                    }
                }

                if (gameSetupController.CurrentSituation.quarter == 4 && gameSetupController.CurrentSituation.secondsRemaining <= 300f)
                {
                    playResultEntryList.Add(PlayResultEntry.Clutch);
                }

                if (gameSetupController.CurrentSituation.yardsFromOwnGoal < 80f && gameSetupController.CurrentSituation.yardsFromOwnGoal + playOutcome.yards >= 80f)
                {
                    playResultEntryList.Add(PlayResultEntry.Redzone);
                }
                
                switch (gameSetupController.CurrentSituation.rivalry)
                {
                    case RivalryType.DivisionRivalry:
                        playResultEntryList.Add(PlayResultEntry.DivisionRivalry);
                        break;
                    case RivalryType.StadiumSeries:
                        playResultEntryList.Add(PlayResultEntry.StadiumSeries);
                        break;
                    case RivalryType.Playoffs:
                        playResultEntryList.Add(PlayResultEntry.Playoffs);
                        break;
                    case RivalryType.SuperBowl:
                        playResultEntryList.Add(PlayResultEntry.SuperBowl);
                        break;
                }

                break;
            case FootballPlayResult.Touchdown:
                resultTitleText.text = "TOUCHDOWN!";
                RosterPosition touchdownPosition = rosterManager.ConvertOffensiveRoleToRosterPosition(playOutcome.ballCarrierRole);
                playerText.text = rosterManager.GetPlayer(playOutcome.playerTeam, touchdownPosition);
                playText.text = playOutcome.yards.ToString("F0") + " YD TD";


                if (playOutcome.wasPass)
                {
                    playResultEntryList.Add(PlayResultEntry.Reception);
                }
                else if (playOutcome.wasScramble)
                {
                    playResultEntryList.Add(PlayResultEntry.Scramble);
                }

                playResultEntryList.Add(PlayResultEntry.Yards);

                playResultEntryList.Add(PlayResultEntry.Touchdown);

                bool clutchFlag = false;
                bool comebackFlag = false;

                if (gameSetupController.CurrentSituation.quarter == 4 && gameSetupController.CurrentSituation.secondsRemaining <= 300f)
                {
                    clutchFlag = true;
                }

                if (gameSetupController.CurrentSituation.opponentScore - gameSetupController.CurrentSituation.playerScore <= 7)
                {
                    comebackFlag = true;
                }

                if (!clutchFlag && comebackFlag)
                {
                    playResultEntryList.Add(PlayResultEntry.Comeback);
                }
                else if (clutchFlag && !comebackFlag)
                {
                    playResultEntryList.Add(PlayResultEntry.Clutch);
                }
                else if (clutchFlag && comebackFlag)
                {
                    playResultEntryList.Add(PlayResultEntry.ClutchComeback);
                }

                if (gameSetupController.CurrentSituation.down == 4)
                {
                    playResultEntryList.Add(PlayResultEntry.FourthDownConversion);
                }
                else if (gameSetupController.CurrentSituation.down == 3 && gameSetupController.CurrentSituation.yardsToGo >= 7)
                {
                    playResultEntryList.Add(PlayResultEntry.ThirdAndLongConversion);
                }

                switch (gameSetupController.CurrentSituation.rivalry)
                {
                    case RivalryType.DivisionRivalry:
                        playResultEntryList.Add(PlayResultEntry.DivisionRivalry);
                        break;
                    case RivalryType.StadiumSeries:
                        playResultEntryList.Add(PlayResultEntry.StadiumSeries);
                        break;
                    case RivalryType.Playoffs:
                        playResultEntryList.Add(PlayResultEntry.Playoffs);
                        break;
                    case RivalryType.SuperBowl:
                        playResultEntryList.Add(PlayResultEntry.SuperBowl);
                        break;
                }
                
                break;
        }

    }

    private IEnumerator RevealEntry(PlayResultEntry entry, float value = 1f)
    {
        GameObject instance =
            Instantiate(
                playResultEntryPrefab,
                scrollContent);

        (float, bool) playEntryValue = GetPlayEntryValue(entry, value);
        if (playEntryValue.Item1 >= 0)
        {
            if (!playEntryValue.Item2)
            {
                playResultEntryPrefab.GetComponent<TextMeshProUGUI>().text = "+ $" + playEntryValue.Item1.ToString("F2") + ToSpacedString(entry);
            }
            else
            {
                playResultEntryPrefab.GetComponent<TextMeshProUGUI>().text = "x $" + playEntryValue.Item1.ToString("F2") + ToSpacedString(entry);
            }
        }
        else
        {
            playResultEntryPrefab.GetComponent<TextMeshProUGUI>().text = "- $" + playEntryValue.Item1.ToString("F2") + ToSpacedString(entry);
        }


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
        float nextTotal = 0f;
        if (!playEntryValue.Item2)
        {
            nextTotal = displayedTotal + playEntryValue.Item1;
        }
        else
        {
            nextTotal = displayedTotal * playEntryValue.Item1;
        }

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

    public string ToSpacedString(PlayResultEntry enumValue)
    {
        string name = enumValue.ToString();
        return Regex.Replace(name, @"(?<=[a-z])([A-Z])", " $1", RegexOptions.Compiled);
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
            totalText.text = "$" + displayedTotal.ToString("F2");
        }
    }

    /*
     * Replace this method later with your actual scoring logic.
     */
    private (float, bool) GetPlayEntryValue(PlayResultEntry entry, float value)
    {
        float betMulti = betManager.CurrentBet;
        float entryPoints = 0f;

        bool bonusFlag = false;

        switch (entry)
        {
            case PlayResultEntry.Reception:

                entryPoints = 0.5f * betMulti;
                break;
            case PlayResultEntry.Redzone:

                entryPoints = 0.65f * betMulti;
                break;
            case PlayResultEntry.Scramble:

                entryPoints = 0.575f * betMulti;
                break;
            case PlayResultEntry.Touchdown:

                entryPoints = 3f * betMulti;
                break;
            case PlayResultEntry.Yards:

                entryPoints = 0.05f * value * betMulti;
                break;
            case PlayResultEntry.Clutch:

                entryPoints = 1.75f * betMulti; //COULD MAKE THIS EFFECTED BY HOW MUCH TIME IS LEFT
                break;
            case PlayResultEntry.ClutchComeback:

                entryPoints = 7.5f * betMulti;
                break;
            case PlayResultEntry.Comeback:

                entryPoints = 2.25f * betMulti; //COULD MAKE THIS EFFECTED BY HOW BIG THE COMEBACK IS
                break;
            case PlayResultEntry.FirstDown:

                entryPoints = 0.3f * betMulti;
                break;
            case PlayResultEntry.FourthDownConversion:

                entryPoints = 1.5f * betMulti;
                break;
            case PlayResultEntry.ThirdAndLongConversion:

                entryPoints = 0.75f * betMulti;
                break;
            case PlayResultEntry.DivisionRivalry:

                entryPoints = 2f * betMulti;
                bonusFlag = true;
                break;
            case PlayResultEntry.StadiumSeries:

                entryPoints = 1.5f;
                bonusFlag = true;
                break;
            case PlayResultEntry.Playoffs:

                entryPoints = 4f;
                bonusFlag = true;
                break;
            case PlayResultEntry.SuperBowl:

                entryPoints = 8f;
                bonusFlag = true;
                break;
        }

        return (entryPoints, bonusFlag);
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

            image.fillAmount =
                Mathf.Lerp(
                    startFill,
                    targetFill,
                    progress);

            yield return null;
        }

        image.fillAmount = targetFill;

        gameSetupController.ReturnToMainMenu();
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