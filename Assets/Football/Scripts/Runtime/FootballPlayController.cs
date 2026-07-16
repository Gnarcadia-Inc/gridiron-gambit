using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RuntimePlayerBinding
{
    public OffensiveRole role;
    public FootballRouteRunner runner;
}

public class FootballPlayController : MonoBehaviour
{
    [SerializeField]
    private Transform playOrigin;

    [SerializeField]
    private List<RuntimePlayerBinding> players = new();

    private readonly Dictionary<
        OffensiveRole,
        FootballRouteRunner> playerLookup = new();

    private void Awake()
    {
        RebuildPlayerLookup();
    }

    private void RebuildPlayerLookup()
    {
        playerLookup.Clear();

        foreach (RuntimePlayerBinding binding in players)
        {
            if (binding == null ||
                binding.runner == null)
            {
                continue;
            }

            playerLookup[binding.role] =
                binding.runner;
        }
    }

    public void ExecutePlay(FootballPlay play)
    {
        if (play == null)
        {
            Debug.LogWarning(
                "Cannot execute a null football play.");

            return;
        }

        foreach (RouteAssignment assignment
                 in play.assignments)
        {
            if (!playerLookup.TryGetValue(
                    assignment.role,
                    out FootballRouteRunner runner))
            {
                Debug.LogWarning(
                    $"No runtime player is bound to " +
                    $"{assignment.role}.");

                continue;
            }

            runner.RunRoute(
                assignment.route,
                playOrigin,
                assignment.startingOffsetYards,
                assignment.releaseDelay);
        }
    }

    public void StopPlay()
    {
        foreach (FootballRouteRunner runner
                 in playerLookup.Values)
        {
            runner.StopRoute();
        }
    }
}