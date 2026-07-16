using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ReceiverChoiceButton : MonoBehaviour
{
    [Header("UI")]

    [SerializeField]
    private Button button;

    [SerializeField]
    private TMP_Text label;

    [SerializeField]
    private RectTransform rectTransform;

    [Header("World Tracking")]

    [SerializeField]
    private Vector3 worldOffset =
        new Vector3(0f, 2.5f, 0f);

    [SerializeField]
    private bool hideWhenOffScreen = true;

    private FootballReceiverTarget receiver;
    private FootballPlaySequenceController controller;

    private Canvas canvas;
    private RectTransform canvasRect;
    private Camera worldCamera;

    private void Reset()
    {
        button = GetComponent<Button>();
        label = GetComponentInChildren<TMP_Text>();
        rectTransform = GetComponent<RectTransform>();
    }

    public void Configure(
        FootballReceiverTarget receiverTarget,
        FootballRoute route,
        FootballPlaySequenceController playController,
        Canvas parentCanvas,
        Camera gameplayCamera)
    {
        receiver = receiverTarget;
        controller = playController;
        canvas = parentCanvas;
        worldCamera = gameplayCamera;

        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        if (canvas != null)
        {
            canvasRect =
                canvas.transform as RectTransform;
        }

        string receiverName =
            receiver != null
                ? receiver.DisplayName
                : "Receiver";

        string routeName =
            route != null
                ? route.routeName
                : "No Route";

        if (label != null)
        {
            label.text =
                $"{receiverName}\n{routeName}";
        }

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(SelectReceiver);
        }

        UpdateScreenPosition();
    }

    private void LateUpdate()
    {
        UpdateScreenPosition();
    }

    private void UpdateScreenPosition()
    {
        if (receiver == null ||
            canvas == null ||
            canvasRect == null ||
            rectTransform == null)
        {
            return;
        }

        Transform trackedTransform =
            receiver.CatchPoint != null
                ? receiver.CatchPoint
                : receiver.transform;

        Vector3 worldPosition =
            trackedTransform.position +
            worldOffset;

        Camera conversionCamera =
            canvas.renderMode ==
            RenderMode.ScreenSpaceOverlay
                ? null
                : worldCamera;

        Vector3 viewportPosition =
            worldCamera.WorldToViewportPoint(
                worldPosition);

        bool isBehindCamera =
            viewportPosition.z <= 0f;

        bool isOutsideScreen =
            viewportPosition.x < 0f ||
            viewportPosition.x > 1f ||
            viewportPosition.y < 0f ||
            viewportPosition.y > 1f;

        bool shouldHide =
            hideWhenOffScreen &&
            (isBehindCamera || isOutsideScreen);

        if (button != null)
        {
            button.gameObject.SetActive(
                !shouldHide);
        }

        if (shouldHide)
        {
            return;
        }

        Vector2 screenPoint =
            RectTransformUtility
                .WorldToScreenPoint(
                    worldCamera,
                    worldPosition);

        if (RectTransformUtility
            .ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPoint,
                conversionCamera,
                out Vector2 localPoint))
        {
            rectTransform.anchoredPosition =
                localPoint;
        }
    }

    private void SelectReceiver()
    {
        if (controller != null &&
            receiver != null)
        {
            controller.SelectThrowTarget(receiver);
        }
    }
}