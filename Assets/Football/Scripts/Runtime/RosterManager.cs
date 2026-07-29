using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public enum RosterPosition
{
    QB,
    WRX,
    WRY,
    WRB,
    TE,
    RB,
    DB,
    LB,
    DL
}

public class RosterManager : MonoBehaviour
{
    public string GetPlayer(FootballTeamDefinition team, RosterPosition position)
    {
        List<RosterSpot> players = team.roster
            .Where(player => player.playerPosition == position)
            .ToList();

        int rand = Random.Range(0, players.Count);
        return players[rand].playerName;
    }

    public RosterPosition ConvertOffensiveRoleToRosterPosition(OffensiveRole role)
    {
        RosterPosition rosterPosition = RosterPosition.QB;
        switch (role)
        {
            case OffensiveRole.Quarterback:
                rosterPosition = RosterPosition.QB;
                break;
            case OffensiveRole.RunningBack:
                rosterPosition = RosterPosition.RB;
                break;
            case OffensiveRole.SlotReceiver:
                rosterPosition = RosterPosition.WRY;
                break;
            case OffensiveRole.WideReceiverLeft:
                rosterPosition = RosterPosition.WRX;
                break;
            case OffensiveRole.WideReceiverRight:
                rosterPosition = RosterPosition.WRB;
                break;
            case OffensiveRole.TightEnd:
                rosterPosition = RosterPosition.TE;
                break;
        }

        return rosterPosition;
    }
}
