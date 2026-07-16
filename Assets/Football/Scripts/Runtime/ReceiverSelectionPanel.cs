using System.Collections.Generic;
using UnityEngine;

public class ReceiverSelectionPanel : MonoBehaviour
{
    [SerializeField]
    private Canvas canvas;

    [SerializeField]
    private ReceiverChoiceButton buttonPrefab;

    [SerializeField]
    private Camera gameplayCamera;

    private readonly List<GameObject>
        spawnedButtons = new();

    private void Reset()
    {
        canvas = GetComponentInParent<Canvas>();
    }

    public void Show(
        IReadOnlyList<RuntimeReceiverAssignment> receivers,
        FootballPlaySequenceController controller)
    {
        ClearButtons();

        gameObject.SetActive(true);

        if (canvas == null)
        {
            canvas = GetComponentInParent<Canvas>();
        }

        if (gameplayCamera == null)
        {
            gameplayCamera = Camera.main;
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

            ReceiverChoiceButton button =
                Instantiate(
                    buttonPrefab,
                    transform);

            button.Configure(
                receiverAssignment.receiver,
                receiverAssignment.assignment.route,
                controller,
                canvas,
                gameplayCamera);

            spawnedButtons.Add(
                button.gameObject);
        }
    }

    public void Hide()
    {
        ClearButtons();
        gameObject.SetActive(false);
    }

    private void ClearButtons()
    {
        foreach (GameObject spawnedButton
                 in spawnedButtons)
        {
            if (spawnedButton != null)
            {
                Destroy(spawnedButton);
            }
        }

        spawnedButtons.Clear();
    }
}