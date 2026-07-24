using System.Collections;
using UnityEngine;

public class FootballFrontDefenderController :
    MonoBehaviour
{
    [SerializeField]
    private CharacterController
        characterController;

    [SerializeField]
    private FootballPlayerAnimator
        playerAnimator;

    [SerializeField]
    private float movementSpeed =
        5f;

    [SerializeField]
    private float contactDistance =
        0.6f;

    private FootballPlaySequenceController
        sequenceController;

    private FootballOffensivePlayerController assignedBlocker;

    public FootballOffensivePlayerController AssignedBlocker => assignedBlocker;

    public bool HasBlockerAssignment =>
        assignedBlocker != null;

    private FootballRouteRunner
        quarterbackRunner;

    private Coroutine movementCoroutine;

    private bool isDesignatedSacker;
    private bool isBlitzing;

    public bool IsRushing
    {
        get
        {
            return (isBlitzing || Role == DefensiveFrontRole.DefensiveLineman) ? true : false; 
        }
    }

    public DefensiveFrontRole Role
    {
        get;
        private set;
    }

    public bool IsBlitzing => isBlitzing;

    public bool IsDesignatedSacker => isDesignatedSacker;

    private FootballOffensivePlayerController bulldozeTarget;

    private bool bulldozeTriggered;

    public FootballPlayerAnimator PlayerAnimator =>
        playerAnimator;

    private void Awake()
    {
        if (characterController == null)
        {
            characterController =
                GetComponent<CharacterController>();
        }

        if (playerAnimator == null)
        {
            playerAnimator =
                GetComponentInChildren<
                    FootballPlayerAnimator>();
        }
    }

    public void Initialize(
        FootballPlaySequenceController controller,
        DefensiveFrontRole role,
        FootballRouteRunner quarterback,
        bool blitzing,
        bool designatedSacker)
    {
        sequenceController = controller;
        Role = role;
        quarterbackRunner = quarterback;
        isBlitzing = blitzing;
        isDesignatedSacker =
            designatedSacker;
    }

    public void AssignBlocker(FootballOffensivePlayerController blocker)
    {
        assignedBlocker = blocker;

        if (blocker != null)
        {
            blocker.AssignDefender(this);
        }
    }

    public void ReceiveBlock(FootballOffensivePlayerController blocker)
    {
        if (blocker != assignedBlocker)
        {
            return;
        }

        StopRush();

        playerAnimator?.TriggerBeingBlocked();
    }

    public void BeginRush()
    {
        if (movementCoroutine != null)
        {
            StopCoroutine(
                movementCoroutine);
        }

        movementCoroutine =
            StartCoroutine(
                RushRoutine());
    }

    public void StopRush()
    {
        if (movementCoroutine != null)
        {
            StopCoroutine(
                movementCoroutine);

            movementCoroutine = null;
        }
    }

    private IEnumerator RushRoutine()
    {
        if (Role == DefensiveFrontRole.Linebacker && !isBlitzing)
        {
            yield return DropIntoCoverage();
            yield break;
        }

        if (isDesignatedSacker)
        {
            yield return RushQuarterback();
        }
        else
        {
            yield return EngageBlocker();
        }

        movementCoroutine = null;
    }

    private IEnumerator RushQuarterback()
    {
        while (quarterbackRunner != null)
        {
            if (!bulldozeTriggered &&
                bulldozeTarget != null)
            {
                float blockerDistance =
                    HorizontalDistance(
                        transform.position,
                        bulldozeTarget
                            .transform.position);

                if (blockerDistance <=
                    contactDistance)
                {
                    bulldozeTriggered = true;

                    bulldozeTarget.GetBulldozed();
                }
            }

            Vector3 quarterbackPosition =
                quarterbackRunner
                    .transform.position;

            MoveToward(
                quarterbackPosition);

            if (HorizontalDistance(
                    transform.position,
                    quarterbackPosition) <=
                contactDistance)
            {
                playerAnimator?.TriggerSack();

                sequenceController?
                    .RegisterSack(this);

                yield break;
            }

            yield return null;
        }
    }

    private IEnumerator EngageBlocker()
    {
        if (assignedBlocker == null)
        {
            yield break;
        }

        while (assignedBlocker != null)
        {
            Vector3 blockerPosition =
                assignedBlocker.transform.position;

            MoveToward(blockerPosition);

            if (HorizontalDistance(
                    transform.position,
                    blockerPosition) <=
                contactDistance)
            {
                assignedBlocker
                    .EngageAssignedDefender();

                yield break;
            }

            yield return null;
        }
    }

    private IEnumerator DropIntoCoverage()
    {
        Vector3 dropDirection =
            -transform.forward;

        float dropDistance =
            FootballUnits.YardsToUnits(5f);

        Vector3 target =
            transform.position +
            dropDirection *
            dropDistance;

        while (HorizontalDistance(
                   transform.position,
                   target) >
               0.1f)
        {
            MoveToward(target);
            yield return null;
        }
    }

    private void MoveToward(
        Vector3 target)
    {
        Vector3 direction =
            target - transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude <
            0.0001f)
        {
            return;
        }

        direction.Normalize();

        characterController.Move(
            direction *
            movementSpeed *
            Time.deltaTime);

        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(
                    direction,
                    Vector3.up),
                12f *
                Time.deltaTime);
    }

    private static float HorizontalDistance(
        Vector3 first,
        Vector3 second)
    {
        first.y = 0f;
        second.y = 0f;

        return Vector3.Distance(
            first,
            second);
    }

    public void AssignBulldozeTarget(FootballOffensivePlayerController target)
    {
        bulldozeTarget = target;
    }
}