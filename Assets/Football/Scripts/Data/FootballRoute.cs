using System;
using System.Collections.Generic;
using UnityEngine;

public enum RouteDirection
{
    // Straight directions
    Forward,
    Backward,
    Left,
    Right,

    // Normal 45-degree diagonal directions
    ForwardLeft,
    ForwardRight,
    BackwardLeft,
    BackwardRight,

    // Shallow 30-degree slants.
    // These are measured from the horizontal
    // left/right axis.
    SlantForwardLeft,
    SlantForwardRight,
    SlantBackwardLeft,
    SlantBackwardRight
}

public enum RouteEndBehavior
{
    None,
    WaitForPass,
    Block
}

[Serializable]
public class RouteStep
{
    public RouteDirection direction = RouteDirection.Forward;

    [Min(0.01f)]
    public float distanceYards = 5f;

    [Min(0.1f)]
    public float speedYardsPerSecond = 6f;

    [Min(0f)]
    public float delayBeforeStep;

    public bool stopAtEnd;
}

[CreateAssetMenu(fileName = "New Route", menuName = "Football/Route")]
public class FootballRoute : ScriptableObject
{
    public string routeName = "New Route";

    [TextArea]
    public string description;

    public List<RouteStep> steps = new();

    public RouteEndBehavior endBehavior = RouteEndBehavior.WaitForPass;
}