using UnityEngine;

public class FootballOffensivePlayerController : MonoBehaviour
{
    [SerializeField]
    private FootballRouteRunner routeRunner;

    [SerializeField]
    private FootballPlayerAnimator playerAnimator;

    private FootballFrontDefenderController
        assignedDefender;

    private bool hasEngagedBlock;

    public OffensiveRole Role
    {
        get;
        private set;
    }

    public FootballFrontDefenderController
        AssignedDefender =>
            assignedDefender;

    public bool HasBlockingAssignment =>
        assignedDefender != null;

    public bool HasEngagedBlock =>
        hasEngagedBlock;

    public FootballRouteRunner RouteRunner =>
        routeRunner;

    public FootballPlayerAnimator PlayerAnimator =>
        playerAnimator;

    public RouteEndBehavior EndBehavior
    {
        get;
        private set;
    }

    public bool IsOffensiveLineman =>
        OffensiveRoleUtility
            .IsOffensiveLineman(Role);

    private void Awake()
    {
        if (routeRunner == null)
        {
            routeRunner =
                GetComponent<FootballRouteRunner>();
        }

        if (playerAnimator == null)
        {
            playerAnimator =
                GetComponentInChildren<
                    FootballPlayerAnimator>();
        }

        if (routeRunner != null)
        {
            routeRunner.RouteCompleted +=
                HandleRouteCompleted;
        }
    }

    private void OnDestroy()
    {
        if (routeRunner != null)
        {
            routeRunner.RouteCompleted -=
                HandleRouteCompleted;
        }
    }

    public void Initialize(
        OffensiveRole role,
        RouteEndBehavior endBehavior)
    {
        Role = role;
        EndBehavior = endBehavior;
    }

    public void EnterBlockState()
    {
        routeRunner?.StopMovement();
        playerAnimator?.TriggerBlock();
    }

    public void ReceiveBlockContact()
    {
        routeRunner?.StopMovement();
        playerAnimator?.TriggerBlock();
    }

    private void HandleRouteCompleted()
    {
        switch (EndBehavior)
        {
            case RouteEndBehavior.WaitForPass:
                playerAnimator?
                    .TriggerWaitForPass();
                break;

            case RouteEndBehavior.Block:
                EnterBlockState();
                break;
        }
    }

    public void AssignDefender(FootballFrontDefenderController defender)
    {
        assignedDefender = defender;
        hasEngagedBlock = false;
    }

    public void ClearDefenderAssignment()
    {
        assignedDefender = null;
        hasEngagedBlock = false;
    }

    public void EngageAssignedDefender()
    {
        if (assignedDefender == null ||
            hasEngagedBlock)
        {
            return;
        }

        hasEngagedBlock = true;

        routeRunner?.StopMovement();
        playerAnimator?.TriggerBlock();

        assignedDefender.ReceiveBlock(this);
    }

    public void GetBulldozed()
    {
        hasEngagedBlock = true;

        routeRunner?.StopMovement();
        playerAnimator?.TriggerBulldozed();
    }
}