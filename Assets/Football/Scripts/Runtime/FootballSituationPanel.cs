using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum RivalryType
{
    DivisionRivalry,
    StadiumSeries,
    Playoffs,
    SuperBowl,
    None
}

public class FootballSituationPanel : MonoBehaviour
{
    [Header("Situation Text")]

    [SerializeField]
    private TMP_Text playerText;

    [SerializeField]
    private TMP_Text opponentText;

    [SerializeField]
    private Image playerTeamLogo;

    [SerializeField]
    private Image opponentTeamLogo;

    [SerializeField]
    private TMP_Text playerScoreText;

    [SerializeField]
    private TMP_Text oppScoreText;

    [SerializeField]
    private TMP_Text quarterText;

    [SerializeField]
    private TMP_Text clockText;

    [SerializeField]
    private TMP_Text downText;

    [SerializeField]
    private TMP_Text yardsToGoText;

    [SerializeField]
    private TMP_Text yardLineText;

    [Header("Animation")]

    [SerializeField]
    private float totalRevealSeconds = 4f;

    [SerializeField]
    private float rollingUpdateInterval = 0.06f;

    private Coroutine revealCoroutine;

    [SerializeField]
    private float preRevealSeconds = 2f;

    [SerializeField]
    private float textFadeDuration = 0.25f;

    [SerializeField]
    private float finalizationStartDelay = 3f;

    [SerializeField]
    private float finalizationEndDelay = 3f;

    [SerializeField]
    [Range(0f, 1f)]
    private float initialTextAlpha = 0.65f;

    private TMP_Text[] animatedTexts;

    [SerializeField]
    private Image rivalryBonusImage;

    [SerializeField]
    private Sprite divisionRivalrySprite;

    [SerializeField]
    private Sprite stadiumSeriesSprite;

    [SerializeField]
    private Sprite playoffsSprite;

    [SerializeField]
    private Sprite superBowlSprite;


    [SerializeField]
    private Image comebackBonusImage;

    [SerializeField]
    private Sprite comebackOnSprite;

    [SerializeField]
    private Sprite comebackOffSprite;


    [SerializeField]
    private Image clutchTimeBonusImage;

    [SerializeField]
    private Sprite clutchTimeOnSprite;

    [SerializeField]
    private Sprite clutchTimeOffSprite;


    [SerializeField]
    private Image downBonusImage;

    [SerializeField]
    private Sprite fourthDownSprite;

    [SerializeField]
    private Sprite thirdAndLongSprite;


    private void Awake()
    {
        animatedTexts = new TMP_Text[]
        {
            playerText,
            opponentText,
            playerScoreText,
            oppScoreText,
            quarterText,
            clockText,
            downText,
            yardsToGoText,
            yardLineText
        };

        SetAllTextAlpha(initialTextAlpha);
    }

    public void ResetForNewSituation()
    {
        StopAllCoroutines();

        ClearBonuses();
    }

    public void Reveal(
        FootballGameSituation situation,
        FootballTeamDefinition[] teamBank,
        Action onFinished)
    {
        if (revealCoroutine != null)
        {
            StopCoroutine(revealCoroutine);
        }

        gameObject.SetActive(true);

        revealCoroutine =
            StartCoroutine(
                RevealRoutine(
                    situation,
                    teamBank,
                    onFinished));
    }

    public void Hide()
    {
        if (revealCoroutine != null)
        {
            StopCoroutine(revealCoroutine);
            revealCoroutine = null;
        }

        SetAllTextAlpha(0f);
        gameObject.SetActive(false);
    }

    private IEnumerator RevealRoutine(
    FootballGameSituation situation,
    FootballTeamDefinition[] teamBank,
    Action onFinished)
    {
        const int fieldCount = 7;

        float intervalBetweenStops =
            totalRevealSeconds / fieldCount;

        SetAllTextAlpha(0f);

        float elapsed = 0f;
        float nextRollUpdate = 0f;
        int stoppedFields = 0;

        /*
         * Total duration now includes:
         *
         * 1. The initial rolling delay.
         * 2. The normal sequential finalization period.
         */
        float fullSequenceDuration =
            finalizationStartDelay +
            totalRevealSeconds;

        while (elapsed < fullSequenceDuration)
        {
            float deltaTime = Time.unscaledDeltaTime;

            elapsed += deltaTime;
            nextRollUpdate -= deltaTime;

            if (nextRollUpdate <= 0f)
            {
                nextRollUpdate = rollingUpdateInterval;

                UpdateRollingFields(
                    stoppedFields,
                    situation,
                    teamBank);
            }

            /*
             * Fade each text group in one second before its own
             * finalization time.
             *
             * finalizationStartDelay pushes every field's
             * finalization and fade time forward.
             */
            for (int fieldIndex = 0;
                 fieldIndex < fieldCount;
                 fieldIndex++)
            {
                float finalValueTime =
                    finalizationStartDelay +
                    intervalBetweenStops *
                    (fieldIndex + 1);

                float fadeStartTime =
                    finalValueTime - 1f;

                float fadeProgress =
                    Mathf.InverseLerp(
                        fadeStartTime,
                        fadeStartTime + 0.25f,
                        elapsed);

                float alpha = Mathf.Lerp(initialTextAlpha, 1f, SmoothEaseOut(fadeProgress));

                SetFieldTextAlpha(fieldIndex, alpha);
            }

            /*
             * No fields finalize until finalizationStartDelay
             * has fully elapsed.
             */
            float finalizationElapsed =
                Mathf.Max(
                    0f,
                    elapsed - finalizationStartDelay);

            int shouldBeStopped =
                Mathf.Clamp(
                    Mathf.FloorToInt(
                        finalizationElapsed /
                        intervalBetweenStops),
                    0,
                    fieldCount);

            while (stoppedFields < shouldBeStopped)
            {
                SetFinalField(
                    stoppedFields,
                    situation);

                SetFieldTextAlpha(
                    stoppedFields,
                    1f);

                stoppedFields++;
            }

            yield return null;
        }

        // Guarantee exact final values at the end.
        while (stoppedFields < fieldCount)
        {
            SetFinalField(
                stoppedFields,
                situation);

            SetFieldTextAlpha(
                stoppedFields,
                1f);

            stoppedFields++;
        }

        yield return new WaitForSeconds(finalizationEndDelay);

        revealCoroutine = null;
        onFinished?.Invoke();
    }

    private void SetAllTextAlpha(float alpha)
    {
        SetTextAlpha(playerText, alpha);
        SetTextAlpha(opponentText, alpha);

        SetTextAlpha(playerScoreText, alpha);
        SetTextAlpha(oppScoreText, alpha);

        SetTextAlpha(quarterText, alpha);
        SetTextAlpha(clockText, alpha);
        SetTextAlpha(downText, alpha);
        SetTextAlpha(yardsToGoText, alpha);
        SetTextAlpha(yardLineText, alpha);
    }

    private static void SetTextAlpha(
        TMP_Text text,
        float alpha)
    {
        if (text != null)
        {
            /*
             * TMP_Text.alpha changes only this text component.
             * It does not fade the panel, logos, or other UI.
             */
            text.alpha = Mathf.Clamp01(alpha);
        }
    }

    private static float SmoothEaseOut(float t)
    {
        t = Mathf.Clamp01(t);

        return 1f - Mathf.Pow(1f - t, 3f);
    }

    private void SetFieldTextAlpha(
    int fieldIndex,
    float alpha)
    {
        alpha = Mathf.Clamp01(alpha);

        switch (fieldIndex)
        {
            /*
             * Opponent/team field.
             *
             * Both playerText and opponentText belong to
             * the first finalized field.
             */
            case 0:
                SetTextAlpha(playerText, alpha);
                SetTextAlpha(opponentText, alpha);
                break;

            // Score field.
            case 1:
                SetTextAlpha(playerScoreText, alpha);
                SetTextAlpha(oppScoreText, alpha);
                break;

            // Quarter field.
            case 2:
                SetTextAlpha(quarterText, alpha);
                break;

            // Clock field.
            case 3:
                SetTextAlpha(clockText, alpha);
                break;

            // Down field.
            case 4:
                SetTextAlpha(downText, alpha);
                break;

            // Yards-to-go field.
            case 5:
                SetTextAlpha(yardsToGoText, alpha);
                break;

            // Yard-line field.
            case 6:
                SetTextAlpha(yardLineText, alpha);
                break;
        }
    }

    private void UpdateRollingFields(
        int stoppedFields,
        FootballGameSituation finalSituation,
        FootballTeamDefinition[] teamBank)
    {
        if (stoppedFields <= 0)
        {
            playerText.text = finalSituation.playerTeam.abbreviation;

            playerTeamLogo.sprite = finalSituation.playerTeam.menuLogo;

            FootballTeamDefinition team = GetRandomTeam(
                    finalSituation.playerTeam,
                    teamBank);

            opponentText.text = team.abbreviation;

            opponentTeamLogo.sprite = team.menuLogo;
        }

        if (stoppedFields <= 1)
        {
            int randomPlayerScore =
                FootballSituationGenerator
                    .RandomPossibleScore(
                        finalSituation.quarter);

            int randomOpponentScore =
                FootballSituationGenerator
                    .RandomPossibleScore(
                        finalSituation.quarter);

            playerScoreText.text = $"{randomPlayerScore}";

            oppScoreText.text = $"{randomOpponentScore}";
        }

        if (stoppedFields <= 2)
        {
            int randomQuarter = UnityEngine.Random.Range(1, 5);

            quarterText.text =
                $"{randomQuarter}" +
                GetOrdinalSuffix(randomQuarter);
        }

        if (stoppedFields <= 3)
        {
            int randomSeconds =
                UnityEngine.Random.Range(0, 901);

            clockText.text =
                $"{randomSeconds / 60}:" +
                $"{randomSeconds % 60:00}";
        }

        if (stoppedFields <= 4)
        {
            int randomDown =
                UnityEngine.Random.Range(1, 5);

            downText.text =
                $"{randomDown}" +
                GetOrdinalSuffix(randomDown);
        }

        if (stoppedFields <= 5)
        {
            yardsToGoText.text =
                UnityEngine.Random.Range(1, 21).ToString();
        }

        if (stoppedFields <= 6)
        {
            int randomPosition =
                UnityEngine.Random.Range(1, 100);

            yardLineText.text =
                FormatRandomYardLine(
                    randomPosition,
                    finalSituation);
        }
    }

    private void SetFinalField(
        int index,
        FootballGameSituation situation)
    {
        switch (index)
        {
            case 0:
                playerText.text = situation.playerTeam.abbreviation;
                playerTeamLogo.sprite = situation.playerTeam.menuLogo;

                opponentText.text = situation.opponentTeam.abbreviation;
                opponentTeamLogo.sprite = situation.opponentTeam.menuLogo;

                CheckForRivalry(situation);
                break;

            case 1:
                playerScoreText.text = $"{situation.playerScore}";
                oppScoreText.text = $"{situation.opponentScore}";

                CheckForComeback(situation);
                break;

            case 2:
                quarterText.text = situation.QuarterText;
                break;

            case 3:
                clockText.text = situation.ClockText;

                CheckForClutchTime(situation);
                break;

            case 4:
                downText.text = situation.DownText;

                CheckForFourthDown(situation);
                break;

            case 5:
                yardsToGoText.text = situation.yardsToGo.ToString();

                CheckForThirdAndLong(situation);
                break;

            case 6:
                yardLineText.text = situation.YardLineText;

                CheckForRedZone(situation);
                break;
        }
    }

    private static FootballTeamDefinition GetRandomTeam(FootballTeamDefinition playerTeam, FootballTeamDefinition[] teamBank)
    {
        if (teamBank == null || teamBank.Length == 0)
        {
            return playerTeam;
        }

        for (int attempt = 0; attempt < 10; attempt++)
        {
            FootballTeamDefinition randomTeam =
                teamBank[
                    UnityEngine.Random.Range(
                        0,
                        teamBank.Length)];

            if (randomTeam != null &&
                randomTeam != playerTeam)
            {
                return randomTeam;
            }
        }

        return playerTeam;
    }

    private static string FormatRandomYardLine(
        int yardsFromOwnGoal,
        FootballGameSituation situation)
    {
        if (yardsFromOwnGoal == 50)
        {
            return "50";
        }

        if (yardsFromOwnGoal < 50)
        {
            return
                $"{situation.playerTeam.abbreviation} " +
                $"{yardsFromOwnGoal}";
        }

        return
            $"{situation.opponentTeam.abbreviation} " +
            $"{100 - yardsFromOwnGoal}";
    }

    private static string GetOrdinalSuffix(
        int value)
    {
        return value switch
        {
            1 => "st",
            2 => "nd",
            3 => "rd",
            _ => "th"
        };
    }

    private void CheckForRivalry(FootballGameSituation situation)
    {
        SetRivalryBonus(situation.rivalry);
    }

    private void SetRivalryBonus(RivalryType rivalry)
    {
        if (rivalry != RivalryType.None)
        {
            StartCoroutine(ShowRivalryBonusRoutine(rivalry));
        }
    }

    private IEnumerator ShowRivalryBonusRoutine(RivalryType rivalry)
    {
        switch (rivalry)
        {
            case RivalryType.DivisionRivalry:
                rivalryBonusImage.sprite = divisionRivalrySprite;
                break;
            case RivalryType.StadiumSeries:
                rivalryBonusImage.sprite = stadiumSeriesSprite;
                break;
            case RivalryType.Playoffs:
                rivalryBonusImage.sprite = playoffsSprite;
                break;
            case RivalryType.SuperBowl:
                rivalryBonusImage.sprite = superBowlSprite;
                break;
        }


        float elapsed = 0f;
        float duration = 0.25f;

        Color color = rivalryBonusImage.color;
        color.a = 0f;
        rivalryBonusImage.color = color;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed / duration);

            color.a = Mathf.Lerp(
                    0f,
                    1f,
                    progress);
            rivalryBonusImage.color = color;

            yield return null;
        }

        color.a = 1f;
        rivalryBonusImage.color = color;
    }


    private void CheckForComeback(FootballGameSituation situation)
    {
        if (situation.opponentScore - situation.playerScore <= 7)
        {
            StartCoroutine(ShowComebackBonusRoutine());
        }
    }

    private IEnumerator ShowComebackBonusRoutine()
    {
        float elapsed = 0f;
        float duration = 0.25f;

        Color color = comebackBonusImage.color;
        color.a = 0.25f;
        comebackBonusImage.color = color;
        comebackBonusImage.sprite = comebackOnSprite;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed / duration);

            color.a = Mathf.Lerp(
                    0.25f,
                    1f,
                    progress);
            comebackBonusImage.color = color;

            yield return null;
        }

        color.a = 1f;
        comebackBonusImage.color = color;
    }

    private void CheckForClutchTime(FootballGameSituation situation)
    {
        if (situation.quarter == 4 && situation.secondsRemaining < 300f)
        {
            StartCoroutine(ShowClutchTimeBonusRoutine());
        }
    }

    private IEnumerator ShowClutchTimeBonusRoutine()
    {
        float elapsed = 0f;
        float duration = 0.25f;

        Color color = clutchTimeBonusImage.color;
        color.a = 0.25f;
        clutchTimeBonusImage.color = color;
        clutchTimeBonusImage.sprite = clutchTimeOnSprite;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed / duration);

            color.a = Mathf.Lerp(
                    0.25f,
                    1f,
                    progress);
            clutchTimeBonusImage.color = color;

            yield return null;
        }

        color.a = 1f;
        clutchTimeBonusImage.color = color;
    }

    private void CheckForFourthDown(FootballGameSituation situation)
    {
        if (situation.down == 4)
        {
            StartCoroutine(ShowFourthDownBonusRoutine());
        }
    }

    private IEnumerator ShowFourthDownBonusRoutine()
    {
        float elapsed = 0f;
        float duration = 0.25f;

        Color color = downBonusImage.color;
        color.a = 0.25f;
        downBonusImage.color = color;
        downBonusImage.sprite = fourthDownSprite;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed / duration);

            color.a = Mathf.Lerp(
                    0.25f,
                    1f,
                    progress);
            downBonusImage.color = color;

            yield return null;
        }

        color.a = 1f;
        downBonusImage.color = color;
    }

    private void CheckForThirdAndLong(FootballGameSituation situation)
    {
        if (situation.down == 3 && situation.yardsToGo >= 7)
        {
            StartCoroutine(ShowThirdAndLongBonusRoutine());
        }
    }

    private IEnumerator ShowThirdAndLongBonusRoutine()
    {
        float elapsed = 0f;
        float duration = 0.25f;

        Color color = downBonusImage.color;
        color.a = 0.25f;
        downBonusImage.color = color;
        downBonusImage.sprite = thirdAndLongSprite;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed / duration);

            color.a = Mathf.Lerp(
                    0.25f,
                    1f,
                    progress);
            downBonusImage.color = color;

            yield return null;
        }

        color.a = 1f;
        downBonusImage.color = color;
    }

    private void CheckForRedZone(FootballGameSituation situation)
    {
        if (situation.yardsFromOwnGoal >= 80)
        {
            StartCoroutine(ShowRedZoneRoutine());
        }
    }

    private IEnumerator ShowRedZoneRoutine()
    {
        float elapsed = 0f;
        float duration = 0.25f;

        Color color = yardLineText.color;
        color = Color.white;
        yardLineText.color = color;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed / duration);

            color = Color.Lerp(
                    Color.white,
                    Color.red,
                    progress);

            yardLineText.color = color;

            yield return null;
        }

        yardLineText.color = Color.red;
    }

    private void ClearBonuses()
    {
        Color color = rivalryBonusImage.color;
        color.a = 0f;
        rivalryBonusImage.color = color;

        comebackBonusImage.sprite = clutchTimeOffSprite;
        clutchTimeBonusImage.sprite = clutchTimeOffSprite;

        color = downBonusImage.color;
        color.a = 0f;
        downBonusImage.color = color;

        yardLineText.color = Color.white;
    }
}