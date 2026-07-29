using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FootballGameSetupController : MonoBehaviour
{
    [Header("Teams")]

    [SerializeField]
    private FootballTeamDefinition[] teams;

    [SerializeField]
    private int selectedTeamIndex;

    [Header("Main Menu")]

    [SerializeField]
    private GameObject mainMenu;

    [SerializeField]
    private Button previousTeamButton;

    [SerializeField]
    private Button nextTeamButton;

    [SerializeField]
    private Button startGameButton;

    [SerializeField]
    private Image selectedTeamLogo;

    [Header("Situation Reveal")]

    [SerializeField]
    private FootballSituationPanel sitchPanel;

    [SerializeField]
    private float situationHoldAfterReveal = 0.35f;

    [Header("Play Selection")]

    [SerializeField]
    private GameObject playSelectionPanel;

    [SerializeField]
    private SlidingUIObject playSelectionSlider;

    [SerializeField]
    private PlaySelectionButton[] playButtons;

    [SerializeField]
    private FootballPlayBank playBank;

    [Header("Game Runtime")]

    [SerializeField]
    private FootballPlaySequenceController sequenceController;

    [SerializeField]
    private FootballFieldVisual fieldVisual;

    [Header("Field Placement")]

    [Tooltip(
        "Position representing the offense's own goal line.")]
    [SerializeField]
    private Transform ownGoalReference;

    [Tooltip(
        "Direction from the offense's own goal toward " +
        "the opponent's goal.")]
    [SerializeField]
    private Transform offensiveDirectionReference;

    private FootballGameSituation currentSituation;

    private bool setupInProgress;
    private bool playHasBeenSelected;

    public FootballTeamDefinition SelectedTeam =>
        teams != null &&
        teams.Length > 0
            ? teams[selectedTeamIndex]
            : null;

    public FootballGameSituation CurrentSituation =>
        currentSituation;

    [SerializeField]
    private PregameMenuAnimation pregameMenuAnimation;

    private Coroutine setupCoroutine;
    private Coroutine confirmPlayCoroutine;
    private bool restartInProgress;
    public bool RestartInProgress =>
       restartInProgress;

    [SerializeField]
    private GameObject endScreen;

    private Coroutine returnToMenuCoroutine;
    private bool returnToMenuInProgress;

    [SerializeField]
    private BetManager betManager;

    private void Awake()
    {
        Time.timeScale = 1f;

        mainMenu.SetActive(true);

        if (sitchPanel != null)
        {
            sitchPanel.gameObject.SetActive(false);
        }

        if (playSelectionPanel != null)
        {
            playSelectionPanel.SetActive(true);
        }


        startGameButton.onClick.AddListener(
            StartGameSetup);

        UpdateSelectedTeamDisplay();
    }

    public void SelectPreviousTeam()
    {
        if (setupInProgress || restartInProgress || returnToMenuInProgress || teams == null || teams.Length == 0)
        {
            return;
        }

        selectedTeamIndex--;

        if (selectedTeamIndex < 0)
        {
            selectedTeamIndex = teams.Length - 1;
        }

        UpdateSelectedTeamDisplay();
    }

    public void SelectNextTeam()
    {
        if (setupInProgress || restartInProgress || returnToMenuInProgress || teams == null || teams.Length == 0)
        {
            return;
        }

        selectedTeamIndex++;

        if (selectedTeamIndex > teams.Length - 1)
        {
            selectedTeamIndex = 0;
        }

        UpdateSelectedTeamDisplay();
    }

    public void StartGameSetup()
    {
        if (setupInProgress || restartInProgress || returnToMenuInProgress || SelectedTeam == null)
        {
            return;
        }

        betManager.PlaceBet();

        setupCoroutine = StartCoroutine(SetupRoutine());
    }

    public void SelectPlay(
    FootballPlay selectedPlay)
    {
        if (!setupInProgress ||
            restartInProgress ||
            returnToMenuInProgress ||
            playHasBeenSelected ||
            selectedPlay == null)
        {
            return;
        }

        playHasBeenSelected = true;

        confirmPlayCoroutine =
            StartCoroutine(
                ConfirmPlayRoutine(
                    selectedPlay));
    }

    private IEnumerator SetupRoutine()
    {
        setupInProgress = true;
        playHasBeenSelected = false;

        SetMainMenuInteractable(false);

        currentSituation =
            FootballSituationGenerator.Generate(
                SelectedTeam,
                teams);

        ApplyGameSituation();

        bool revealFinished = false;

        if (restartInProgress)
        {
            sitchPanel.Reveal(
                    currentSituation,
                    teams,
                    () => revealFinished = true);
        }
        else
        {
            pregameMenuAnimation.PlayExitAnimation(
            () =>
            {
                sitchPanel.Reveal(
                    currentSituation,
                    teams,
                    () => revealFinished = true);
            });
        }

        while (!revealFinished)
        {
            yield return null;
        }

        mainMenu.SetActive(false);

        yield return new WaitForSecondsRealtime(
            situationHoldAfterReveal);

        ConfigurePlayOptions();

        sitchPanel.Hide();

        if (playSelectionSlider != null)
        {
            yield return
                playSelectionSlider.ShowAndWait();
        }

        setupCoroutine = null;
    }

    private IEnumerator ConfirmPlayRoutine(
    FootballPlay selectedPlay)
    {
        DisablePlayButtons();

        if (playSelectionSlider != null)
        {
            yield return
                playSelectionSlider.HideAndWait();
        }

        sequenceController.SetSelectedPlay(
            selectedPlay);

        sequenceController.StartSelectedPlay();

        setupInProgress = false;
        confirmPlayCoroutine = null;
    }

    private void ApplyGameSituation()
    {
        MovePlayOriginToYardLine();

        sequenceController.ConfigureTeams(
            currentSituation.playerTeam,
            currentSituation.opponentTeam);
    }

    private void MovePlayOriginToYardLine()
    {
        if (ownGoalReference == null ||
            offensiveDirectionReference == null)
        {
            Debug.LogWarning(
                "Field placement references are missing.");

            return;
        }

        Vector3 fieldDirection =
            offensiveDirectionReference.forward;

        fieldDirection.y = 0f;
        fieldDirection.Normalize();

        Vector3 playPosition =
            ownGoalReference.position +
            fieldDirection *
            FootballUnits.YardsToUnits(
                currentSituation.yardsFromOwnGoal);

        sequenceController.SetPlayOriginPosition(
            playPosition,
            Quaternion.LookRotation(
                fieldDirection,
                Vector3.up));
    }

    private void ConfigurePlayOptions()
    {
        List<FootballPlayOption> options =
            playBank.ChooseOptions(
                currentSituation,
                playButtons.Length);

        for (int i = 0;
             i < playButtons.Length;
             i++)
        {
            FootballPlayOption option =
                i < options.Count
                    ? options[i]
                    : null;

            playButtons[i].Configure(
                option,
                this);
        }
    }

    private void UpdateSelectedTeamDisplay()
    {
        FootballTeamDefinition team =
            SelectedTeam;

        if (team == null)
        {

            startGameButton.interactable =
                false;

            return;
        }

        if (selectedTeamLogo != null)
        {
            selectedTeamLogo.sprite =
                team.menuLogo;

            selectedTeamLogo.enabled =
                team.menuLogo != null;
        }

        startGameButton.interactable =
            true;


        fieldVisual.ApplyTeamField(team);
    }

    private void SetMainMenuInteractable(
        bool interactable)
    {
        previousTeamButton.interactable =
            interactable;

        nextTeamButton.interactable =
            interactable;

        startGameButton.interactable =
            interactable;
    }

    private void DisablePlayButtons()
    {
        foreach (PlaySelectionButton playButton
                 in playButtons)
        {
            Button button =
                playButton.GetComponent<Button>();

            if (button != null)
            {
                button.interactable = false;
            }
        }
    }

    public void ReturnToMainMenu()
    {
        if (returnToMenuInProgress || restartInProgress || setupInProgress)
        {
            return;
        }

        returnToMenuCoroutine = StartCoroutine(ReturnToMainMenuRoutine());
    }

    private IEnumerator ReturnToMainMenuRoutine()
    {
        returnToMenuInProgress = true;

        Time.timeScale = 1f;

        if (setupCoroutine != null)
        {
            StopCoroutine(setupCoroutine);
            setupCoroutine = null;
        }

        if (confirmPlayCoroutine != null)
        {
            StopCoroutine(confirmPlayCoroutine);
            confirmPlayCoroutine = null;
        }

        setupInProgress = false;
        playHasBeenSelected = false;
        restartInProgress = false;

        if (sequenceController != null)
        {
            sequenceController.ResetCurrentPlay();
            sequenceController.SetSelectedPlay(null);
        }

        if (playSelectionSlider != null)
        {
            playSelectionSlider.ResetImmediately();
        }
        else if (playSelectionPanel != null)
        {
            playSelectionPanel.SetActive(false);
        }

        ClearPlayOptions();

        endScreen.SetActive(false);

        if (sitchPanel != null)
        {
            sitchPanel.ResetForNewSituation();
        }

        currentSituation = null;

        yield return null;

        if (playSelectionPanel != null)
        {
            playSelectionPanel.SetActive(false);
        }

        if (sitchPanel != null)
        {
            sitchPanel.gameObject.SetActive(false);
        }

        if (pregameMenuAnimation != null)
        {
            pregameMenuAnimation.ResetImmediately();
        }

        if (mainMenu != null)
        {
            mainMenu.SetActive(true);
        }

        UpdateSelectedTeamDisplay();
        SetMainMenuInteractable(true);

        returnToMenuCoroutine = null;
        returnToMenuInProgress = false;
    }

    public void RestartGameSequence()
    {
        if (restartInProgress)
        {
            return;
        }

        StartCoroutine(
            RestartGameSequenceRoutine());
    }

    private IEnumerator RestartGameSequenceRoutine()
    {
        restartInProgress = true;

        Time.timeScale = 1f;

        if (setupCoroutine != null)
        {
            StopCoroutine(setupCoroutine);
            setupCoroutine = null;
        }

        if (confirmPlayCoroutine != null)
        {
            StopCoroutine(confirmPlayCoroutine);
            confirmPlayCoroutine = null;
        }

        setupInProgress = false;
        playHasBeenSelected = false;
        restartInProgress = false;

        if (sequenceController != null)
        {
            sequenceController.ResetCurrentPlay();
            sequenceController.SetSelectedPlay(null);
        }

        if (playSelectionSlider != null)
        {
            playSelectionSlider.ResetImmediately();
        }
        else if (playSelectionPanel != null)
        {
            playSelectionPanel.SetActive(false);
        }

        ClearPlayOptions();

        endScreen.SetActive(false);

        if (sitchPanel != null)
        {
            sitchPanel.ResetForNewSituation();
        }

        currentSituation = null;

        if (setupCoroutine != null)
        {
            StopCoroutine(setupCoroutine);
            setupCoroutine = null;
        }

        if (confirmPlayCoroutine != null)
        {
            StopCoroutine(confirmPlayCoroutine);
            confirmPlayCoroutine = null;
        }

        setupInProgress = false;
        playHasBeenSelected = false;

        if (sequenceController != null)
        {
            sequenceController.ResetCurrentPlay();
            sequenceController.SetSelectedPlay(null);
        }

        if (playSelectionSlider != null)
        {
            playSelectionSlider.ResetImmediately();
        }
        else if (playSelectionPanel != null)
        {
            playSelectionPanel.SetActive(false);
        }

        ClearPlayOptions();



        yield return null;

        currentSituation = null;

        restartInProgress = false;

        StartGameSetup();
    }

    private void ClearPlayOptions()
    {
        if (playButtons == null)
        {
            return;
        }

        foreach (PlaySelectionButton playButton
                 in playButtons)
        {
            if (playButton == null)
            {
                continue;
            }

            playButton.Clear();

            Button button =
                playButton.GetComponent<Button>();

            if (button != null)
            {
                button.interactable = false;
            }
        }
    }
}