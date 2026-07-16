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
    WideReceiverRight
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

[CreateAssetMenu(
    fileName = "New Play",
    menuName = "Football/Play")]
public class FootballPlay : ScriptableObject
{
    public string playName = "New Play";

    [TextArea]
    public string description;

    public List<RouteAssignment> assignments = new();
}