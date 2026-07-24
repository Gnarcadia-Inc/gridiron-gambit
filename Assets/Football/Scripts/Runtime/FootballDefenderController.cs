using System.Collections;
using UnityEngine;

public enum DefensiveRole
{
    Coverage,
    Rushing,
    None
}

public enum DefensiveFrontRole
{
    DefensiveLineman,
    Linebacker
}

public enum DefensiveFrontType
{
    ThreeLinemenThreeLinebackers,
    FourLinemenTwoLinebackers
}

[RequireComponent(typeof(CharacterController))]
public class FootballDefenderController : MonoBehaviour
{
    [Header("Components")]

    [SerializeField]
    private CharacterController characterController;

    [Header("Coverage")]

    [SerializeField]
    private float defaultSpeedYardsPerSecond = 6f;

    [SerializeField]
    private float rotationSpeed = 720f;

    [SerializeField]
    private float coverageCushionYards = 0.75f;

    [SerializeField]
    private float stoppingDistance = 0.1f;

    private FootballReceiverTarget coverageTarget;
    private FootballPlaySequenceController sequenceController;
    private Transform playOrigin;

    private Coroutine coverageCoroutine;

    private float currentSpeedUnitsPerSecond;
    private bool hasBall;
    private bool isActiveDefender;

    public FootballReceiverTarget CoverageTarget =>
        coverageTarget;

    public bool HasBall =>
        hasBall;

    public bool IsActiveDefender =>
        isActiveDefender;

    [Header("Defender Reaction")]
    [SerializeField]
    [Range(0f, 1f)]
    private float predictionAbility = 0.5f;

    private float maximumPredictionSeconds = 0.75f;

    [SerializeField]
    [Min(1f)]
    private float closingSpeedMultiplier = 1.2f;

    [SerializeField]
    [Min(0.1f)]
    private float fullClosingSpeedGapYards = 5f;

    [SerializeField]
    private FootballPlayerAnimator playerAnimator;

    public FootballPlayerAnimator PlayerAnimator => playerAnimator;

    private void Reset()
    {
        characterController =
            GetComponent<CharacterController>();
    }

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
                GetComponent<FootballPlayerAnimator>();
        }

        if (playerAnimator == null)
        {
            playerAnimator =
                GetComponentInChildren<FootballPlayerAnimator>();
        }

        isActiveDefender = false;
    }

    public void InitializeDefender(
        FootballReceiverTarget assignedReceiver,
        DefensivePosition defensivePosition,
        Transform fieldOrigin,
        FootballPlaySequenceController controller,
        float speedYardsPerSecond)
    {
        StopCoverage();

        coverageTarget = assignedReceiver;
        sequenceController = controller;
        playOrigin = fieldOrigin;

        hasBall = false;
        isActiveDefender = true;

        float resolvedSpeed =
            speedYardsPerSecond > 0f
                ? speedYardsPerSecond
                : defaultSpeedYardsPerSecond;

        currentSpeedUnitsPerSecond =
            FootballUnits.YardsToUnits(
                resolvedSpeed);

        gameObject.name =
            $"{defensivePosition} covering " +
            $"{assignedReceiver.DisplayName}";
    }

    public void SetStartingPosition(
        Vector3 worldPosition,
        Quaternion worldRotation)
    {
        bool wasEnabled =
            characterController.enabled;

        characterController.enabled = false;

        transform.position = worldPosition;
        transform.rotation = worldRotation;

        characterController.enabled = wasEnabled;
    }

    public void BeginCoverage()
    {
        if (!isActiveDefender ||
            coverageTarget == null)
        {
            return;
        }

        StopCoverage();

        coverageCoroutine =
            StartCoroutine(
                CoverageRoutine());
    }

    private float CalculateClosingSpeed(
    Vector3 targetPosition)
    {
        float gapUnits =
            GetHorizontalDistance(
                transform.position,
                targetPosition);

        float gapYards =
            gapUnits /
            Mathf.Max(
                FootballUnits.UnityUnitsPerYard,
                0.0001f);

        float closingAmount =
            Mathf.InverseLerp(
                coverageCushionYards,
                fullClosingSpeedGapYards,
                gapYards);

        float speedMultiplier =
            Mathf.Lerp(
                1f,
                closingSpeedMultiplier,
                closingAmount);

        return currentSpeedUnitsPerSecond *
               speedMultiplier;
    }

    private static float GetHorizontalDistance(
    Vector3 first,
    Vector3 second)
    {
        first.y = 0f;
        second.y = 0f;

        return Vector3.Distance(
            first,
            second);
    }

    public void StopCoverage()
    {
        if (coverageCoroutine != null)
        {
            StopCoroutine(coverageCoroutine);
            coverageCoroutine = null;
        }
    }

    public void ReceiveInterception(
        FootballBall ball)
    {
        if (ball == null ||
            hasBall)
        {
            return;
        }

        hasBall = true;

        StopCoverage();

        ball.AttachToReceiver(transform);

        sequenceController.RegisterInterception(
            this,
            ball);
    }

    private IEnumerator CoverageRoutine()
    {
        while (coverageTarget != null)
        {
            Vector3 receiverCurrentPosition =
                coverageTarget.transform.position;

            Vector3 receiverPredictedPosition =
                GetPredictedReceiverPosition();

            Vector3 coverageReadPosition =
                Vector3.Lerp(
                    receiverCurrentPosition,
                    receiverPredictedPosition,
                    predictionAbility);

            Vector3 cushionDirection =
                playOrigin != null
                    ? playOrigin.forward
                    : Vector3.forward;

            cushionDirection.y = 0f;
            cushionDirection.Normalize();

            Vector3 targetPosition =
                coverageReadPosition +
                cushionDirection *
                FootballUnits.YardsToUnits(
                    coverageCushionYards);

            float movementSpeed =
                CalculateClosingSpeed(
                    targetPosition);

            MoveToward(
                targetPosition,
                movementSpeed);

            FaceReceiver();

            yield return null;
        }

        coverageCoroutine = null;
    }

    private Vector3 GetPredictedReceiverPosition()
    {
        if (coverageTarget == null ||
            coverageTarget.RouteRunner == null)
        {
            return coverageTarget != null
                ? coverageTarget.transform.position
                : transform.position;
        }

        Vector3 receiverPosition =
            coverageTarget.transform.position;

        Vector3 receiverVelocity =
            coverageTarget.RouteRunner.CurrentVelocity;

        receiverVelocity.y = 0f;

        if (receiverVelocity.sqrMagnitude <
            0.0001f)
        {
            return receiverPosition;
        }

        float gap =
            GetHorizontalDistance(
                transform.position,
                receiverPosition);

        float defenderSpeed =
            Mathf.Max(
                currentSpeedUnitsPerSecond,
                0.01f);

        float estimatedInterceptTime =
            gap / defenderSpeed;

        float predictionTime =
            Mathf.Clamp(
                estimatedInterceptTime,
                0f,
                maximumPredictionSeconds);

        return receiverPosition +
               receiverVelocity * predictionTime;
    }

    private void MoveToward(
    Vector3 worldTarget,
    float movementSpeed)
    {
        Vector3 toTarget =
            worldTarget - transform.position;

        toTarget.y = 0f;

        if (toTarget.magnitude <=
            stoppingDistance)
        {
            return;
        }

        Vector3 movement =
            Vector3.ClampMagnitude(
                toTarget,
                movementSpeed *
                Time.deltaTime);

        characterController.Move(movement);
    }

    private void FaceReceiver()
    {
        if (coverageTarget == null)
        {
            return;
        }

        Vector3 lookDirection =
            coverageTarget.transform.position -
            transform.position;

        lookDirection.y = 0f;

        if (lookDirection.sqrMagnitude <
            0.0001f)
        {
            return;
        }

        Quaternion desiredRotation =
            Quaternion.LookRotation(
                lookDirection,
                Vector3.up);

        transform.rotation =
            Quaternion.RotateTowards(
                transform.rotation,
                desiredRotation,
                rotationSpeed *
                Time.deltaTime);
    }

    private void OnTriggerEnter(
        Collider other)
    {
        if (!isActiveDefender ||
            sequenceController == null ||
            sequenceController.PlayIsDead)
        {
            return;
        }

        FootballReceiverTarget offensivePlayer =
            other.GetComponentInParent<
                FootballReceiverTarget>();

        if (offensivePlayer == null ||
            offensivePlayer.RouteRunner == null)
        {
            return;
        }

        /*
         * Ignore other defenders using the same prefab.
         */
        FootballDefenderController otherDefender =
            offensivePlayer.GetComponent<
                FootballDefenderController>();

        if (otherDefender != null &&
            otherDefender.IsActiveDefender)
        {
            return;
        }

        if (!offensivePlayer.RouteRunner.HasBall)
        {
            return;
        }

        StopCoverage();
        offensivePlayer.RouteRunner.StopMovement();

        sequenceController.RegisterTackle(this, offensivePlayer);
    }

    public Vector3 PredictPositionAtTime(float seconds)
    {
        if (coverageTarget == null)
        {
            return transform.position;
        }

        Vector3 direction = coverageTarget.transform.position - transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
        {
            return transform.position;
        }

        direction.Normalize();

        return transform.position + direction * currentSpeedUnitsPerSecond * Mathf.Max(0f, seconds);
    }
}