using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "Football Play Bank",
    menuName = "Football/Play Bank")]
public class FootballPlayBank : ScriptableObject
{
    public List<FootballPlayOption> plays = new();

    public List<FootballPlayOption> ChooseOptions(
        FootballGameSituation situation,
        int optionCount = 3)
    {
        var scoredOptions =
            new List<ScoredPlayOption>();

        foreach (FootballPlayOption option in plays)
        {
            if (option == null ||
                option.play == null)
            {
                continue;
            }

            float score =
                option.CalculateScore(situation);

            if (float.IsNegativeInfinity(score))
            {
                continue;
            }

            scoredOptions.Add(
                new ScoredPlayOption
                {
                    option = option,
                    score = score
                });
        }

        scoredOptions.Sort(
            (a, b) =>
                b.score.CompareTo(a.score));

        var result =
            new List<FootballPlayOption>();

        for (int i = 0;
             i < Mathf.Min(
                 optionCount,
                 scoredOptions.Count);
             i++)
        {
            result.Add(
                scoredOptions[i].option);
        }

        if (result.Count < optionCount)
        {
            foreach (FootballPlayOption option in plays)
            {
                if (option == null ||
                    option.play == null ||
                    result.Contains(option))
                {
                    continue;
                }

                result.Add(option);

                if (result.Count >= optionCount)
                {
                    break;
                }
            }
        }

        return result;
    }

    private class ScoredPlayOption
    {
        public FootballPlayOption option;
        public float score;
    }
}