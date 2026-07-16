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

    public void ThrowLob(
        Vector3 start,
        Vector3 predictedTarget,
        float flightTime,
        float lobHeight,
        FootballReceiverTarget receiver,
        FootballPlaySequenceController controller)
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
        if (hasBeenCaught)
        {
            return;
        }

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
        Vector3 start,
        Vector3 target,
        float flightTime,
        float lobHeight)
    {
        flightTime = Mathf.Max(0.1f, flightTime);

        Vector3 midpoint =
            (start + target) * 0.5f +
            Vector3.up * lobHeight;

        float elapsed = 0f;

        while (elapsed < flightTime &&
               !hasBeenCaught)
        {
            elapsed += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / flightTime);

            Vector3 oldPosition =
                transform.position;

            Vector3 newPosition =
                EvaluateQuadraticBezier(
                    start,
                    midpoint,
                    target,
                    t);

            transform.position = newPosition;

            Vector3 direction =
                newPosition - oldPosition;

            if (direction.sqrMagnitude > 0.0001f)
            {
                transform.rotation =
                    Quaternion.LookRotation(direction);
            }

            transform.Rotate(
                Vector3.forward,
                spinSpeed * Time.deltaTime,
                Space.Self);

            yield return null;
        }

        if (!hasBeenCaught &&
            intendedReceiver != null)
        {
            float catchDistance =
                Vector3.Distance(
                    transform.position,
                    intendedReceiver.CatchPoint.position);

            if (catchDistance <= 1.5f)
            {
                CompleteCatch();
            }
        }

        flightCoroutine = null;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasBeenCaught ||
            intendedReceiver == null)
        {
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