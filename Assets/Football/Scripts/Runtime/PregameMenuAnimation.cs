using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

public class PregameMenuAnimation : MonoBehaviour
{
    [Header("Title")]
    [SerializeField] private Image holeTitleImage;
    [SerializeField] private Image titleImage;

    [Header("3D Object")]
    [SerializeField] private Transform rotatingObject;
    [SerializeField] private float rotationSpeed = 30f;

    [Header("UI Elements")]
    [SerializeField] private Image backImage;
    [SerializeField] private Image balanceTab;
    [SerializeField] private RectTransform buttonsTransform;

    [Header("Fading UI Elements")]
    [SerializeField] private CanvasGroup placeBetCanvasGroup;
    [SerializeField] private CanvasGroup selectTeamCanvasGroup;
    [SerializeField] private CanvasGroup captionCanvasGroup;
    [SerializeField] private TextMeshProUGUI captionText;

    [Header("Timing")]
    [SerializeField] private float entranceDuration = 0.25f;
    [SerializeField] private float rotationDuration = 2f;

    private Coroutine exitCoroutine;

    private Coroutine animationCoroutine;

    private void Awake()
    {
        AnimatePregameMenu();
    }

    private void AnimatePregameMenu()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }

        animationCoroutine = StartCoroutine(AnimatePregameMenuRoutine());
    }

    private IEnumerator AnimatePregameMenuRoutine()
    {
        /*
         * Awake occurs before Unity has completed its first UI layout pass.
         * Waiting one frame prevents the layout system from immediately
         * replacing our starting positions.
         */
        yield return null;

        Canvas.ForceUpdateCanvases();

        holeTitleImage.gameObject.SetActive(true);
        titleImage.gameObject.SetActive(false);

        RectTransform titleRect = holeTitleImage.rectTransform;
        RectTransform backTitleRect = titleImage.rectTransform;
        RectTransform backRect = backImage.rectTransform;
        RectTransform balanceRect = balanceTab.rectTransform;

        RectTransform placeBetRect =
            placeBetCanvasGroup.GetComponent<RectTransform>();

        RectTransform selectTeamRect =
            selectTeamCanvasGroup.GetComponent<RectTransform>();

        RectTransform captionRect = captionText.rectTransform;

        // Cache initial rotations so only the X rotation changes.
        Quaternion objectStartingRotation = rotatingObject.localRotation;

        // Initial states.
        titleRect.localScale = Vector3.one * 3f;
        SetGraphicAlpha(holeTitleImage, 0f);
        SetGraphicAlpha(titleImage, 1f);

        rotatingObject.localScale = new Vector3(160f, 110f, 110f);

        rotatingObject.gameObject.SetActive(true);

        backRect.localScale = Vector3.one * 2f;

        balanceRect.anchoredPosition3D =
            new Vector3(-806f, 150f, 0f);

        buttonsTransform.anchoredPosition3D =
            new Vector3(85f, 0f, 0f);

        placeBetRect.anchoredPosition3D =
            new Vector3(0, -700f, 0f);

        placeBetCanvasGroup.alpha = 0f;

        selectTeamRect.anchoredPosition3D =
            new Vector3(0f, 366f, 0f);

        selectTeamCanvasGroup.alpha = 0f;

        captionRect.anchoredPosition3D =
            new Vector3(0f, 116f, 0f);

        captionCanvasGroup.alpha = 0f;

        // Ensure the initial state appears before animation begins.
        Canvas.ForceUpdateCanvases();
        yield return null;

        float elapsed = 0f;

        while (elapsed < entranceDuration)
        {
            float deltaTime = Time.unscaledDeltaTime;
            elapsed += deltaTime;

            float normalizedTime =
                Mathf.Clamp01(elapsed / entranceDuration);

            float t = SmoothEaseOut(normalizedTime);

            // Title scale and fade.
            titleRect.localScale = Vector3.Lerp(
                Vector3.one * 3f,
                Vector3.one * 1.5f,
                t
            );

            SetGraphicAlpha(holeTitleImage, t);

            // 3D object scale.
            rotatingObject.localScale = Vector3.Lerp(
                new Vector3(160f, 110f, 110f),
                new Vector3(80f, 55f, 55f),
                t
            );

            // Rotate around its local X axis.
            rotatingObject.Rotate(
                rotationSpeed * deltaTime,
                0f,
                0f,
                Space.Self
            );

            // Back image scale.
            backRect.localScale = Vector3.Lerp(
                Vector3.one * 2f,
                Vector3.one,
                t
            );


            // Balance tab.
            balanceRect.anchoredPosition3D = Vector3.Lerp(
                new Vector3(-806f, 150f, 0f),
                new Vector3(-462f, 150f, 0f),
                t
            );

            // Buttons.
            buttonsTransform.anchoredPosition3D = Vector3.Lerp(
                new Vector3(85f, 0f, 0f),
                new Vector3(-115f, 0f, 0f),
                t
            );

            // Place bet.
            placeBetRect.anchoredPosition3D = Vector3.Lerp(
                new Vector3(0f, -700f, 0f),
                new Vector3(0f, -300f, 0f),
                t
            );

            placeBetCanvasGroup.alpha = t;

            // Select team.
            selectTeamRect.anchoredPosition3D = Vector3.Lerp(
                new Vector3(0f, 166f, 0f),
                new Vector3(0f, 366f, 0f),
                t
            );

            selectTeamCanvasGroup.alpha = t;

            // Caption.
            captionRect.anchoredPosition3D = Vector3.Lerp(
                new Vector3(0f, -84f, 0f),
                new Vector3(0f, 116f, 0f),
                t
            );

            captionCanvasGroup.alpha = t;

            yield return null;
        }

        // Apply exact final values.
        titleRect.localScale = Vector3.one * 1.5f;
        backTitleRect.localScale = Vector3.one * 1.5f;
        SetGraphicAlpha(holeTitleImage, 1f);

        rotatingObject.localScale = new Vector3(80f, 55f, 55f);

        backRect.localScale = Vector3.one;

        balanceRect.anchoredPosition3D =
            new Vector3(-462f, 150f, 0f);

        buttonsTransform.anchoredPosition3D =
            new Vector3(-115f, 0f, 0f);

        placeBetRect.anchoredPosition3D =
            new Vector3(0f, -300f, 0f);

        placeBetCanvasGroup.alpha = 1f;

        selectTeamRect.anchoredPosition3D =
            new Vector3(0f, 366f, 0f);

        selectTeamCanvasGroup.alpha = 1f;

        captionRect.anchoredPosition3D =
            new Vector3(0f, 116f, 0f);

        captionCanvasGroup.alpha = 1f;

        // Continue rotating until two total seconds have elapsed.
        float remainingRotationTime =
            Mathf.Max(0f, rotationDuration - entranceDuration);

        elapsed = 0f;

        while (elapsed < remainingRotationTime)
        {
            float deltaTime = Time.unscaledDeltaTime;
            elapsed += deltaTime;

            rotatingObject.Rotate(
                rotationSpeed * deltaTime,
                0f,
                0f,
                Space.Self
            );

            yield return null;
        }

        holeTitleImage.gameObject.SetActive(false);
        titleImage.gameObject.SetActive(true);

        rotatingObject.gameObject.SetActive(false);

        animationCoroutine = null;
    }

    private static void SetGraphicAlpha(Graphic graphic, float alpha)
    {
        Color color = graphic.color;
        color.a = alpha;
        graphic.color = color;
    }

    private static void SetRectOffsets(
        RectTransform rect,
        float left,
        float right,
        float top,
        float bottom)
    {
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    private static float SmoothEaseOut(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }

    public void PlayExitAnimation(Action onFinished = null)
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }

        if (exitCoroutine != null)
        {
            StopCoroutine(exitCoroutine);
        }

        exitCoroutine = StartCoroutine(
            ExitAnimationRoutine(onFinished));
    }

    private IEnumerator ExitAnimationRoutine(Action onFinished)
    {
        Canvas.ForceUpdateCanvases();

        rotatingObject.gameObject.SetActive(false);

        RectTransform titleRect = titleImage.rectTransform;
        Image currentTitleImage = titleImage;

        if (!titleImage.gameObject.activeInHierarchy)
        {
            titleRect = holeTitleImage.rectTransform;
            currentTitleImage = holeTitleImage;
        }

        
        RectTransform backRect = backImage.rectTransform;
        RectTransform balanceRect = balanceTab.rectTransform;

        RectTransform placeBetRect =
            placeBetCanvasGroup.GetComponent<RectTransform>();

        RectTransform selectTeamRect =
            selectTeamCanvasGroup.GetComponent<RectTransform>();

        RectTransform captionRect = captionText.rectTransform;

        /*
         * Capture the current values rather than assuming the entrance
         * animation completed perfectly.
         */
        Vector3 titleStartScale = titleRect.localScale;
        float titleStartAlpha = currentTitleImage.color.a;

        Vector3 objectStartScale = rotatingObject.localScale;

        Vector3 backStartScale = backRect.localScale;

        Vector3 balanceStartPosition =
            balanceRect.anchoredPosition3D;

        Vector3 buttonsStartPosition =
            buttonsTransform.anchoredPosition3D;

        Vector3 placeBetStartPosition =
            placeBetRect.anchoredPosition3D;

        Vector3 selectTeamStartPosition =
            selectTeamRect.anchoredPosition3D;

        Vector3 captionStartPosition =
            captionRect.anchoredPosition3D;

        float placeBetStartAlpha =
            placeBetCanvasGroup.alpha;

        float selectTeamStartAlpha =
            selectTeamCanvasGroup.alpha;

        float captionStartAlpha =
            captionCanvasGroup.alpha;

        float elapsed = 0f;

        while (elapsed < entranceDuration)
        {
            float deltaTime = Time.unscaledDeltaTime;
            elapsed += deltaTime;

            float normalizedTime =
                Mathf.Clamp01(elapsed / entranceDuration);

            float t = SmoothEaseOut(normalizedTime);

            // Title grows and fades out.
            titleRect.localScale = Vector3.Lerp(
                titleStartScale,
                Vector3.one * 3f,
                t);

            SetGraphicAlpha(
                currentTitleImage,
                Mathf.Lerp(titleStartAlpha, 0f, t));

            // 3D object grows back to its starting scale.
            rotatingObject.localScale = Vector3.Lerp(
                objectStartScale,
                new Vector3(160f, 110f, 110f),
                t);

            rotatingObject.Rotate(
                rotationSpeed * deltaTime,
                0f,
                0f,
                Space.Self);

            // Back image grows and restores its original offsets.
            backRect.localScale = Vector3.Lerp(
                backStartScale,
                Vector3.one * 2f,
                t);

            // Slide everything back to where it entered from.
            balanceRect.anchoredPosition3D = Vector3.Lerp(
                balanceStartPosition,
                new Vector3(-462f, 650f, 0f),
                t);

            buttonsTransform.anchoredPosition3D = Vector3.Lerp(
                buttonsStartPosition,
                new Vector3(85f, 0f, 0f),
                t);

            placeBetRect.anchoredPosition3D = Vector3.Lerp(
                placeBetStartPosition,
                new Vector3(-700f, 0f, 0f),
                t);

            selectTeamRect.anchoredPosition3D = Vector3.Lerp(
                selectTeamStartPosition,
                new Vector3(366f, 0f, 0f),
                t);

            captionRect.anchoredPosition3D = Vector3.Lerp(
                captionStartPosition,
                new Vector3(116f, 0f, 0f),
                t);

            placeBetCanvasGroup.alpha =
                Mathf.Lerp(placeBetStartAlpha, 0f, t);

            selectTeamCanvasGroup.alpha =
                Mathf.Lerp(selectTeamStartAlpha, 0f, t);

            captionCanvasGroup.alpha =
                Mathf.Lerp(captionStartAlpha, 0f, t);

            yield return null;
        }

        // Apply exact final exit values.
        titleRect.localScale = Vector3.one * 3f;
        SetGraphicAlpha(currentTitleImage, 0f);

        rotatingObject.localScale =
            new Vector3(160f, 110f, 110f);

        backRect.localScale = Vector3.one * 2f;

        balanceRect.anchoredPosition3D =
            new Vector3(-462f, 650f, 0f);

        buttonsTransform.anchoredPosition3D =
            new Vector3(85f, 0f, 0f);

        placeBetRect.anchoredPosition3D =
            new Vector3(-700f, 0f, 0f);

        selectTeamRect.anchoredPosition3D =
            new Vector3(366f, 0f, 0f);

        captionRect.anchoredPosition3D =
            new Vector3(116f, 0f, 0f);

        placeBetCanvasGroup.alpha = 0f;
        selectTeamCanvasGroup.alpha = 0f;
        captionCanvasGroup.alpha = 0f;

        exitCoroutine = null;
        onFinished?.Invoke();
    }

    public void ResetImmediately()
    {
        StopAllCoroutines();

        AnimatePregameMenu();
    }
}