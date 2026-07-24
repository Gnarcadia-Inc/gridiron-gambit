using UnityEngine;

public sealed class WorldLogoUISync : MonoBehaviour
{
    [SerializeField] private RectTransform holeTarget;
    [SerializeField] private Canvas canvas;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Transform logoObject;

    [Tooltip("Distance from the camera along the screen-point ray.")]
    [SerializeField] private float distanceFromCamera = 5f;

    private void LateUpdate()
    {
        if (!holeTarget || !canvas || !worldCamera || !logoObject)
            return;

        Camera uiCamera =
            canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : canvas.worldCamera;

        Vector2 screenPoint =
            RectTransformUtility.WorldToScreenPoint(uiCamera, holeTarget.position);

        Ray ray = worldCamera.ScreenPointToRay(screenPoint);
        logoObject.position = ray.GetPoint(distanceFromCamera);
    }
}