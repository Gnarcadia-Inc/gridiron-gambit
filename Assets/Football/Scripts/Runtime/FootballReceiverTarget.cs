using UnityEngine;
using UnityEngine.Events;

public class FootballReceiverTarget : MonoBehaviour
{
    [SerializeField]
    private FootballRouteRunner routeRunner;

    [Tooltip(
        "Where the ball aims, usually the chest or hands.")]
    [SerializeField]
    private Transform catchPoint;

    [SerializeField]
    private string displayName = "Receiver";

    public UnityEvent onCatch;

    public FootballRouteRunner RouteRunner =>
        routeRunner;

    public Transform CatchPoint =>
        catchPoint != null
            ? catchPoint
            : transform;

    public string DisplayName =>
        string.IsNullOrWhiteSpace(displayName)
            ? gameObject.name
            : displayName;

    [SerializeField]
    private OffensiveRole role;

    public OffensiveRole Role => role;

    [SerializeField]
    private FootballPlayerAnimator playerAnimator;

    public FootballPlayerAnimator PlayerAnimator =>
    playerAnimator;

    private void Reset()
    {
        routeRunner =
            GetComponent<FootballRouteRunner>();

        playerAnimator =
            GetComponent<FootballPlayerAnimator>();

        if (playerAnimator == null)
        {
            playerAnimator =
                GetComponentInChildren<FootballPlayerAnimator>();
        }
    }

    private void Awake()
    {
        if (playerAnimator == null)
        {
            playerAnimator =
                GetComponent<FootballPlayerAnimator>();
        }

        if (playerAnimator == null)
        {
            playerAnimator =
                GetComponentInChildren<FootballPlayerAnimator>();
        }
    }

    public Vector3 PredictCatchPosition(
        float flightTime)
    {
        if (routeRunner == null)
        {
            return CatchPoint.position;
        }

        Vector3 predictedRoot =
            routeRunner.PredictPosition(flightTime);

        Vector3 catchOffset =
            CatchPoint.position -
            transform.position;

        return predictedRoot + catchOffset;
    }

    public void ReceiveBall(
        FootballBall ball,
        Transform playOrigin,
        float runAfterCatchSpeed)
    {
        if (ball == null)
        {
            return;
        }

        ball.AttachToReceiver(CatchPoint);

        routeRunner.StopMovement();

        routeRunner.StartRunningForward(
            playOrigin,
            runAfterCatchSpeed);

        onCatch?.Invoke();
    }

    public void InitializeRole(OffensiveRole offensiveRole)
    {
        role = offensiveRole;
    }
}