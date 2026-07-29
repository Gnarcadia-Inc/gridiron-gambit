using System;

[Serializable]
public class FootballGameSituation
{
    public FootballTeamDefinition playerTeam;
    public FootballTeamDefinition opponentTeam;

    public int playerScore;
    public int opponentScore;

    public int quarter;

    public int secondsRemaining;

    public int down;
    public int yardsToGo;

    public int yardsFromOwnGoal;

    public RivalryType rivalry;

    public int ScoreDifferential =>
        playerScore - opponentScore;

    public string QuarterText =>
        quarter switch
        {
            1 => "1st",
            2 => "2nd",
            3 => "3rd",
            4 => "4th",
            _ => $"{quarter}th"
        };

    public string DownText =>
        down switch
        {
            1 => "1st",
            2 => "2nd",
            3 => "3rd",
            4 => "4th",
            _ => $"{down}th"
        };

    public string ClockText
    {
        get
        {
            int minutes = secondsRemaining / 60;
            int seconds = secondsRemaining % 60;

            return $"{minutes}:{seconds:00}";
        }
    }

    public string ScoreText =>
        $"{playerScore}-{opponentScore}";

    public string DownAndDistanceText =>
        $"{DownText} & {yardsToGo}";

    public string YardLineText
    {
        get
        {
            if (yardsFromOwnGoal == 50)
            {
                return "50 Yard Line";
            }

            if (yardsFromOwnGoal < 50)
            {
                return
                    $"{playerTeam.abbreviation} " +
                    $"{yardsFromOwnGoal}";
            }

            int opponentYardLine =
                100 - yardsFromOwnGoal;

            return
                $"{opponentTeam.abbreviation} " +
                $"{opponentYardLine}";
        }
    }
}