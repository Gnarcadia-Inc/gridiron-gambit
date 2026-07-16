using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlaySelectionButton : MonoBehaviour
{
    [SerializeField]
    private FootballPlay play;

    [SerializeField]
    private FootballPlaySequenceController controller;

    [SerializeField]
    private Button button;

    [SerializeField]
    private TMP_Text label;

    private void Awake()
    {
        if (label != null &&
            play != null)
        {
            label.text = play.playName;
        }

        if (button != null)
        {
            button.onClick.AddListener(
                SelectPlay);
        }
    }

    private void SelectPlay()
    {
        if (controller != null &&
            play != null)
        {
            controller.SelectAndStartPlay(play);
        }
    }
}