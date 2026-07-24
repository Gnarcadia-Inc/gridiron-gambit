using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ReceiverChoiceButton : MonoBehaviour
{
    [Header("UI")]

    [SerializeField]
    private Button button;

    [SerializeField]
    private Image buttonImage;

    [SerializeField]
    private TMP_Text label;

    [SerializeField]
    private RectTransform rectTransform;

    [SerializeField]
    private CanvasGroup canvasGroup;

    [Header("Selection Sprites")]

    [SerializeField]
    private Sprite offSprite;

    [SerializeField]
    private Sprite onSprite;

    private Vector3 worldOffset =
        new Vector3(0f, 5f, 0f);

    [SerializeField]
    private bool hideWhenOffScreen = true;

    private FootballReceiverTarget receiver;
    private FootballRoute route;
    private ReceiverSelectionPanel selectionPanel;

    private Canvas canvas;
    private RectTransform canvasRect;
    private Camera worldCamera;

    private bool isSelected;

    public FootballReceiverTarget Receiver =>
        receiver;

    public bool IsSelected =>
        isSelected;

    private void Reset()
    {
        button =
            GetComponent<Button>();

        buttonImage =
            GetComponent<Image>();

        label =
            GetComponentInChildren<TMP_Text>();

        rectTransform =
            GetComponent<RectTransform>();

        canvasGroup =
            GetComponent<CanvasGroup>();
    }

    public void Configure(
        FootballReceiverTarget receiverTarget,
        FootballRoute assignedRoute,
        ReceiverSelectionPanel panel,
        Canvas parentCanvas,
        Camera gameplayCamera,
        Sprite roleOffSprite,
        Sprite roleOnSprite)
    {
        receiver = receiverTarget;
        route = assignedRoute;
        selectionPanel = panel;

        canvas = parentCanvas;
        worldCamera = gameplayCamera;

        offSprite = roleOffSprite;
        onSprite = roleOnSprite;

        if (rectTransform == null)
        {
            rectTransform =
                GetComponent<RectTransform>();
        }

        if (buttonImage == null)
        {
            buttonImage =
                GetComponent<Image>();
        }

        if (canvasGroup == null)
        {
            canvasGroup =
                GetComponent<CanvasGroup>();
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
            button.onClick.AddListener(
                HandleButtonClicked);
        }

        /*
         * Every button begins in its unselected state.
         */
        SetSelected(false);

        UpdateScreenPosition();
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;

        if (buttonImage == null)
        {
            return;
        }

        Sprite desiredSprite =
            isSelected
                ? onSprite
                : offSprite;

        if (desiredSprite != null)
        {
            buttonImage.sprite =
                desiredSprite;
        }
    }

    private void HandleButtonClicked()
    {
        if (selectionPanel == null ||
            receiver == null)
        {
            return;
        }

        /*
         * This does not throw the ball.
         * It only tells the panel which receiver
         * is currently selected.
         */
        selectionPanel.SelectButton(this);
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
            rectTransform == null ||
            worldCamera == null)
        {
            return;
        }

        Transform trackedTransform = receiver.transform;

        Vector3 worldPosition =
            trackedTransform.position +
            worldOffset;

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

        if (canvasGroup != null)
        {
            canvasGroup.alpha =
                shouldHide ? 0f : 1f;

            canvasGroup.interactable =
                !shouldHide;

            canvasGroup.blocksRaycasts =
                !shouldHide;
        }

        if (shouldHide)
        {
            return;
        }

        Vector2 screenPoint =
            RectTransformUtility.WorldToScreenPoint(
                worldCamera,
                worldPosition);

        Camera canvasCamera =
            canvas.renderMode ==
            RenderMode.ScreenSpaceOverlay
                ? null
                : worldCamera;

        if (RectTransformUtility
            .ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPoint,
                canvasCamera,
                out Vector2 localPoint))
        {
            rectTransform.anchoredPosition =
                localPoint;
        }
    }
}