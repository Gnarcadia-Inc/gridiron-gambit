using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FootballRouteRunner : MonoBehaviour
{
    [SerializeField]
    private CharacterController characterController;

    [SerializeField]
    private float rotationSpeed = 720f;

    [SerializeField]
    private float stoppingDistance = 0.05f;

    [SerializeField]
    private float defaultReceiverSpeed = 1.5f;

    private Coroutine movementCoroutine;

    private Vector3 previousPosition;
    private Vector3 currentVelocity;

    private bool isRunningRoute;
    private bool hasBall = false;

    public Vector3 CurrentVelocity => currentVelocity;
    public bool IsRunningRoute => isRunningRoute;
    public bool HasBall => hasBall;

    public event System.Action RouteCompleted;

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

        previousPosition = transform.position;
    }

    private void LateUpdate()
    {
        if (Time.deltaTime > 0.0001f)
        {
            currentVelocity =
                (transform.position - previousPosition) /
                Time.deltaTime;
        }

        previousPosition = transform.position;
    }

    public void SetHasBall(bool value)
    {
        hasBall = value;
    }

    public void PrepareForPlay(Transform playOrigin, Vector2 startingOffsetYards)
    {
        StopMovement();

        hasBall = false;
        isRunningRoute = false;
        currentVelocity = Vector3.zero;

        Vector3 localStartPosition =
            new Vector3(
                FootballUnits.YardsToUnits(
                    startingOffsetYards.x),
                0f,
                FootballUnits.YardsToUnits(
                    startingOffsetYards.y));

        Vector3 worldStartPosition =
            playOrigin.TransformPoint(
                localStartPosition);

        SetPositionSafely(worldStartPosition);

        transform.rotation = playOrigin.rotation;
        previousPosition = transform.position;
    }

    public void StartPreparedRoute(
        FootballRoute route,
        Transform playOrigin,
        float releaseDelay = 0f)
    {
        StopMovement();

        movementCoroutine =
            StartCoroutine(
                RunPreparedRouteRoutine(
                    route,
                    playOrigin,
                    releaseDelay));
    }

    public void StartQuarterbackDropback(
    Transform playOrigin,
    float distanceYards,
    float receiverSpeedYardsPerSecond)
    {
        StopMovement();

        float quarterbackSpeed =
            FootballUnits.YardsToUnits(
                receiverSpeedYardsPerSecond * 0.5f);

        movementCoroutine =
            StartCoroutine(
                QuarterbackDropbackRoutine(
                    playOrigin,
                    FootballUnits.YardsToUnits(distanceYards),
                    quarterbackSpeed));
    }

    private IEnumerator QuarterbackDropbackRoutine(
    Transform playOrigin,
    float distance,
    float movementSpeed)
    {
        Vector3 forwardDirection = playOrigin.forward;
        forwardDirection.y = 0f;
        forwardDirection.Normalize();

        Vector3 backwardDirection = -forwardDirection;

        Vector3 startPosition = transform.position;
        Vector3 targetPosition =
            startPosition + backwardDirection * distance;

        Quaternion forwardRotation =
            Quaternion.LookRotation(
                forwardDirection,
                Vector3.up);

        while (GetHorizontalDistance(
                   transform.position,
                   targetPosition) >
               stoppingDistance)
        {
            /*
             * Keep the QB facing downfield even though
             * his movement direction is backward.
             */
            transform.rotation = forwardRotation;

            Vector3 toTarget =
                targetPosition - transform.position;

            toTarget.y = 0f;

            Vector3 movement =
                Vector3.ClampMagnitude(
                    toTarget,
                    movementSpeed * Time.deltaTime);

            characterController.Move(movement);

            yield return null;
        }

        transform.rotation = forwardRotation;
        movementCoroutine = null;
    }

    public void StartRunningForward(
        Transform playOrigin,
        float speedYardsPerSecond)
    {
        StopMovement();

        hasBall = true;

        movementCoroutine =
            StartCoroutine(
                RunForwardRoutine(
                    playOrigin,
                    FootballUnits.YardsToUnits(
                        speedYardsPerSecond)));
    }

    public void StopMovement()
    {
        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
            movementCoroutine = null;
        }

        isRunningRoute = false;
        currentVelocity = Vector3.zero;
    }

    public Vector3 PredictPosition(float secondsAhead)
    {
        secondsAhead = Mathf.Max(0f, secondsAhead);

        Vector3 horizontalVelocity = currentVelocity;
        horizontalVelocity.y = 0f;

        return transform.position +
               horizontalVelocity * secondsAhead;
    }

    private IEnumerator RunPreparedRouteRoutine(
        FootballRoute route,
        Transform playOrigin,
        float releaseDelay)
    {
        if (route == null)
        {
            movementCoroutine = null;
            yield break;
        }

        if (releaseDelay > 0f)
        {
            yield return new WaitForSeconds(
                releaseDelay);
        }

        isRunningRoute = true;

        List<RouteNode> nodes =
            RouteCompiler.Compile(route);

        Vector3 routeWorldOrigin =
            transform.position;

        for (int i = 1; i < nodes.Count; i++)
        {
            RouteNode node = nodes[i];

            if (node.delay > 0f)
            {
                yield return new WaitForSeconds(
                    node.delay);
            }

            Vector3 worldOffset =
                playOrigin.TransformDirection(
                    node.localPosition);

            Vector3 worldTarget =
                routeWorldOrigin + worldOffset;

            yield return MoveToTargetRoutine(
                worldTarget,
                node.speed,
                true);

            if (node.stopAtNode)
            {
                break;
            }
        }

        isRunningRoute = false;

        currentVelocity = Vector3.zero;

        RouteCompleted?.Invoke();

        movementCoroutine = null;
    }

    private IEnumerator MoveToTargetRoutine(
        Vector3 worldTarget,
        float movementSpeed,
        bool preserveRouteState)
    {
        while (GetHorizontalDistance(
                   transform.position,
                   worldTarget) >
               stoppingDistance)
        {
            Vector3 toTarget =
                worldTarget - transform.position;

            toTarget.y = 0f;

            if (toTarget.sqrMagnitude <
                0.0001f)
            {
                break;
            }

            Vector3 direction =
                toTarget.normalized;

            RotateToward(direction);

            float maximumMovement =
                movementSpeed * Time.deltaTime;

            Vector3 movement =
                Vector3.ClampMagnitude(
                    toTarget,
                    maximumMovement);

            characterController.Move(movement * defaultReceiverSpeed);

            yield return null;
        }

        if (!preserveRouteState)
        {
            movementCoroutine = null;
        }
    }

    private IEnumerator RunForwardRoutine(
        Transform playOrigin,
        float speed)
    {
        while (true)
        {
            Vector3 forward =
                playOrigin.forward;

            forward.y = 0f;
            forward.Normalize();

            RotateToward(forward);

            characterController.Move(
                forward * speed * Time.deltaTime);

            yield return null;
        }
    }

    private void RotateToward(
        Vector3 direction)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude <
            0.0001f)
        {
            return;
        }

        Quaternion targetRotation =
            Quaternion.LookRotation(direction);

        transform.rotation =
            Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime);
    }

    private void SetPositionSafely(
        Vector3 position)
    {
        bool wasEnabled =
            characterController.enabled;

        characterController.enabled = false;
        transform.position = position;
        characterController.enabled = wasEnabled;
    }

    /// <summary>
    /// Compatibility method for older play-controller scripts.
    /// Places the player and then begins the route.
    /// </summary>
    public void RunRoute(
        FootballRoute route,
        Transform playOrigin,
        Vector2 startingOffsetYards,
        float releaseDelay = 0f)
    {
        PrepareForPlay(
            playOrigin,
            startingOffsetYards);

        StartPreparedRoute(
            route,
            playOrigin,
            releaseDelay);
    }

    /// <summary>
    /// Compatibility method for older scripts.
    /// </summary>
    public void StopRoute()
    {
        StopMovement();
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
}