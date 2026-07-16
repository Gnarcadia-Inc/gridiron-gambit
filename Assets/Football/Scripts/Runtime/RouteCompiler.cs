using System.Collections.Generic;
using UnityEngine;

public struct RouteNode
{
    public Vector3 localPosition;
    public float speed;
    public float delay;
    public bool stopAtNode;

    public RouteNode(
        Vector3 localPosition,
        float speed,
        float delay,
        bool stopAtNode)
    {
        this.localPosition = localPosition;
        this.speed = speed;
        this.delay = delay;
        this.stopAtNode = stopAtNode;
    }
}

public static class RouteCompiler
{
    public static List<RouteNode> Compile(FootballRoute route)
    {
        var nodes = new List<RouteNode>();

        Vector3 currentPosition = Vector3.zero;

        nodes.Add(
            new RouteNode(
                currentPosition,
                0f,
                0f,
                false));

        if (route == null)
        {
            return nodes;
        }

        foreach (RouteStep step in route.steps)
        {
            Vector3 direction = GetDirection(step.direction);

            currentPosition +=
                direction *
                FootballUnits.YardsToUnits(step.distanceYards);

            nodes.Add(
                new RouteNode(
                    currentPosition,
                    FootballUnits.YardsToUnits(
                        step.speedYardsPerSecond),
                    step.delayBeforeStep,
                    step.stopAtEnd));
        }

        return nodes;
    }

    public static Vector3 GetDirection(
    RouteDirection direction)
    {
        /*
         * A 30-degree slant measured from the
         * horizontal axis has:
         *
         * Horizontal amount:
         * cos(30 degrees) = approximately 0.866
         *
         * Forward/backward amount:
         * sin(30 degrees) = 0.5
         */
        const float slantHorizontal = 0.8660254f;
        const float slantVertical = 0.5f;

        return direction switch
        {
            RouteDirection.Forward =>
                Vector3.forward,

            RouteDirection.Backward =>
                Vector3.back,

            RouteDirection.Left =>
                Vector3.left,

            RouteDirection.Right =>
                Vector3.right,

            // Normal 45-degree diagonals

            RouteDirection.ForwardLeft =>
                new Vector3(
                    -1f,
                    0f,
                    1f).normalized,

            RouteDirection.ForwardRight =>
                new Vector3(
                    1f,
                    0f,
                    1f).normalized,

            RouteDirection.BackwardLeft =>
                new Vector3(
                    -1f,
                    0f,
                    -1f).normalized,

            RouteDirection.BackwardRight =>
                new Vector3(
                    1f,
                    0f,
                    -1f).normalized,

            // Shallow 30-degree slants

            RouteDirection.SlantForwardLeft =>
                new Vector3(
                    -slantHorizontal,
                    0f,
                    slantVertical),

            RouteDirection.SlantForwardRight =>
                new Vector3(
                    slantHorizontal,
                    0f,
                    slantVertical),

            RouteDirection.SlantBackwardLeft =>
                new Vector3(
                    -slantHorizontal,
                    0f,
                    -slantVertical),

            RouteDirection.SlantBackwardRight =>
                new Vector3(
                    slantHorizontal,
                    0f,
                    -slantVertical),

            _ => Vector3.zero
        };
    }
}