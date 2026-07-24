using System;
using UnityEngine;

[Serializable]
public class FootballPlayOption
{
    [Header("Play")]

    public FootballPlay play;

    public Sprite buttonSprite;

    [Header("Down")]

    [Range(1, 4)]
    public int minimumDown = 1;

    [Range(1, 4)]
    public int maximumDown = 4;

    [Header("Distance")]

    [Min(1)]
    public int minimumYardsToGo = 1;

    [Min(1)]
    public int maximumYardsToGo = 99;

    [Header("Field Position")]

    [Range(1, 99)]
    public int minimumYardsFromOwnGoal = 1;

    [Range(1, 99)]
    public int maximumYardsFromOwnGoal = 99;

    [Header("Quarter")]

    [Range(1, 4)]
    public int minimumQuarter = 1;

    [Range(1, 4)]
    public int maximumQuarter = 4;

    [Header("Score Differential")]

    public int minimumScoreDifferential = -99;

    public int maximumScoreDifferential = 99;

    [Header("Clock")]

    [Range(0, 900)]
    public int minimumSecondsRemaining = 0;

    [Range(0, 900)]
    public int maximumSecondsRemaining = 900;

    [Header("Weight")]

    [Min(0.01f)]
    public float baseWeight = 1f;

    public bool IsValid(
        FootballGameSituation situation)
    {
        return
            situation.down >= minimumDown &&
            situation.down <= maximumDown &&

            situation.yardsToGo >= minimumYardsToGo &&
            situation.yardsToGo <= maximumYardsToGo &&

            situation.yardsFromOwnGoal >=
            minimumYardsFromOwnGoal &&

            situation.yardsFromOwnGoal <=
            maximumYardsFromOwnGoal &&

            situation.quarter >= minimumQuarter &&
            situation.quarter <= maximumQuarter &&

            situation.ScoreDifferential >=
            minimumScoreDifferential &&

            situation.ScoreDifferential <=
            maximumScoreDifferential &&

            situation.secondsRemaining >=
            minimumSecondsRemaining &&

            situation.secondsRemaining <=
            maximumSecondsRemaining;
    }

    public float CalculateScore(
        FootballGameSituation situation)
    {
        if (!IsValid(situation))
        {
            return float.NegativeInfinity;
        }

        float score = baseWeight;

        /*
         * Reward plays whose preferred ranges
         * are close to the exact situation.
         */
        float distanceCenter =
            (minimumYardsToGo +
             maximumYardsToGo) * 0.5f;

        float distanceDifference =
            Mathf.Abs(
                situation.yardsToGo -
                distanceCenter);

        score +=
            1f /
            (1f + distanceDifference);

        float fieldCenter =
            (minimumYardsFromOwnGoal +
             maximumYardsFromOwnGoal) * 0.5f;

        float fieldDifference =
            Mathf.Abs(
                situation.yardsFromOwnGoal -
                fieldCenter);

        score +=
            0.5f /
            (1f + fieldDifference * 0.1f);

        score += UnityEngine.Random.Range(0f, 0.3f);

        return score;
    }
}