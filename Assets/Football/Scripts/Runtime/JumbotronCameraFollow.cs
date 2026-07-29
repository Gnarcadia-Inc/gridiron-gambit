using UnityEngine;

[RequireComponent(typeof(Camera))]
public class JumbotronCameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Tooltip("The point, relative to the target, that the camera looks toward.")]
    [SerializeField] private Vector3 lookOffset = new Vector3(0f, 1.5f, 0f);

    [Header("Camera Position")]
    [Tooltip("Camera position relative to the target's rotation.")]
    [SerializeField]
    private Vector3 followOffset =
        new Vector3(0f, 3f, -8f);

    [SerializeField, Min(0f)]
    private float positionSmoothSpeed = 6f;

    [SerializeField, Min(0f)]
    private float rotationSmoothSpeed = 8f;

    [Header("Automatic Zoom")]
    [Tooltip("Distance at which the camera uses the minimum field of view.")]
    [SerializeField, Min(0.01f)]
    private float nearDistance = 4f;

    [Tooltip("Distance at which the camera uses the maximum field of view.")]
    [SerializeField, Min(0.01f)]
    private float farDistance = 15f;

    [Tooltip("Smaller field of view means more zoomed in.")]
    [SerializeField, Range(1f, 179f)]
    private float zoomedInFieldOfView = 30f;

    [Tooltip("Larger field of view means more zoomed out.")]
    [SerializeField, Range(1f, 179f)]
    private float zoomedOutFieldOfView = 65f;

    [SerializeField, Min(0f)]
    private float zoomSmoothSpeed = 5f;

    private Camera broadcastCamera;

    private void Awake()
    {
        broadcastCamera = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        FollowTarget();
        LookAtTarget();
        UpdateZoom();
    }

    public void SetBall(Transform ball)
    {
        target = ball;
    }

    private void FollowTarget()
    {
        // Rotates the offset along with the target.
        Vector3 desiredPosition =
            target.position + target.rotation * followOffset;

        float positionT =
            1f - Mathf.Exp(-positionSmoothSpeed * Time.deltaTime);

        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            positionT
        );
    }

    private void LookAtTarget()
    {
        Vector3 lookPosition =
            target.position + target.rotation * lookOffset;

        Vector3 lookDirection = lookPosition - transform.position;

        if (lookDirection.sqrMagnitude < 0.001f)
        {
            return;
        }

        Quaternion desiredRotation =
            Quaternion.LookRotation(lookDirection, target.up);

        float rotationT =
            1f - Mathf.Exp(-rotationSmoothSpeed * Time.deltaTime);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            desiredRotation,
            rotationT
        );
    }

    private void UpdateZoom()
    {
        Vector3 lookPosition =
            target.position + target.rotation * lookOffset;

        float distance =
            Vector3.Distance(transform.position, lookPosition);

        float distancePercent = Mathf.InverseLerp(
            nearDistance,
            farDistance,
            distance
        );

        float desiredFieldOfView = Mathf.Lerp(
            zoomedInFieldOfView,
            zoomedOutFieldOfView,
            distancePercent
        );

        float zoomT =
            1f - Mathf.Exp(-zoomSmoothSpeed * Time.deltaTime);

        broadcastCamera.fieldOfView = Mathf.Lerp(
            broadcastCamera.fieldOfView,
            desiredFieldOfView,
            zoomT
        );
    }

    private void OnValidate()
    {
        farDistance = Mathf.Max(farDistance, nearDistance + 0.01f);
        zoomedOutFieldOfView = Mathf.Max(
            zoomedOutFieldOfView,
            zoomedInFieldOfView
        );
    }
}