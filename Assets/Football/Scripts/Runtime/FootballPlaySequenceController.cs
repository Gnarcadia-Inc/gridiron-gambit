using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class FootballPlaySequenceController : MonoBehaviour
{
    [Header("Selected Play")]

    [SerializeField]
    private FootballPlay selectedPlay;

    [Header("Field")]

    [SerializeField]
    private Transform playOrigin;

    [SerializeField]
    private Transform spawnedPlayersParent;

    [Header("Prefabs")]

    [SerializeField]
    private GameObject quarterbackPrefab;

    [SerializeField]
    private List<FootballPlayerPrefabEntry>
        skillPlayerPrefabs = new();

    [SerializeField]
    private FootballBall footballPrefab;

    [Header("Timing")]

    [SerializeField]
    private float preSnapDelay = 3f;

    [SerializeField]
    private float timeBeforeReceiverChoice = 2f;

    [SerializeField]
    private float receiverChoiceRealSeconds = 3f;

    [SerializeField]
    [Range(0.05f, 1f)]
    private float receiverChoiceTimeScale = 0.25f;

    [Header("Quarterback")]

    [SerializeField]
    private float quarterbackDropbackYards = 5f;

    [SerializeField]
    private float fallbackReceiverSpeedYardsPerSecond = 6f;

    [SerializeField]
    private Transform quarterbackThrowPointOverride;

    [Header("Throw")]

    [SerializeField]
    private float ballFlightTime = 1.1f;

    [SerializeField]
    private float ballLobHeight = 4f;

    [SerializeField]
    private float runAfterCatchSpeedYardsPerSecond = 7f;

    [Header("UI")]

    [SerializeField]
    private ReceiverSelectionPanel receiverSelectionPanel;

    [Header("Camera")]

    [SerializeField]
    private SimpleTargetCamera targetCamera;

    [SerializeField]
    private Vector3 quarterbackCameraOffset =
        new Vector3(0f, 5f, -8f);

    [SerializeField]
    private Vector3 ballCameraOffset =
        new Vector3(0f, 2.5f, -5f);

    [SerializeField]
    private Vector3 receiverCameraOffset =
        new Vector3(0f, 4f, -7f);

    [Header("Events")]

    public UnityEvent onFormationReady;
    public UnityEvent onSnap;
    public UnityEvent onReceiverChoiceOpened;
    public UnityEvent onThrow;
    public UnityEvent onCatch;

    private readonly List<GameObject>
        spawnedPlayers = new();

    private readonly List<RuntimeReceiverAssignment>
        runtimeReceivers = new();

    private GameObject quarterbackObject;
    private FootballRouteRunner quarterbackRunner;
    private Transform quarterbackThrowPoint;

    private FootballBall activeBall;

    private Coroutine playSequenceCoroutine;
    private Coroutine receiverChoiceCoroutine;

    private bool receiverChoiceIsOpen;
    private bool ballHasBeenThrown;

    public FootballPlay SelectedPlay =>
        selectedPlay;

    public void SelectAndStartPlay(
        FootballPlay play)
    {
        selectedPlay = play;
        StartSelectedPlay();
    }

    public void SetSelectedPlay(
        FootballPlay play)
    {
        selectedPlay = play;
    }

    public void StartSelectedPlay()
    {
        if (selectedPlay == null)
        {
            Debug.LogWarning(
                "No football play is selected.");

            return;
        }

        ResetCurrentPlay();

        playSequenceCoroutine =
            StartCoroutine(
                RunPlaySequence(selectedPlay));
    }

    public void SelectThrowTarget(
        FootballReceiverTarget receiver)
    {
        if (!receiverChoiceIsOpen ||
            ballHasBeenThrown ||
            receiver == null)
        {
            return;
        }

        receiverChoiceIsOpen = false;

        if (receiverChoiceCoroutine != null)
        {
            StopCoroutine(receiverChoiceCoroutine);
            receiverChoiceCoroutine = null;
        }

        receiverSelectionPanel.Hide();

        RestoreNormalTime();

        ThrowToReceiver(receiver);
    }

    public void CompleteCatch(
        FootballReceiverTarget receiver,
        FootballBall ball)
    {
        if (receiver == null ||
            ball == null ||
            ball.HasBeenCaught)
        {
            return;
        }

        receiver.ReceiveBall(
            ball,
            playOrigin,
            runAfterCatchSpeedYardsPerSecond);

        if (targetCamera != null)
        {
            targetCamera.SetTarget(
                receiver.transform,
                receiverCameraOffset);
        }

        onCatch?.Invoke();
    }

    public void ResetCurrentPlay()
    {
        if (playSequenceCoroutine != null)
        {
            StopCoroutine(playSequenceCoroutine);
            playSequenceCoroutine = null;
        }

        if (receiverChoiceCoroutine != null)
        {
            StopCoroutine(receiverChoiceCoroutine);
            receiverChoiceCoroutine = null;
        }

        receiverChoiceIsOpen = false;
        ballHasBeenThrown = false;

        RestoreNormalTime();

        if (receiverSelectionPanel != null)
        {
            receiverSelectionPanel.Hide();
        }

        foreach (GameObject spawnedPlayer
                 in spawnedPlayers)
        {
            if (spawnedPlayer != null)
            {
                Destroy(spawnedPlayer);
            }
        }

        spawnedPlayers.Clear();
        runtimeReceivers.Clear();

        if (activeBall != null)
        {
            Destroy(activeBall.gameObject);
            activeBall = null;
        }

        quarterbackObject = null;
        quarterbackRunner = null;
        quarterbackThrowPoint = null;
    }

    private IEnumerator RunPlaySequence(
        FootballPlay play)
    {
        SpawnFormation(play);

        onFormationReady?.Invoke();

        yield return new WaitForSeconds(
            preSnapDelay);

        ExecuteSnap();

        yield return new WaitForSeconds(
            timeBeforeReceiverChoice);

        receiverChoiceCoroutine =
            StartCoroutine(
                OpenReceiverChoiceWindow());

        playSequenceCoroutine = null;
    }

    private void SpawnFormation(
        FootballPlay play)
    {
        SpawnQuarterback();

        foreach (RouteAssignment assignment
                 in play.assignments)
        {
            if (assignment == null ||
                assignment.route == null)
            {
                continue;
            }

            GameObject prefab =
                FindSkillPrefab(
                    assignment.role);

            if (prefab == null)
            {
                Debug.LogWarning(
                    $"No prefab assigned for " +
                    $"{assignment.role}.");

                continue;
            }

            GameObject player =
                Instantiate(prefab);

            if (spawnedPlayersParent != null)
            {
                player.transform.SetParent(
                    spawnedPlayersParent,
                    true);
            }

            player.name =
                $"{assignment.role} - " +
                $"{assignment.route.routeName}";

            FootballRouteRunner runner =
                player.GetComponent<
                    FootballRouteRunner>();

            FootballReceiverTarget receiver =
                player.GetComponent<
                    FootballReceiverTarget>();

            if (runner == null ||
                receiver == null)
            {
                Debug.LogError(
                    $"{player.name} needs both " +
                    "FootballRouteRunner and " +
                    "FootballReceiverTarget.");

                Destroy(player);
                continue;
            }

            runner.PrepareForPlay(
                playOrigin,
                assignment.startingOffsetYards);

            spawnedPlayers.Add(player);

            runtimeReceivers.Add(
                new RuntimeReceiverAssignment
                {
                    assignment = assignment,
                    runner = runner,
                    receiver = receiver
                });
        }

        if (targetCamera != null &&
            quarterbackObject != null)
        {
            targetCamera.SetTarget(
                quarterbackObject.transform,
                quarterbackCameraOffset);
        }
    }

    private void SpawnQuarterback()
    {
        if (quarterbackPrefab == null)
        {
            Debug.LogError(
                "No quarterback prefab assigned.");

            return;
        }

        quarterbackObject =
            Instantiate(
                quarterbackPrefab,
                playOrigin.position,
                playOrigin.rotation);

        if (spawnedPlayersParent != null)
        {
            quarterbackObject.transform.SetParent(
                spawnedPlayersParent,
                true);
        }

        spawnedPlayers.Add(
            quarterbackObject);

        quarterbackRunner =
            quarterbackObject.GetComponent<
                FootballRouteRunner>();

        if (quarterbackRunner == null)
        {
            Debug.LogError(
                "Quarterback prefab needs a " +
                "FootballRouteRunner.");
        }

        QuarterbackThrowPoint marker =
            quarterbackObject.GetComponentInChildren<
                QuarterbackThrowPoint>();

        if (quarterbackThrowPointOverride != null)
        {
            quarterbackThrowPoint =
                quarterbackThrowPointOverride;
        }
        else if (marker != null)
        {
            quarterbackThrowPoint =
                marker.transform;
        }
        else
        {
            quarterbackThrowPoint =
                quarterbackObject.transform;
        }
    }

    private void ExecuteSnap()
    {
        onSnap?.Invoke();

        float sharedReceiverSpeed =
            FindRepresentativeReceiverSpeed();

        foreach (RuntimeReceiverAssignment receiver
                 in runtimeReceivers)
        {
            receiver.runner.StartPreparedRoute(
                receiver.assignment.route,
                playOrigin,
                receiver.assignment.releaseDelay);
        }

        if (quarterbackRunner != null)
        {
            quarterbackRunner.StartQuarterbackDropback(
                playOrigin,
                quarterbackDropbackYards,
                sharedReceiverSpeed);
        }
    }

    private IEnumerator OpenReceiverChoiceWindow()
    {
        if (runtimeReceivers.Count == 0)
        {
            yield break;
        }

        receiverChoiceIsOpen = true;

        Time.timeScale =
            receiverChoiceTimeScale;

        if (receiverSelectionPanel != null)
        {
            receiverSelectionPanel.Show(
                runtimeReceivers,
                this);
        }

        onReceiverChoiceOpened?.Invoke();

        yield return new WaitForSecondsRealtime(
            receiverChoiceRealSeconds);

        if (!receiverChoiceIsOpen)
        {
            yield break;
        }

        receiverChoiceIsOpen = false;

        if (receiverSelectionPanel != null)
        {
            receiverSelectionPanel.Hide();
        }

        RestoreNormalTime();

        receiverChoiceCoroutine = null;
    }

    private void ThrowToReceiver(
        FootballReceiverTarget receiver)
    {
        if (footballPrefab == null)
        {
            Debug.LogError(
                "No football prefab assigned.");

            return;
        }

        ballHasBeenThrown = true;

        if (quarterbackRunner != null)
        {
            quarterbackRunner.StopMovement();
        }

        Vector3 throwPosition =
            quarterbackThrowPoint != null
                ? quarterbackThrowPoint.position
                : quarterbackObject.transform.position +
                  Vector3.up * 1.5f;

        Vector3 predictedCatchPosition =
            receiver.PredictCatchPosition(
                ballFlightTime);

        activeBall =
            Instantiate(
                footballPrefab,
                throwPosition,
                Quaternion.identity);

        activeBall.ThrowLob(
            throwPosition,
            predictedCatchPosition,
            ballFlightTime,
            ballLobHeight,
            receiver,
            this);

        if (targetCamera != null)
        {
            targetCamera.SetTarget(
                activeBall.transform,
                ballCameraOffset);
        }

        onThrow?.Invoke();
    }

    private float FindRepresentativeReceiverSpeed()
    {
        foreach (RuntimeReceiverAssignment receiver
                 in runtimeReceivers)
        {
            FootballRoute route =
                receiver.assignment.route;

            if (route != null &&
                route.steps.Count > 0)
            {
                return route.steps[0]
                    .speedYardsPerSecond;
            }
        }

        return fallbackReceiverSpeedYardsPerSecond;
    }

    private GameObject FindSkillPrefab(
        OffensiveRole role)
    {
        foreach (FootballPlayerPrefabEntry entry
                 in skillPlayerPrefabs)
        {
            if (entry != null &&
                entry.role == role)
            {
                return entry.playerPrefab;
            }
        }

        return null;
    }

    private static void RestoreNormalTime()
    {
        Time.timeScale = 1f;
    }

    private void OnDisable()
    {
        RestoreNormalTime();
    }
}