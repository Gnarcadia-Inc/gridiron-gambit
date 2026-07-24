using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

        gameObject.SetActive(false);
    }

    private IEnumerator RevealRoutine(
        FootballGameSituation situation,
        FootballTeamDefinition[] teamBank,
        Action onFinished)
    {
        /*
         * Left-to-right stop order.
         *
         * Opponent
         * Score
         * Quarter
         * Clock
         * Down
         * Yards
         * Yard line
         */
        const int fieldCount = 7;

        float intervalBetweenStops =
            totalRevealSeconds /
            fieldCount;

        float elapsed = 0f;
        float nextRollUpdate = 0f;
        int stoppedFields = 0;

        while (elapsed < totalRevealSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            nextRollUpdate -= Time.unscaledDeltaTime;

            if (nextRollUpdate <= 0f)
            {
                nextRollUpdate =
                    rollingUpdateInterval;

                UpdateRollingFields(
                    stoppedFields,
                    situation,
                    teamBank);
            }

            int shouldBeStopped =
                Mathf.Clamp(
                    Mathf.FloorToInt(
                        elapsed /
                        intervalBetweenStops),
                    0,
                    fieldCount);

            while (stoppedFields <
                   shouldBeStopped)
            {
                SetFinalField(
                    stoppedFields,
                    situation);

                stoppedFields++;
            }

            yield return null;
        }

        while (stoppedFields < fieldCount)
        {
            SetFinalField(
                stoppedFields,
                situation);

            stoppedFields++;
        }

        revealCoroutine = null;
        onFinished?.Invoke();
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
                break;

            case 1:
                playerScoreText.text = $"{situation.playerScore}";
                oppScoreText.text = $"{situation.opponentScore}";
                break;

            case 2:
                quarterText.text =
                    situation.QuarterText;
                break;

            case 3:
                clockText.text =
                    situation.ClockText;
                break;

            case 4:
                downText.text =
                    situation.DownText;
                break;

            case 5:
                yardsToGoText.text =
                    situation.yardsToGo.ToString();
                break;

            case 6:
                yardLineText.text =
                    situation.YardLineText;
                break;
        }
    }

    private static FootballTeamDefinition GetRandomTeam(
        FootballTeamDefinition playerTeam,
        FootballTeamDefinition[] teamBank)
    {
        if (teamBank == null ||
            teamBank.Length == 0)
        {
            return teamBank[0];
        }

        for (int attempt = 0;
             attempt < 10;
             attempt++)
        {
            return teamBank[
                    UnityEngine.Random.Range(
                        0,
                        teamBank.Length)];
        }

        return teamBank[0];
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
}