using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class FootballBall : MonoBehaviour
{
    [SerializeField]
    private float spinSpeed = 720f;

    private Coroutine flightCoroutine;

    private FootballReceiverTarget intendedReceiver;
    private FootballPlaySequenceController sequenceController;

    private bool hasBeenCaught;

    public bool HasBeenCaught => hasBeenCaught;

    private bool canBeCaught;

    public bool CanBeCaught =>
        canBeCaught;

    [SerializeField]
    private Vector3 ballModelRotationOffset;

    public void SetCatchEnabled(bool enabled)
    {
        canBeCaught = enabled;
    }

    public void ThrowLob(Vector3 start, Vector3 predictedTarget, float flightTime, float lobHeight, FootballReceiverTarget receiver, FootballPlaySequenceController controller)
    {
        if (flightCoroutine != null)
        {
            StopCoroutine(flightCoroutine);
        }

        intendedReceiver = receiver;
        sequenceController = controller;
        hasBeenCaught = false;

        transform.SetParent(null);
        transform.position = start;

        flightCoroutine =
            StartCoroutine(
                LobRoutine(
                    start,
                    predictedTarget,
                    flightTime,
                    lobHeight));
    }

    public void AttachToReceiver(
    Transform catchPoint)
    {
        hasBeenCaught = true;

        if (flightCoroutine != null)
        {
            StopCoroutine(flightCoroutine);
            flightCoroutine = null;
        }

        transform.SetParent(catchPoint);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    private IEnumerator LobRoutine(
    Vector3 startPosition,
    Vector3 targetPosition,
    float flightDuration,
    float arcHeight)
    {
        Vector3 controlPoint =
            Vector3.Lerp(
                startPosition,
                targetPosition,
                0.5f);

        controlPoint.y += arcHeight;

        Vector3 previousPosition =
            startPosition;

        float elapsed = 0f;

        while (elapsed < flightDuration)
        {
            elapsed += Time.deltaTime;

            float normalizedTime =
                Mathf.Clamp01(
                    elapsed / flightDuration);

            Vector3 newPosition =
                EvaluateQuadraticBezier(
                    startPosition,
                    controlPoint,
                    targetPosition,
                    normalizedTime);

            Vector3 travelDirection =
                newPosition - previousPosition;

            transform.position =
                newPosition;

            FaceTravelDirection(
                travelDirection);

            previousPosition =
                newPosition;

            yield return null;
        }

        transform.position =
            targetPosition;

        if (!hasBeenCaught)
        {
            sequenceController.RegisterIncompletion(transform.position);
        }
    }

    private void FaceTravelDirection(
    Vector3 travelDirection)
    {
        if (travelDirection.sqrMagnitude <
            0.000001f)
        {
            return;
        }

        Quaternion travelRotation =
            Quaternion.LookRotation(
                travelDirection.normalized,
                Vector3.up);

        transform.rotation =
            travelRotation *
            Quaternion.Euler(
                ballModelRotationOffset);
    }

    private void OnTriggerEnter(
    Collider other)
    {
        if (!canBeCaught || hasBeenCaught || sequenceController == null)
        {
            return;
        }

        FootballDefenderController defender =
            other.GetComponentInParent<
                FootballDefenderController>();

        if (defender != null &&
            defender.IsActiveDefender)
        {
            hasBeenCaught = true;

            if (flightCoroutine != null)
            {
                StopCoroutine(flightCoroutine);
                flightCoroutine = null;
            }

            defender.ReceiveInterception(this);
            return;
        }

        FootballReceiverTarget receiver =
            other.GetComponentInParent<
                FootballReceiverTarget>();

        if (receiver != intendedReceiver)
        {
            return;
        }

        CompleteCatch();
    }

    private void CompleteCatch()
    {
        if (hasBeenCaught ||
            intendedReceiver == null ||
            sequenceController == null)
        {
            return;
        }

        sequenceController.CompleteCatch(
            intendedReceiver,
            this);
    }

    private static Vector3 EvaluateQuadraticBezier(
        Vector3 start,
        Vector3 control,
        Vector3 end,
        float t)
    {
        float inverse = 1f - t;

        return
            inverse * inverse * start +
            2f * inverse * t * control +
            t * t * end;
    }
}