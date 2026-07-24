using System;
using System.Collections.Generic;
using UnityEngine;

public enum OffensiveRole
{
    Quarterback,

    RunningBack,
    TightEnd,
    WideReceiverLeft,
    SlotReceiver,
    WideReceiverRight,

    LeftTackle,
    LeftGuard,
    Center,
    RightGuard,
    RightTackle
}

public enum FootballPlayType
{
    Pass,
    Run,
    Option
}

[Serializable]
public class RouteAssignment
{
    public OffensiveRole role;

    public FootballRoute route;

    [Tooltip(
        "Starting location relative to the quarterback/play origin. " +
        "X is left/right and Y is forward/backward.")]
    public Vector2 startingOffsetYards;

    [Min(0f)]
    public float releaseDelay;
}

[Serializable]
public class OffensiveLinePlayEntry
{
    public OffensiveRole role;

    public Vector2 startingOffsetYards;

    public FootballRoute route;

    public RouteEndBehavior endBehavior =
        RouteEndBehavior.Block;
}

[CreateAssetMenu(fileName = "New Play", menuName = "Football/Play")]
public class FootballPlay : ScriptableObject
{
    public string playName = "New Play";

    [TextArea]
    public string description;

    public List<RouteAssignment> assignments = new();

    public FootballPlayType playType = FootballPlayType.Pass;

    [Min(0f)]
    public float choiceDelaySeconds = 2f;

    [Header("Run Play")]

    public OffensiveRole runBallCarrierRole = OffensiveRole.RunningBack;

    [Min(0f)]
    public float handoffDelaySeconds = 0.6f;

    [Min(0.1f)]
    public float handoffDistanceYards = 1.5f;

    [Min(0f)]
    public float maximumHandoffWaitSeconds = 1f;

    public List<OffensiveLinePlayEntry> offensiveLine = new();

    public Vector2 quarterbackStartingOffsetYards = new Vector2(0f, -2f);


    private void OnValidate()
    {
        ValidateOffensiveLine();
    }

    private void ValidateOffensiveLine()
    {
        if (offensiveLine == null)
        {
            return;
        }

        if (offensiveLine.Count != 5)
        {
            Debug.LogWarning(
                $"Play '{name}' should contain exactly " +
                $"5 offensive linemen.",
                this);
        }

        var usedRoles = new HashSet<OffensiveRole>();

        foreach (OffensiveLinePlayEntry entry
                 in offensiveLine)
        {
            if (entry == null)
            {
                continue;
            }

            if (!OffensiveRoleUtility.IsOffensiveLineman(entry.role))
            {
                Debug.LogWarning(
                    $"{entry.role} is not an " +
                    $"offensive-line role.",
                    this);
            }

            if (!usedRoles.Add(entry.role))
            {
                Debug.LogWarning(
                    $"Play '{name}' contains duplicate " +
                    $"offensive-line role {entry.role}.",
                    this);
            }
        }
    }

    public bool HasValidOffensivePersonnel()
    {
        int skillPlayerCount = 0;

        foreach (RouteAssignment assignment
                 in assignments)
        {
            if (assignment == null)
            {
                continue;
            }

            if (OffensiveRoleUtility.IsSkillPlayer(
                    assignment.role))
            {
                skillPlayerCount++;
            }
        }

        int offensiveLineCount =
            offensiveLine != null
                ? offensiveLine.Count
                : 0;

        return skillPlayerCount == 5 &&
               offensiveLineCount == 5;
    }
}