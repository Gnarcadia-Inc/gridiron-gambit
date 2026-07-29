using System.Collections.Generic;
using UnityEngine;

public static class FootballSituationGenerator
{
    private static readonly int[] CommonScoringValues =
    {
        2, 3, 6, 7, 8
    };

    public static FootballGameSituation Generate(
        FootballTeamDefinition playerTeam,
        IReadOnlyList<FootballTeamDefinition> allTeams)
    {
        FootballTeamDefinition opponent =
            ChooseOpponent(
                playerTeam,
                allTeams);

        RivalryType rivalry = CheckForRivalry(playerTeam, opponent);

        int quarter =
            Random.Range(1, 5);

        int secondsRemaining =
            Random.Range(30, 901);

        float elapsedMinutes =
            (quarter - 1) * 15f +
            (900f - secondsRemaining) / 60f;

        int playerScore =
            GeneratePlausibleScore(
                elapsedMinutes);

        int opponentScore =
            GeneratePlausibleScore(
                elapsedMinutes);

        /*
         * Avoid an excessive share of exact ties.
         */
        if (playerScore == opponentScore &&
            Random.value < 0.65f)
        {
            if (Random.value < 0.5f)
            {
                playerScore +=
                    Random.value < 0.75f ? 3 : 7;
            }
            else
            {
                opponentScore +=
                    Random.value < 0.75f ? 3 : 7;
            }
        }

        int down =
            Random.Range(1, 5);

        int yardsToGo =
            GenerateYardsToGo(down);

        int fieldPosition =
            GenerateFieldPosition();

        /*
         * Keep goal-to-go distances valid.
         */
        int yardsToOpponentGoal =
            100 - fieldPosition;

        yardsToGo =
            Mathf.Clamp(
                yardsToGo,
                1,
                Mathf.Max(1, yardsToOpponentGoal));

        return new FootballGameSituation
        {
            playerTeam = playerTeam,
            opponentTeam = opponent,
            playerScore = playerScore,
            opponentScore = opponentScore,
            quarter = quarter,
            secondsRemaining = secondsRemaining,
            down = down,
            yardsToGo = yardsToGo,
            yardsFromOwnGoal = fieldPosition,
            rivalry = rivalry
        };
    }

    private static FootballTeamDefinition ChooseOpponent(
        FootballTeamDefinition playerTeam,
        IReadOnlyList<FootballTeamDefinition> teams)
    {
        var candidates =
            new List<FootballTeamDefinition>();

        foreach (FootballTeamDefinition team in teams)
        {
            if (team != null &&
                team != playerTeam)
            {
                candidates.Add(team);
            }
        }

        if (candidates.Count == 0)
        {
            return playerTeam;
        }

        return candidates[
            Random.Range(0, candidates.Count)];
    }

    private static RivalryType CheckForRivalry(FootballTeamDefinition pTeam, FootballTeamDefinition oTeam)
    {
        RivalryType rivalry = RivalryType.None;

        if (pTeam.abbreviation == "RHN" && oTeam.abbreviation == "JUG" ||
            pTeam.abbreviation == "BST" && oTeam.abbreviation == "PGN" ||
            pTeam.abbreviation == "MMB" && oTeam.abbreviation == "PLT" ||
            pTeam.abbreviation == "JUG" && oTeam.abbreviation == "RHN" ||
            pTeam.abbreviation == "PGN" && oTeam.abbreviation == "BST" ||
            pTeam.abbreviation == "PLT" && oTeam.abbreviation == "MMB"
            )
        {
            //CHANGE TO DETERMINISTIC
            int rand = UnityEngine.Random.Range(0, 7);
            if (rand == 6)
            {
                rivalry = RivalryType.Playoffs;
            }
            else
            {
                rivalry = RivalryType.DivisionRivalry;
            }
            
        }
        else
        {
            //CHANGE TO DETERMINISTIC
            int rand = UnityEngine.Random.Range(0, 14);
            if (rand < 2)
            {
                rivalry = RivalryType.StadiumSeries;
            }
            else if (rand < 4)
            {
                rivalry = RivalryType.Playoffs;
            }
            else if (rand < 5)
            {
                rivalry = RivalryType.SuperBowl;
            }
        }

        return rivalry;
    }

    private static int GeneratePlausibleScore(
        float elapsedMinutes)
    {
        /*
         * Roughly one possible scoring opportunity
         * every 5–8 elapsed minutes.
         */
        float expectedScoringEvents =
            elapsedMinutes / 6.5f;

        int maximumEvents =
            Mathf.Clamp(
                Mathf.CeilToInt(
                    expectedScoringEvents + 2f),
                0,
                10);

        int eventCount = 0;

        for (int i = 0; i < maximumEvents; i++)
        {
            float eventChance =
                Mathf.Clamp01(
                    expectedScoringEvents -
                    i * 0.72f);

            if (Random.value < eventChance)
            {
                eventCount++;
            }
        }

        int score = 0;

        for (int i = 0; i < eventCount; i++)
        {
            float roll = Random.value;

            if (roll < 0.48f)
            {
                score += 7;
            }
            else if (roll < 0.82f)
            {
                score += 3;
            }
            else if (roll < 0.89f)
            {
                score += 6;
            }
            else if (roll < 0.95f)
            {
                score += 8;
            }
            else
            {
                score += 2;
            }
        }

        /*
         * Keep prototype situations believable.
         */
        int timeBasedMaximum =
            Mathf.RoundToInt(
                Mathf.Lerp(
                    7f,
                    49f,
                    elapsedMinutes / 60f));

        return Mathf.Clamp(
            score,
            0,
            timeBasedMaximum);
    }

    private static int GenerateYardsToGo(
        int down)
    {
        float roll = Random.value;

        if (down == 1 &&
            roll < 0.55f)
        {
            return 10;
        }

        if (roll < 0.20f)
        {
            return Random.Range(1, 4);
        }

        if (roll < 0.82f)
        {
            return Random.Range(4, 11);
        }

        return Random.Range(11, 21);
    }

    private static int GenerateFieldPosition()
    {
        float roll = Random.value;

        if (roll < 0.18f)
        {
            return Random.Range(5, 21);
        }

        if (roll < 0.66f)
        {
            return Random.Range(21, 50);
        }

        if (roll < 0.90f)
        {
            return Random.Range(50, 81);
        }

        return Random.Range(81, 96);
    }

    public static int RandomPossibleScore(
        int quarter)
    {
        float approximateElapsed =
            Mathf.Clamp(
                quarter * 12f,
                1f,
                60f);

        return GeneratePlausibleScore(
            approximateElapsed);
    }

    public static int RandomScoringValue()
    {
        return CommonScoringValues[
            Random.Range(
                0,
                CommonScoringValues.Length)];
    }
}