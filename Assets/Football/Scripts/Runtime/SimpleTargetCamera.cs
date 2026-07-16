using UnityEngine;

public class SimpleTargetCamera : MonoBehaviour
{
    [SerializeField]
    private Transform target;

    private Vector3 localOffset =
        new Vector3(0f, 4f, -6f);

    private float positionDamping = 8f;

    [SerializeField]
    private float rotationDamping = 10f;

    [SerializeField]
    private bool useTargetRotation = true;

    public Transform Target => target;

    public void SetTarget(
        Transform newTarget)
    {
        target = newTarget;
    }

    public void SetTarget(
        Transform newTarget,
        Vector3 newLocalOffset)
    {
        target = newTarget;
        localOffset = newLocalOffset;
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 desiredPosition;

        if (useTargetRotation)
        {
            desiredPosition =
                target.position +
                target.rotation * localOffset;
        }
        else
        {
            desiredPosition =
                target.position + localOffset;
        }

        float positionBlend =
            1f -
            Mathf.Exp(
                -positionDamping *
                Time.unscaledDeltaTime);

        transform.position =
            Vector3.Lerp(
                transform.position,
                desiredPosition,
                positionBlend);

        Vector3 lookDirection =
            target.position -
            transform.position;

        if (lookDirection.sqrMagnitude >
            0.0001f)
        {
            Quaternion desiredRotation =
                Quaternion.LookRotation(
                    lookDirection);

            float rotationBlend =
                1f -
                Mathf.Exp(
                    -rotationDamping *
                    Time.unscaledDeltaTime);

            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    desiredRotation,
                    rotationBlend);
        }
    }
}