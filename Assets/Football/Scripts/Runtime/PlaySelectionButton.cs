using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlaySelectionButton : MonoBehaviour
{
    [SerializeField]
    private Button button;

    [SerializeField]
    private Image playImage;

    [SerializeField]
    private TMP_Text playNameText;

    private FootballPlayOption option;
    private FootballGameSetupController setupController;

    private void Reset()
    {
        button = GetComponent<Button>();
        playImage = GetComponent<Image>();
        playNameText =
            GetComponentInChildren<TMP_Text>();
    }

    public void Configure(
        FootballPlayOption newOption,
        FootballGameSetupController controller)
    {
        option = newOption;
        setupController = controller;

        if (playNameText != null)
        {
            playNameText.text =
                option != null &&
                option.play != null
                    ? option.play.playName
                    : "Unavailable";
        }

        if (playImage != null &&
            option != null &&
            option.buttonSprite != null)
        {
            playImage.sprite =
                option.buttonSprite;
        }

        button.interactable =
            option != null &&
            option.play != null;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(
            SelectPlay);
    }

    private void SelectPlay()
    {
        if (option == null ||
            option.play == null ||
            setupController == null)
        {
            return;
        }

        setupController.SelectPlay(
            option.play);
    }
}