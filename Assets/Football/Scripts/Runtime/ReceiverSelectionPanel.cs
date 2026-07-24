using System.Collections.Generic;
using UnityEngine;

public class ReceiverSelectionPanel : MonoBehaviour
{
    [Header("UI")]

    [SerializeField]
    private Canvas canvas;

    [SerializeField]
    private ReceiverChoiceButton buttonPrefab;

    [SerializeField]
    private Transform buttonContainer;

    [SerializeField]
    private Camera gameplayCamera;

    [Header("Position Sprites")]

    [SerializeField]
    private List<ReceiverButtonSprite>
        roleSprites = new();

    private readonly List<ReceiverChoiceButton>
        spawnedButtons = new();

    private FootballPlaySequenceController controller;

    private ReceiverChoiceButton selectedButton;

    public FootballReceiverTarget SelectedReceiver =>
        selectedButton != null
            ? selectedButton.Receiver
            : null;

    public bool HasSelection =>
        selectedButton != null;

    private void Reset()
    {
        canvas =
            GetComponentInParent<Canvas>();

        buttonContainer =
            transform;
    }

    public void Show(
        IReadOnlyList<RuntimeReceiverAssignment> receivers,
        FootballPlaySequenceController playController)
    {
        ClearButtons();

        controller = playController;
        selectedButton = null;

        gameObject.SetActive(true);

        if (canvas == null)
        {
            canvas =
                GetComponentInParent<Canvas>();
        }

        if (gameplayCamera == null)
        {
            gameplayCamera =
                Camera.main;
        }

        if (buttonContainer == null)
        {
            buttonContainer =
                transform;
        }

        foreach (
            RuntimeReceiverAssignment receiverAssignment
            in receivers)
        {
            if (receiverAssignment == null ||
                receiverAssignment.receiver == null)
            {
                continue;
            }

            OffensiveRole role =
                receiverAssignment.assignment.role;

            ReceiverButtonSprite spriteEntry =
                GetSpriteEntry(role);

            ReceiverChoiceButton newButton =
                Instantiate(
                    buttonPrefab,
                    buttonContainer);

            newButton.Configure(
                receiverAssignment.receiver,
                receiverAssignment.assignment.route,
                this,
                canvas,
                gameplayCamera,
                spriteEntry != null
                    ? spriteEntry.offSprite
                    : null,
                spriteEntry != null
                    ? spriteEntry.onSprite
                    : null);

            /*
             * All buttons start unselected/off.
             */
            newButton.SetSelected(false);

            spawnedButtons.Add(newButton);
        }
    }

    public void SelectButton(
        ReceiverChoiceButton clickedButton)
    {
        if (clickedButton == null)
        {
            return;
        }

        selectedButton = clickedButton;

        /*
         * Exactly one button is On.
         * Every other button is Off.
         */
        foreach (ReceiverChoiceButton button
                 in spawnedButtons)
        {
            if (button == null)
            {
                continue;
            }

            button.SetSelected(
                button == selectedButton);
        }

        if (controller != null)
        {
            controller.UpdatePendingThrowTarget(
                selectedButton.Receiver);
        }
    }

    public void Hide()
    {
        ClearButtons();

        selectedButton = null;
        controller = null;

        gameObject.SetActive(false);
    }

    private ReceiverButtonSprite GetSpriteEntry(
        OffensiveRole role)
    {
        foreach (ReceiverButtonSprite entry
                 in roleSprites)
        {
            if (entry != null &&
                entry.role == role)
            {
                return entry;
            }
        }

        return null;
    }

    private void ClearButtons()
    {
        foreach (ReceiverChoiceButton button
                 in spawnedButtons)
        {
            if (button != null)
            {
                Destroy(button.gameObject);
            }
        }

        spawnedButtons.Clear();
    }
}