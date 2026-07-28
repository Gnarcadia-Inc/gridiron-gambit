using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public enum FootballPlayResult
{
    Completion,
    Incompletion,
    Interception,
    Run,
    Tackle,
    Sack,
    Touchdown
}

[System.Serializable]
public class FootballPlayOutcome
{
    public FootballPlayResult result;

    public float yards;

    public OffensiveRole ballCarrierRole;

    public bool wasPass;
    public bool wasRun;
    public bool wasScramble;
}

[System.Serializable]
public class FootballPlayOutcomeEvent : UnityEngine.Events.UnityEvent<FootballPlayOutcome> { }

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

    [Header("Quarterback Scramble")]

    [SerializeField]
    private List<FootballRoute> scrambleRoutes = new();

    [SerializeField]
    private float scrambleRouteDelaySeconds = 0f;

    public UnityEvent onScramble;

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

    private FootballReceiverTarget pendingThrowTarget;

    [Header("Defense")]

    [SerializeField]
    private GameObject defenderPrefab;

    [SerializeField]
    private float defaultDefenderSpeedYardsPerSecond;

    [SerializeField]
    private float cornerDepthYards = 1f;

    [SerializeField]
    private float safetyDepthYards = 8f;

    [SerializeField]
    private float safetyMiddleSpacingYards = 3f;

    [SerializeField]
    private float cornerLateralAdjustmentYards;

    [SerializeField]
    private float safetyLateralBlendTowardMiddle = 0.65f;

    private readonly List<FootballDefenderController>
    runtimeDefenders = new();

    private bool playIsDead;

    public bool PlayIsDead =>
        playIsDead;

    public UnityEvent onInterception;
    public UnityEvent onTackle;
    public UnityEvent onPlayEnded;

    private string offenseLayerName = "OffensePlayer";
    private string defenseLayerName = "DefensePlayer";

    private FootballTeamDefinition offenseTeam;
    private FootballTeamDefinition defenseTeam;

    [SerializeField]
    [Min(0f)]
    private float catchAnimationLeadSeconds = 0.32f;

    [SerializeField]
    [Min(0f)]
    private float deflectionAnimationLeadSeconds = 0.28f;

    [SerializeField]
    [Min(0f)]
    private float deflectionAttemptRadiusYards = 2f;

    private Coroutine ballArrivalAnimationCoroutine;

    private RuntimeReceiverAssignment runBallCarrier;
    private Coroutine postSnapDecisionCoroutine;
    private bool handoffCompleted;

    public UnityEvent onHandoff;

    private readonly List<FootballOffensivePlayerController> runtimeOffensiveLinemen = new();

    [SerializeField]
    private GameObject offensiveLinemanPrefab;

    [SerializeField]
    private bool randomlyChooseDefensiveFront = true;

    [SerializeField]
    private DefensiveFrontType defaultFront = DefensiveFrontType.FourLinemenTwoLinebackers;

    private FootballPlayResult expectedPlayResult;

    [SerializeField]
    private GameObject defensiveLinemanPrefab;

    [SerializeField]
    private GameObject linebackerPrefab;

    [SerializeField]
    private float defensiveLineSpacingYards = 2f;

    [SerializeField]
    private float defensiveLineDepthYards = 0.75f;

    [SerializeField]
    private float linebackerDepthYards = 4f;

    private readonly List<FootballFrontDefenderController> runtimeFrontDefenders = new();

    public UnityEngine.Events.UnityEvent onSack;

    [Header("Final Play Outcome")]

    [SerializeField]
    private FootballPlayOutcomeEvent onFinalOutcome;

    private FootballPlayOutcome finalOutcome;

    public FootballPlayOutcome FinalOutcome =>
        finalOutcome;

    private bool passWasCompleted;
    private bool runWasStarted;
    private bool quarterbackIsScrambling;

    private OffensiveRole activeBallCarrierRole = OffensiveRole.Quarterback;

    [Header("Touchdown")]
    [SerializeField]
    private Transform opponentGoalLineReference;
    public UnityEvent onTouchdown;

    [SerializeField]
    private FootballTouchdownZone opponentTouchdownZone;

    private DefensiveFrontType ResolveDefensiveFront()
    {
        if (!randomlyChooseDefensiveFront)
        {
            return defaultFront;
        }

        return Random.value < 0.5f ? DefensiveFrontType.ThreeLinemenThreeLinebackers
            : DefensiveFrontType.FourLinemenTwoLinebackers;
    }

    public void SetExpectedPlayResult(FootballPlayResult result)
    {
        expectedPlayResult = result;
    }

    public void SelectAndStartPlay(FootballPlay play)
    {
        selectedPlay = play;
        StartSelectedPlay();
    }

    public void SetSelectedPlay(FootballPlay play)
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

        if (!selectedPlay.HasValidOffensivePersonnel())
        {
            Debug.LogError(
                $"Play '{selectedPlay.name}' does not have " +
                $"1 QB, 5 skill players, and 5 linemen.");

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
        if (playIsDead)
        {
            return;
        }

        UpdatePendingThrowTarget(receiver);
    }

    public void CompleteCatch(FootballReceiverTarget receiver, FootballBall ball)
    {
        if (playIsDead ||
            receiver == null ||
            ball == null)
        {
            return;
        }

        passWasCompleted = true;
        activeBallCarrierRole = receiver.Role;

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

    public void ConfigureTeams(
    FootballTeamDefinition newOffenseTeam,
    FootballTeamDefinition newDefenseTeam)
    {
        offenseTeam = newOffenseTeam;
        defenseTeam = newDefenseTeam;
    }

    public void SetPlayOriginPosition(
        Vector3 worldPosition,
        Quaternion worldRotation)
    {
        if (playOrigin == null)
        {
            Debug.LogError(
                "FootballPlaySequenceController has no " +
                "Play Origin assigned.");

            return;
        }

        playOrigin.SetPositionAndRotation(
            worldPosition,
            worldRotation);
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

        if (ballArrivalAnimationCoroutine != null)
        {
            StopCoroutine(
                ballArrivalAnimationCoroutine);

            ballArrivalAnimationCoroutine = null;
        }

        if (postSnapDecisionCoroutine != null)
        {
            StopCoroutine(
                postSnapDecisionCoroutine);

            postSnapDecisionCoroutine = null;
        }

        passWasCompleted = false;
        runWasStarted = false;
        quarterbackIsScrambling = false;
        handoffCompleted = false;
        runBallCarrier = null;
        receiverChoiceIsOpen = false;
        ballHasBeenThrown = false;
        pendingThrowTarget = null;
        playIsDead = false;
        runtimeDefenders.Clear();

        activeBallCarrierRole = OffensiveRole.Quarterback;

        RestoreNormalTime();

        receiverSelectionPanel.Hide();

        foreach (GameObject spawnedPlayer
                 in spawnedPlayers)
        {
            if (spawnedPlayer != null)
            {
                Destroy(spawnedPlayer);
            }
        }

        if (quarterbackRunner != null)
        {
            quarterbackRunner.SetHasBall(false);
        }

        foreach (RuntimeReceiverAssignment receiver
                 in runtimeReceivers)
        {
            if (receiver?.runner != null)
            {
                receiver.runner.SetHasBall(false);
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

        playSequenceCoroutine = null;
    }

    private void SpawnFormation(FootballPlay play)
    {
        SpawnQuarterback();

        SpawnReceivers(play);

        ResolveRunBallCarrier();

        SpawnOffensiveLine(play);

        SpawnDefensiveBacks();

        SpawnDefensiveFront();

        if (targetCamera != null && quarterbackObject != null)
        {
            targetCamera.SetTarget(quarterbackObject.transform, quarterbackCameraOffset);
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

        Vector3 quarterbackPosition = ConvertOffsetToWorldPosition(selectedPlay.quarterbackStartingOffsetYards);

        quarterbackObject = Instantiate(quarterbackPrefab, quarterbackPosition, playOrigin.rotation);

        FootballUniformVisual quarterbackUniform = quarterbackObject.GetComponentInChildren<FootballUniformVisual>(true);

        if (quarterbackUniform != null &&
            offenseTeam != null)
        {
            quarterbackUniform.ApplyMaterial(
                offenseTeam.offensiveJerseyMaterial);
        }

        if (spawnedPlayersParent != null)
        {
            quarterbackObject.transform.SetParent(spawnedPlayersParent, true);
        }

        spawnedPlayers.Add(quarterbackObject);

        quarterbackRunner = quarterbackObject.GetComponent<FootballRouteRunner>();

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

        quarterbackRunner.SetHasBall(true);

        FootballPlayerAnimator quarterbackAnimator = quarterbackObject.GetComponent<FootballPlayerAnimator>();

        if (quarterbackAnimator == null)
        {
            quarterbackAnimator = quarterbackObject.GetComponentInChildren<FootballPlayerAnimator>();
        }

        quarterbackAnimator?.GetSet();
    }

    private void SpawnOffensiveLine(FootballPlay play)
    {
        runtimeOffensiveLinemen.Clear();

        if (selectedPlay == null || selectedPlay.offensiveLine == null)
        {
            return;
        }

        foreach (OffensiveLinePlayEntry entry in selectedPlay.offensiveLine)
        {
            if (entry == null)
            {
                continue;
            }

            Vector3 spawnPosition =
                ConvertOffsetToWorldPosition(entry.startingOffsetYards);

            GameObject playerObject =
                Instantiate(
                    offensiveLinemanPrefab,
                    spawnPosition,
                    playOrigin.rotation);

            FootballUniformVisual uniform = playerObject.GetComponentInChildren<FootballUniformVisual>(true);

            if (uniform != null &&
                offenseTeam != null)
            {
                uniform.ApplyMaterial(
                    offenseTeam.offensiveJerseyMaterial);
            }

            FootballOffensivePlayerController
                offensiveController =
                    playerObject.GetComponent<
                        FootballOffensivePlayerController>();

            FootballRouteRunner routeRunner =
                playerObject.GetComponent<
                    FootballRouteRunner>();

            if (offensiveController == null)
            {
                Debug.LogError(
                    "Offensive lineman prefab is missing " +
                    "FootballOffensivePlayerController.",
                    playerObject);

                Destroy(playerObject);
                continue;
            }

            offensiveController.Initialize(
                entry.role,
                entry.endBehavior);

            if (routeRunner != null &&
                entry.route != null)
            {
                routeRunner.PrepareForPlay(playOrigin, entry.startingOffsetYards);
            }

            int offenseLayer = LayerMask.NameToLayer(offenseLayerName);
            SetPlayerRootLayer(playerObject, offenseLayer);

            runtimeOffensiveLinemen.Add(
                offensiveController);
        }
    }



    private void SpawnReceivers(FootballPlay play)
    {
        foreach (RouteAssignment assignment in play.assignments)
        {
            if (assignment == null)
            {
                continue;
            }

            GameObject prefab = FindSkillPrefab(assignment.role);

            if (prefab == null)
            {
                Debug.LogWarning(
                    $"No prefab assigned for " +
                    $"{assignment.role}.");

                continue;
            }

            GameObject player = Instantiate(prefab);

            if (spawnedPlayersParent != null)
            {
                player.transform.SetParent(spawnedPlayersParent, true);
            }

            FootballUniformVisual uniformVisual = player.GetComponentInChildren<FootballUniformVisual>(true);

            if (uniformVisual != null &&
                offenseTeam != null)
            {
                uniformVisual.ApplyMaterial(offenseTeam.offensiveJerseyMaterial);
            }

            FootballRouteRunner runner = player.GetComponent<FootballRouteRunner>();

            FootballReceiverTarget receiver = player.GetComponent<FootballReceiverTarget>();

            if (runner == null || receiver == null)
            {
                Debug.LogError(
                    $"{player.name} needs both " +
                    "FootballRouteRunner and " +
                    "FootballReceiverTarget.");

                Destroy(player);
                continue;
            }

            receiver.InitializeRole(assignment.role);

            runner.SetHasBall(false);

            receiver.PlayerAnimator?.GetSet();

            if (assignment.route != null)
            {
                player.name =
                $"{assignment.role} - " +
                $"{assignment.route.routeName}";

                runner.PrepareForPlay(playOrigin, assignment.startingOffsetYards);
            }
            else
            {
                player.name =
                $"{assignment.role} - ";
            }

            int offenseLayer = LayerMask.NameToLayer(offenseLayerName);
            SetPlayerRootLayer(player, offenseLayer);

            spawnedPlayers.Add(player);

            runtimeReceivers.Add(
                new RuntimeReceiverAssignment
                {
                    assignment = assignment,
                    runner = runner,
                    receiver = receiver
                });
        }
    }

    private void SpawnDefensiveBacks()
    {
        runtimeDefenders.Clear();

        if (defenderPrefab == null)
        {
            Debug.LogWarning(
                "No defender prefab is assigned.");

            return;
        }

        int receiverCount =
            runtimeReceivers.Count;

        if (receiverCount == 0)
        {
            return;
        }

        /*
         * Examples:
         *
         * 3 receivers -> 2 corners, 1 safety
         * 4 receivers -> 3 corners, 1 safety
         * 5 receivers -> 3 corners, 2 safeties
         * 6 receivers -> 4 corners, 2 safeties
         */
        int safetyCount =
            receiverCount >= 5 ? 2 : 1;

        safetyCount =
            Mathf.Clamp(
                safetyCount,
                1,
                2);

        int cornerCount =
            receiverCount - safetyCount;

        cornerCount =
            Mathf.Clamp(
                cornerCount,
                2,
                4);

        /*
         * Ensure the total still equals the available
         * receiver count.
         */
        safetyCount =
            Mathf.Clamp(
                receiverCount - cornerCount,
                0,
                2);

        var orderedReceivers =
            new List<RuntimeReceiverAssignment>(
                runtimeReceivers);

        orderedReceivers.Sort(
            (first, second) =>
            {
                float firstX =
                    playOrigin.InverseTransformPoint(
                        first.receiver.transform.position).x;

                float secondX =
                    playOrigin.InverseTransformPoint(
                        second.receiver.transform.position).x;

                return firstX.CompareTo(secondX);
            });

        /*
         * Corners cover the widest receivers first.
         */
        var cornerTargets =
            SelectOutsideReceivers(
                orderedReceivers,
                cornerCount);

        var remainingTargets =
            new List<RuntimeReceiverAssignment>(
                orderedReceivers);

        foreach (RuntimeReceiverAssignment cornerTarget
                 in cornerTargets)
        {
            remainingTargets.Remove(cornerTarget);

            SpawnCornerback(cornerTarget);
        }

        /*
         * Safeties cover remaining receivers. If no receivers
         * remain, assign them to inside receivers.
         */
        for (int i = 0; i < safetyCount; i++)
        {
            RuntimeReceiverAssignment safetyTarget;

            if (remainingTargets.Count > 0)
            {
                safetyTarget =
                    remainingTargets[
                        Mathf.Min(
                            i,
                            remainingTargets.Count - 1)];
            }
            else
            {
                safetyTarget =
                    orderedReceivers[
                        Mathf.Clamp(
                            orderedReceivers.Count / 2 + i,
                            0,
                            orderedReceivers.Count - 1)];
            }

            SpawnSafety(
                safetyTarget,
                i,
                safetyCount);
        }
    }

    private static List<RuntimeReceiverAssignment>
    SelectOutsideReceivers(
        List<RuntimeReceiverAssignment> ordered,
        int count)
    {
        var result =
            new List<RuntimeReceiverAssignment>();

        int leftIndex = 0;
        int rightIndex = ordered.Count - 1;

        bool takeLeft = true;

        while (result.Count < count &&
               leftIndex <= rightIndex)
        {
            if (takeLeft)
            {
                result.Add(
                    ordered[leftIndex]);

                leftIndex++;
            }
            else
            {
                result.Add(
                    ordered[rightIndex]);

                rightIndex--;
            }

            takeLeft = !takeLeft;
        }

        return result;
    }

    private void SpawnCornerback(
    RuntimeReceiverAssignment target)
    {
        if (target == null ||
            target.receiver == null)
        {
            return;
        }

        Vector3 receiverLocalPosition =
            playOrigin.InverseTransformPoint(
                target.receiver.transform.position);

        Vector3 defenderLocalPosition =
            receiverLocalPosition;

        defenderLocalPosition.z +=
            FootballUnits.YardsToUnits(
                cornerDepthYards);

        defenderLocalPosition.x +=
            FootballUnits.YardsToUnits(
                cornerLateralAdjustmentYards);

        SpawnDefender(
            target,
            DefensivePosition.Cornerback,
            defenderLocalPosition);
    }

    private void SpawnSafety(
    RuntimeReceiverAssignment target,
    int safetyIndex,
    int safetyCount)
    {
        if (target == null ||
            target.receiver == null)
        {
            return;
        }

        Vector3 receiverLocalPosition =
            playOrigin.InverseTransformPoint(
                target.receiver.transform.position);

        float centeredX =
            Mathf.Lerp(
                receiverLocalPosition.x,
                0f,
                safetyLateralBlendTowardMiddle);

        if (safetyCount > 1)
        {
            float side =
                safetyIndex == 0 ? -1f : 1f;

            centeredX +=
                side *
                FootballUnits.YardsToUnits(
                    safetyMiddleSpacingYards);
        }

        Vector3 defenderLocalPosition =
            new Vector3(
                centeredX,
                receiverLocalPosition.y,
                FootballUnits.YardsToUnits(
                    safetyDepthYards));

        SpawnDefender(
            target,
            DefensivePosition.Safety,
            defenderLocalPosition);
    }

    private void SpawnDefender(
    RuntimeReceiverAssignment target,
    DefensivePosition position,
    Vector3 localStartingPosition)
    {
        GameObject defenderObject =
            Instantiate(defenderPrefab);

        if (spawnedPlayersParent != null)
        {
            defenderObject.transform.SetParent(
                spawnedPlayersParent,
                true);
        }

        int defenseLayer = LayerMask.NameToLayer(defenseLayerName);
        SetPlayerRootLayer(defenderObject, defenseLayer);

        FootballDefenderController defender =
            defenderObject.GetComponent<
                FootballDefenderController>();

        FootballUniformVisual defenderUniform = defenderObject.GetComponentInChildren<FootballUniformVisual>(true);

        if (defenderUniform != null && defenseTeam != null)
        {
            defenderUniform.ApplyMaterial(defenseTeam.defensiveJerseyMaterial);
        }

        if (defender == null)
        {
            Debug.LogError(
                "Defender prefab needs a " +
                "FootballDefenderController.");

            Destroy(defenderObject);
            return;
        }

        float defenderSpeed =
            ResolveDefenderSpeed(target);

        defender.InitializeDefender(
            target.receiver,
            position,
            playOrigin,
            this,
            defenderSpeed);

        defender.PlayerAnimator?.GetSet(DefensiveRole.Coverage);

        Vector3 worldPosition =
            playOrigin.TransformPoint(
                localStartingPosition);

        Vector3 directionToReceiver =
            target.receiver.transform.position -
            worldPosition;

        directionToReceiver.y = 0f;

        Quaternion worldRotation =
            directionToReceiver.sqrMagnitude >
            0.0001f
                ? Quaternion.LookRotation(
                    directionToReceiver,
                    Vector3.up)
                : Quaternion.LookRotation(
                    -playOrigin.forward,
                    Vector3.up);

        defender.SetStartingPosition(
            worldPosition,
            worldRotation);

        runtimeDefenders.Add(defender);
        spawnedPlayers.Add(defenderObject);
    }

    private float ResolveDefenderSpeed(
    RuntimeReceiverAssignment target)
    {
        if (defaultDefenderSpeedYardsPerSecond > 0f)
        {
            return defaultDefenderSpeedYardsPerSecond;
        }

        FootballRoute route =
            target.assignment.route;

        if (route != null &&
            route.steps != null &&
            route.steps.Count > 0)
        {
            return route.steps[0]
                .speedYardsPerSecond;
        }

        return fallbackReceiverSpeedYardsPerSecond;
    }

    private void SpawnDefensiveFront()
    {
        runtimeFrontDefenders.Clear();

        DefensiveFrontType frontType = ResolveDefensiveFront();

        int defensiveLineCount = frontType == DefensiveFrontType.ThreeLinemenThreeLinebackers ? 3 : 4;

        int linebackerCount = 6 - defensiveLineCount;

        List<FootballFrontDefenderController> defensiveLinemen = SpawnFrontRow(
                    defensiveLineCount,
                    DefensiveFrontRole
                        .DefensiveLineman,
                    defensiveLineDepthYards,
                    defensiveLinemanPrefab);

        List<FootballFrontDefenderController> linebackers = SpawnFrontRow(
                    linebackerCount,
                    DefensiveFrontRole.Linebacker,
                    linebackerDepthYards,
                    linebackerPrefab);

        ConfigureLinebackerResponsibilities(linebackers);

        ConfigureRushAssignments(defensiveLinemen, linebackers);
    }

    private List<FootballFrontDefenderController> SpawnFrontRow(int count, DefensiveFrontRole role, float depthYards, GameObject prefab)
    {
        var result = new List<FootballFrontDefenderController>();

        float totalWidth =
            (count - 1) *
            defensiveLineSpacingYards;

        float startingX =
            -totalWidth * 0.5f;

        for (int i = 0; i < count; i++)
        {
            float x =
                startingX +
                i *
                defensiveLineSpacingYards;

            Vector2 offset = new Vector2(x, depthYards);

            Vector3 position = ConvertOffsetToWorldPosition(offset);

            GameObject defenderObject = Instantiate(prefab, position, Quaternion.LookRotation(-playOrigin.forward, Vector3.up));

            FootballFrontDefenderController defender = defenderObject.GetComponent<FootballFrontDefenderController>();

            defender.Initialize(
                this,
                role,
                quarterbackRunner,
                false,
                false);

            defender.PlayerAnimator?.GetSet(DefensiveRole.Rushing, role);

            FootballUniformVisual defenderUniform = defenderObject.GetComponentInChildren<FootballUniformVisual>(true);

            if (defenderUniform != null && defenseTeam != null)
            {
                defenderUniform.ApplyMaterial(defenseTeam.defensiveJerseyMaterial);
            }

            int defenseLayer = LayerMask.NameToLayer(defenseLayerName);
            SetPlayerRootLayer(defenderObject, defenseLayer);

            runtimeFrontDefenders.Add(defender);

            result.Add(defender);
        }

        return result;
    }

    public Vector3 ConvertOffsetToWorldPosition(Vector2 startingOffsetYards)
    {

        Vector3 localStartPosition =
            new Vector3(
                FootballUnits.YardsToUnits(
                    startingOffsetYards.x),
                0f,
                FootballUnits.YardsToUnits(
                    startingOffsetYards.y));

        Vector3 worldStartPosition = playOrigin.TransformPoint(localStartPosition);

        return worldStartPosition;
    }

    private void ConfigureLinebackerResponsibilities(List<FootballFrontDefenderController> linebackers)
    {
        int blitzCount = linebackers.Count == 3 ? 2 : 1;

        for (int i = 0; i < linebackers.Count; i++)
        {
            int swapIndex = Random.Range(i, linebackers.Count);

            (
                linebackers[i],
                linebackers[swapIndex]
            ) =
            (
                linebackers[swapIndex],
                linebackers[i]
            );
        }

        for (int i = 0; i < linebackers.Count; i++)
        {
            bool blitzing = i < blitzCount;

            linebackers[i].Initialize(
                this,
                DefensiveFrontRole.Linebacker,
                quarterbackRunner,
                blitzing,
                false);
        }
    }

    private void ConfigureRushAssignments(List<FootballFrontDefenderController> defensiveLinemen, List<FootballFrontDefenderController> linebackers)
    {
        var rushers =
            new List<
                FootballFrontDefenderController>();

        rushers.AddRange(
            defensiveLinemen);

        foreach (
            FootballFrontDefenderController linebacker
            in linebackers)
        {
            if (linebacker.IsBlitzing)
            {
                rushers.Add(linebacker);
            }
        }

        bool shouldProduceSack =
            expectedPlayResult ==
            FootballPlayResult.Sack;

        FootballFrontDefenderController
            designatedSacker = null;

        if (shouldProduceSack &&
            rushers.Count > 0)
        {
            designatedSacker =
                ChooseBestSackRusher(
                    rushers);
        }

        foreach (
            FootballFrontDefenderController rusher
            in rushers)
        {
            bool isSacker =
                rusher == designatedSacker;

            FootballOffensivePlayerController
                blocker =
                    FindClosestAvailableLineman(
                        rusher.transform.position,
                        isSacker
                            ? designatedSacker
                            : null);

            rusher.Initialize(
                this,
                rusher.Role,
                quarterbackRunner,
                true,
                isSacker);

            if (isSacker)
            {
                if (blocker != null)
                {
                    StartCoroutine(BulldozeBlockerWhenClose(rusher, blocker));
                }
            }
            else
            {
                rusher.AssignBlocker(blocker);
            }
        }
    }

    private FootballFrontDefenderController ChooseBestSackRusher(List<FootballFrontDefenderController> rushers)
    {
        FootballFrontDefenderController best =
            null;

        float bestDistance =
            float.PositiveInfinity;

        foreach (
            FootballFrontDefenderController rusher
            in rushers)
        {
            float distance =
                Vector3.Distance(
                    rusher.transform.position,
                    quarterbackRunner
                        .transform.position);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = rusher;
            }
        }

        return best;
    }

    private FootballOffensivePlayerController FindClosestAvailableLineman(Vector3 defenderPosition, FootballFrontDefenderController designatedSacker)
    {
        FootballOffensivePlayerController
            closest = null;

        float closestDistance =
            float.PositiveInfinity;

        foreach (
            FootballOffensivePlayerController lineman
            in runtimeOffensiveLinemen)
        {
            if (lineman == null)
            {
                continue;
            }

            float distance =
                Vector3.Distance(
                    defenderPosition,
                    lineman.transform.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = lineman;
            }
        }

        return closest;
    }

    private IEnumerator BulldozeBlockerWhenClose(FootballFrontDefenderController rusher, FootballOffensivePlayerController blocker)
    {
        if (rusher == null || blocker == null)
        {
            yield break;
        }

        float contactDistance = FootballUnits.YardsToUnits(0.75f);

        while (!playIsDead &&
               rusher != null &&
               blocker != null)
        {
            Vector3 rusherPosition =
                rusher.transform.position;

            Vector3 blockerPosition =
                blocker.transform.position;

            rusherPosition.y = 0f;
            blockerPosition.y = 0f;

            if (Vector3.Distance(
                    rusherPosition,
                    blockerPosition) <=
                contactDistance)
            {
                blocker.GetBulldozed();
                yield break;
            }

            yield return null;
        }
    }

    public void RegisterSack(FootballFrontDefenderController sacker)
    {
        if (playIsDead)
        {
            return;
        }

        FootballPlayerAnimator quarterbackAnimator = quarterbackObject.GetComponent<FootballPlayerAnimator>();

        if (quarterbackAnimator == null)
        {
            quarterbackAnimator = quarterbackObject.GetComponentInChildren<FootballPlayerAnimator>();
        }

        quarterbackAnimator?.TriggerTackled();

        if (targetCamera != null &&
            quarterbackObject != null)
        {
            targetCamera.SetTarget(
                quarterbackObject.transform,
                quarterbackCameraOffset);
        }

        onSack?.Invoke();

        Vector3 sackPosition = quarterbackObject != null ? quarterbackObject.transform.position : playOrigin.position;

        FinishPlay(FootballPlayResult.Sack, sackPosition, OffensiveRole.Quarterback, false, false, false);
    }

    private void ResolveRunBallCarrier()
    {
        runBallCarrier = null;

        if (selectedPlay == null)
        {
            return;
        }

        foreach (RuntimeReceiverAssignment receiver in runtimeReceivers)
        {
            if (receiver == null || receiver.assignment == null || receiver.receiver == null)
            {
                continue;
            }

            if (receiver.assignment.role == selectedPlay.runBallCarrierRole)
            {
                runBallCarrier = receiver;
                return;
            }
        }

        if (selectedPlay.playType == FootballPlayType.Run)
        {
            Debug.LogWarning(
                $"Run play '{selectedPlay.name}' could not find " +
                $"a player with role " +
                $"{selectedPlay.runBallCarrierRole}.");
        }
    }

    private void ExecuteSnap()
    {
        onSnap?.Invoke();

        float sharedReceiverSpeed =
            FindRepresentativeReceiverSpeed();


        foreach (RuntimeReceiverAssignment receiver in runtimeReceivers)
        {
            receiver.runner.StartPreparedRoute(
                receiver.assignment.route,
                playOrigin,
                receiver.assignment.releaseDelay);

            receiver.receiver.PlayerAnimator.SnapBall();
        }

        foreach (FootballOffensivePlayerController lineman in runtimeOffensiveLinemen)
        {
            if (lineman == null)
            {
                continue;
            }

            lineman.PlayerAnimator?.SnapBall();

            FootballRouteRunner runner = lineman.RouteRunner;

            lineman.EnterBlockState();
        }

        foreach (FootballDefenderController defender in runtimeDefenders)
        {
            if (defender != null)
            {
                defender.BeginCoverage();

                defender.PlayerAnimator.SnapBall();
            }
        }

        foreach (FootballFrontDefenderController defender in runtimeFrontDefenders)
        {
            FootballPlayerAnimator animator =
                defender.GetComponentInChildren<
                    FootballPlayerAnimator>();

            animator?.SnapBall();

            defender.BeginRush();
        }

        if (quarterbackRunner != null)
        {
            quarterbackRunner.StartQuarterbackDropback(
                playOrigin,
                quarterbackDropbackYards,
                sharedReceiverSpeed);

            quarterbackRunner.gameObject.GetComponent<FootballPlayerAnimator>().SnapBall();
        }

        StartPostSnapPlayLogic();
    }

    private void StartPostSnapPlayLogic()
    {
        if (postSnapDecisionCoroutine != null)
        {
            StopCoroutine(postSnapDecisionCoroutine);
        }

        postSnapDecisionCoroutine = StartCoroutine(PostSnapPlayRoutine());
    }

    private IEnumerator PostSnapPlayRoutine()
    {
        if (selectedPlay == null)
        {
            postSnapDecisionCoroutine = null;
            yield break;
        }

        switch (selectedPlay.playType)
        {
            case FootballPlayType.Pass:
                yield return
                    PassPlayRoutine();
                break;

            case FootballPlayType.Run:
                yield return
                    RunPlayRoutine();
                break;

            case FootballPlayType.Option:
                yield return
                    OptionPlayRoutine();
                break;
        }

        postSnapDecisionCoroutine = null;
    }

    private IEnumerator PassPlayRoutine()
    {
        yield return new WaitForSeconds(
            selectedPlay.choiceDelaySeconds);

        if (playIsDead ||
            ballHasBeenThrown)
        {
            yield break;
        }

        receiverChoiceCoroutine =
            StartCoroutine(
                OpenReceiverChoiceWindow());

        yield return receiverChoiceCoroutine;
    }

    private IEnumerator RunPlayRoutine()
    {
        handoffCompleted = false;

        yield return new WaitForSeconds(selectedPlay.handoffDelaySeconds);

        if (playIsDead)
        {
            yield break;
        }

        if (runBallCarrier == null || runBallCarrier.receiver == null || runBallCarrier.runner == null)
        {
            Debug.LogWarning(
                "Run play ended because no valid " +
                "ball carrier was found.");

            yield break;
        }

        float maximumWait = selectedPlay.maximumHandoffWaitSeconds;

        float elapsedWait = 0f;

        while (!IsRunnerInHandoffRange(runBallCarrier.receiver) &&elapsedWait < maximumWait)
        {
            if (playIsDead)
            {
                yield break;
            }

            elapsedWait += Time.deltaTime;
            yield return null;
        }

        CompleteHandoff(runBallCarrier);
    }

    private IEnumerator OptionPlayRoutine()
    {
        yield return new WaitForSeconds(selectedPlay.choiceDelaySeconds);

        if (playIsDead || ballHasBeenThrown)
        {
            yield break;
        }

        receiverChoiceCoroutine = StartCoroutine(OpenReceiverChoiceWindow());

        yield return receiverChoiceCoroutine;
    }

    private bool IsRunnerInHandoffRange(FootballReceiverTarget receiver)
    {
        if (receiver == null || quarterbackObject == null || selectedPlay == null)
        {
            return false;
        }

        Vector3 quarterbackPosition = quarterbackObject.transform.position;

        Vector3 runnerPosition = receiver.transform.position;

        quarterbackPosition.y = 0f;
        runnerPosition.y = 0f;

        float distanceUnits = Vector3.Distance(quarterbackPosition, runnerPosition);

        float maximumDistanceUnits = FootballUnits.YardsToUnits(selectedPlay.handoffDistanceYards);

        return distanceUnits <= maximumDistanceUnits;
    }

    private void CompleteHandoff(RuntimeReceiverAssignment ballCarrier)
    {
        if (playIsDead ||handoffCompleted || ballCarrier == null || ballCarrier.receiver == null)
        {
            return;
        }

        handoffCompleted = true;

        FootballReceiverTarget receiver = ballCarrier.receiver;

        runWasStarted = true;
        activeBallCarrierRole = receiver.Role;

        Transform catchPoint =
            receiver.CatchPoint != null
                ? receiver.CatchPoint
                : receiver.transform;

        if (activeBall != null)
        {
            activeBall.SetCatchEnabled(false);
            activeBall.AttachToReceiver(catchPoint);
        }

        if (quarterbackRunner != null)
        {
            quarterbackRunner.SetHasBall(false);
        }

        if (receiver.RouteRunner != null)
        {
            receiver.RouteRunner.SetHasBall(true);
        }

        if (targetCamera != null)
        {
            targetCamera.SetTarget(receiver.transform,receiverCameraOffset);
        }

        onHandoff?.Invoke();

        Debug.Log(
            $"Handoff completed to " +
            $"{receiver.DisplayName}.");
    }

    private IEnumerator OpenReceiverChoiceWindow()
    {
        if (runtimeReceivers.Count == 0)
        {
            yield break;
        }

        receiverChoiceIsOpen = true;
        pendingThrowTarget = null;

        Time.timeScale =
            receiverChoiceTimeScale;

        if (receiverSelectionPanel != null)
        {
            receiverSelectionPanel.Show(
                runtimeReceivers,
                this);
        }

        onReceiverChoiceOpened?.Invoke();

        /*
         * Always wait for the full real-time duration.
         *
         * Clicking a button does not stop this coroutine.
         */
        yield return new WaitForSecondsRealtime(
            receiverChoiceRealSeconds);

        receiverChoiceIsOpen = false;

        receiverSelectionPanel.Hide();

        RestoreNormalTime();

        FootballReceiverTarget finalSelection =
            pendingThrowTarget;

        pendingThrowTarget = null;
        receiverChoiceCoroutine = null;

        if (finalSelection != null)
        {
            ThrowToReceiver(finalSelection);
        }
        else if (selectedPlay != null &&
                 selectedPlay.playType ==
                 FootballPlayType.Pass)
        {
            BeginQuarterbackScramble();
        }
        else
        {
            Debug.Log(
                "Receiver selection period ended " +
                "without a selected target.");
        }
    }

    private void ThrowToReceiver(FootballReceiverTarget receiver)
    {
        if (footballPrefab == null)
        {
            Debug.LogError(
                "No football prefab assigned.");

            return;
        }

        if (playIsDead)
        {
            return;
        }

        if (expectedPlayResult == FootballPlayResult.Sack)
        {
            return;
        }

        ballHasBeenThrown = true;

        if (quarterbackRunner != null)
        {
            quarterbackRunner.StopMovement();
        }

        quarterbackRunner.gameObject.GetComponent<FootballPlayerAnimator>().QuarterbackThrow();

        Vector3 throwPosition =
            quarterbackThrowPoint != null
                ? quarterbackThrowPoint.position
                : quarterbackObject.transform.position +
                  Vector3.up * 1.5f;

        Vector3 predictedCatchPosition = receiver.PredictCatchPosition(ballFlightTime);

        bool isOptionRunSelection = selectedPlay != null && selectedPlay.playType == FootballPlayType.Option && receiver.Role == selectedPlay.runBallCarrierRole;

        activeBall = Instantiate(footballPrefab, throwPosition, Quaternion.identity);

        float throwDuration = isOptionRunSelection ? 0.12f : ballFlightTime;

        float throwArcHeight = isOptionRunSelection ? 0.15f : ballLobHeight;

        activeBall.SetCatchEnabled(true);
        activeBall.ThrowLob(throwPosition, predictedCatchPosition, throwDuration, throwArcHeight, receiver, this);

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

    public void UpdatePendingThrowTarget(
    FootballReceiverTarget receiver)
    {
        if (!receiverChoiceIsOpen ||
            ballHasBeenThrown || playIsDead)
        {
            return;
        }

        pendingThrowTarget = receiver;
    }

    public void RegisterInterception(
    FootballDefenderController defender,
    FootballBall ball)
    {
        if (playIsDead ||
            defender == null)
        {
            return;
        }

        if (targetCamera != null)
        {
            targetCamera.SetTarget(
                defender.transform,
                receiverCameraOffset);
        }

        if (ballArrivalAnimationCoroutine != null)
        {
            StopCoroutine(
                ballArrivalAnimationCoroutine);

            ballArrivalAnimationCoroutine = null;
        }

        onInterception?.Invoke();

        Debug.Log($"Interception by {defender.name}.");

        Vector3 interceptionPosition = defender.transform.position;

        FinishPlay(FootballPlayResult.Interception, interceptionPosition, OffensiveRole.Quarterback, true, false, false);
    }

    public void RegisterTackle(
    FootballDefenderController defender,
    FootballRouteRunner ballCarrier)
    {
        if (playIsDead || defender == null || ballCarrier == null)
        {
            return;
        }

        FootballPlayResult result;

        bool wasPass =
            passWasCompleted;

        bool wasRun =
            runWasStarted ||
            handoffCompleted;

        bool wasScramble =
            quarterbackIsScrambling;

        if (passWasCompleted)
        {
            result =
                FootballPlayResult.Completion;
        }
        else if (runWasStarted ||
                 handoffCompleted ||
                 quarterbackIsScrambling)
        {
            result =
                FootballPlayResult.Run;
        }
        else
        {
            result =
                FootballPlayResult.Tackle;
        }

        defender.PlayerAnimator?.TriggerTackle();
        ballCarrier.PlayerAnimator?.TriggerTackled();

        defender.StopCoverage();
        ballCarrier.StopMovement();

        if (targetCamera != null)
        {
            targetCamera.SetTarget(
                ballCarrier.transform,
                receiverCameraOffset);
        }

        onTackle?.Invoke();

        FinishPlay(
            result,
            ballCarrier.transform.position,
            ballCarrier.Role,
            wasPass,
            wasRun,
            wasScramble);
    }

    public void RegisterIncompletion(
    Vector3 ballEndPosition)
    {
        if (playIsDead ||
            !ballHasBeenThrown)
        {
            return;
        }

        FinishPlay(FootballPlayResult.Incompletion, playOrigin != null ? playOrigin.position : ballEndPosition, OffensiveRole.Quarterback, true, false, false);
    }

    public void RegisterTouchdown(FootballRouteRunner ballCarrierRunner)
    {
        if (playIsDead ||
            ballCarrierRunner == null ||
            !ballCarrierRunner.HasBall)
        {
            return;
        }

        Transform ballCarrierTransform =
            ballCarrierRunner.transform;

        OffensiveRole ballCarrierRole =
            ResolveBallCarrierRole(
                ballCarrierRunner);

        bool wasScramble =
            quarterbackIsScrambling;

        bool wasRun =
            handoffCompleted ||
            runWasStarted ||
            wasScramble;

        bool wasPass =
            passWasCompleted &&
            !wasRun;

        ballCarrierRunner.StopMovement();

        FootballPlayerAnimator playerAnimator =
            ballCarrierRunner.GetComponentInChildren<
                FootballPlayerAnimator>(true);

        playerAnimator?.Touchdown();

        if (targetCamera != null)
        {
            targetCamera.SetTarget(
                ballCarrierTransform,
                receiverCameraOffset);
        }

        float touchdownYards =
            CalculateTouchdownYards();

        onTouchdown?.Invoke();

        FinishPlay(
            FootballPlayResult.Touchdown,
            ballCarrierTransform.position,
            ballCarrierRole,
            wasPass,
            wasRun,
            wasScramble);
    }

    private OffensiveRole ResolveBallCarrierRole(
    FootballRouteRunner runner)
    {
        if (runner == null)
        {
            return OffensiveRole.Quarterback;
        }

        if (runner == quarterbackRunner)
        {
            return OffensiveRole.Quarterback;
        }

        FootballOffensivePlayerController
            offensiveController =
                runner.GetComponent<
                    FootballOffensivePlayerController>();

        if (offensiveController == null)
        {
            offensiveController =
                runner.GetComponentInParent<
                    FootballOffensivePlayerController>();
        }

        if (offensiveController != null)
        {
            return offensiveController.Role;
        }

        foreach (RuntimeReceiverAssignment receiver in runtimeReceivers)
        {
            if (receiver == null ||
                receiver.runner != runner)
            {
                continue;
            }

            return receiver.assignment.role;
        }

        Debug.LogWarning(
            "Could not resolve touchdown ball-carrier role. " +
            "Defaulting to Quarterback.");

        return OffensiveRole.Quarterback;
    }

    private float CalculateTouchdownYards()
    {
        if (playOrigin == null ||
            opponentGoalLineReference == null)
        {
            Debug.LogWarning(
                "Cannot calculate touchdown yardage because " +
                "the play origin or opponent goal-line " +
                "reference is missing.");

            return 0f;
        }

        Vector3 displacement =
            opponentGoalLineReference.position -
            playOrigin.position;

        Vector3 offensiveForward =
            playOrigin.forward;

        offensiveForward.y = 0f;

        if (offensiveForward.sqrMagnitude <=
            0.0001f)
        {
            return 0f;
        }

        offensiveForward.Normalize();

        float distanceInUnits =
            Vector3.Dot(
                displacement,
                offensiveForward);

        float oneYardInUnits =
            FootballUnits.YardsToUnits(1f);

        if (Mathf.Abs(oneYardInUnits) <=
            0.0001f)
        {
            return 0f;
        }

        float yards =
            distanceInUnits /
            oneYardInUnits;

        yards = Mathf.Max(0f, yards);

        return Mathf.Round(
            yards * 10f) / 10f;
    }

    private void StopAllPlayerMovement()
    {
        if (quarterbackRunner != null)
        {
            quarterbackRunner.StopMovement();
        }

        foreach (RuntimeReceiverAssignment receiver
                 in runtimeReceivers)
        {
            if (receiver?.runner != null)
            {
                receiver.runner.StopMovement();
            }
        }

        foreach (FootballOffensivePlayerController lineman in runtimeOffensiveLinemen)
        {
            FootballRouteRunner runner = lineman.GetComponent<FootballRouteRunner>();

            runner?.StopMovement();
        }

        foreach (FootballDefenderController defender
                 in runtimeDefenders)
        {
            if (defender != null)
            {
                defender.StopCoverage();
            }
        }

        foreach (FootballFrontDefenderController defender in runtimeFrontDefenders)
        {
            defender?.StopRush();
        }
    }

    private static void SetPlayerRootLayer(GameObject player, int playerLayer)
    {
        if (player == null)
        {
            return;
        }

        player.layer = playerLayer;

        foreach (Transform child in player.transform)
        {
            Collider childCollider =
                child.GetComponent<Collider>();

            if (childCollider != null &&
                childCollider.isTrigger)
            {
                continue;
            }

            child.gameObject.layer = playerLayer;
        }
    }

    private void ScheduleBallArrivalAnimations(FootballReceiverTarget intendedReceiver, Vector3 projectedCatchPosition, float flightDuration)
    {
        if (ballArrivalAnimationCoroutine != null)
        {
            StopCoroutine(ballArrivalAnimationCoroutine);
        }

        ballArrivalAnimationCoroutine =StartCoroutine(BallArrivalAnimationRoutine(intendedReceiver, projectedCatchPosition, flightDuration));
    }

    private IEnumerator BallArrivalAnimationRoutine(FootballReceiverTarget intendedReceiver, Vector3 projectedCatchPosition, float flightDuration)
    {
        float elapsed = 0f;

        bool catchAnimationTriggered = false;
        bool deflectionAnimationsTriggered = false;

        float catchTriggerTime = Mathf.Max(0f, flightDuration - catchAnimationLeadSeconds);

        float deflectionTriggerTime = Mathf.Max(0f, flightDuration - deflectionAnimationLeadSeconds);

        while (elapsed < flightDuration)
        {
            if (playIsDead)
            {
                ballArrivalAnimationCoroutine = null;
                yield break;
            }

            elapsed += Time.deltaTime;

            if (!catchAnimationTriggered && elapsed >= catchTriggerTime)
            {
                catchAnimationTriggered = true;

                intendedReceiver?.PlayerAnimator?.TriggerCatchAttempt();
            }

            if (!deflectionAnimationsTriggered && elapsed >= deflectionTriggerTime)
            {
                deflectionAnimationsTriggered = true;

                TriggerNearbyDeflectionAttempts(projectedCatchPosition, intendedReceiver);
            }

            yield return null;
        }

        ballArrivalAnimationCoroutine = null;
    }

    private void TriggerNearbyDeflectionAttempts(Vector3 projectedCatchPosition, FootballReceiverTarget intendedReceiver)
    {
        float maximumDistanceUnits = FootballUnits.YardsToUnits(deflectionAttemptRadiusYards);

        foreach (FootballDefenderController defender
                 in runtimeDefenders)
        {
            if (defender == null ||
                !defender.IsActiveDefender)
            {
                continue;
            }

            Vector3 defenderPosition =
                defender.transform.position;

            defenderPosition.y = 0f;

            Vector3 catchPosition =
                projectedCatchPosition;

            catchPosition.y = 0f;

            float distance =
                Vector3.Distance(
                    defenderPosition,
                    catchPosition);

            Vector3 predictedDefenderPosition =
                defender.PredictPositionAtTime(
                    deflectionAnimationLeadSeconds);

            predictedDefenderPosition.y = 0f;

            float predictedDistance =
                Vector3.Distance(
                    predictedDefenderPosition,
                    catchPosition);

            if (Mathf.Min(distance, predictedDistance) <=
                maximumDistanceUnits)
            {
                defender.PlayerAnimator?
                    .TriggerDeflectionAttempt();
            }
        }
    }

    private void AssignOffensiveLineBlocks(FootballFrontDefenderController designatedSacker)
    {
        var unassignedDefenders = new List<FootballFrontDefenderController>();

        foreach (FootballFrontDefenderController defender
                 in runtimeFrontDefenders)
        {
            if (defender == null)
            {
                continue;
            }

            if (!defender.IsRushing)
            {
                continue;
            }

            if (defender == designatedSacker)
            {
                continue;
            }

            unassignedDefenders.Add(defender);
        }

        foreach (FootballOffensivePlayerController lineman
                 in runtimeOffensiveLinemen)
        {
            if (lineman == null ||
                unassignedDefenders.Count == 0)
            {
                break;
            }

            FootballFrontDefenderController
                closestDefender =
                    FindClosestDefender(
                        lineman.transform.position,
                        unassignedDefenders);

            if (closestDefender == null)
            {
                continue;
            }

            lineman.AssignDefender(
                closestDefender);

            closestDefender.AssignBlocker(
                lineman);

            unassignedDefenders.Remove(
                closestDefender);
        }

        foreach (FootballFrontDefenderController defender in unassignedDefenders)
        {
            FootballOffensivePlayerController
                closestLineman =
                    FindClosestLineman(
                        defender.transform.position);

            defender.AssignBlocker(
                closestLineman);
        }
    }

    private static FootballFrontDefenderController FindClosestDefender(Vector3 linemanPosition, List<FootballFrontDefenderController> availableDefenders)
    {
        FootballFrontDefenderController closest =
            null;

        float closestDistanceSquared =
            float.PositiveInfinity;

        foreach (FootballFrontDefenderController defender
                 in availableDefenders)
        {
            if (defender == null)
            {
                continue;
            }

            Vector3 difference =
                defender.transform.position -
                linemanPosition;

            difference.y = 0f;

            float distanceSquared =
                difference.sqrMagnitude;

            if (distanceSquared <
                closestDistanceSquared)
            {
                closestDistanceSquared =
                    distanceSquared;

                closest = defender;
            }
        }

        return closest;
    }

    private FootballOffensivePlayerController FindClosestLineman(Vector3 defenderPosition)
    {
        FootballOffensivePlayerController closest =
            null;

        float closestDistanceSquared =
            float.PositiveInfinity;

        foreach (FootballOffensivePlayerController lineman
                 in runtimeOffensiveLinemen)
        {
            if (lineman == null)
            {
                continue;
            }

            Vector3 difference =
                lineman.transform.position -
                defenderPosition;

            difference.y = 0f;

            float distanceSquared =
                difference.sqrMagnitude;

            if (distanceSquared <
                closestDistanceSquared)
            {
                closestDistanceSquared =
                    distanceSquared;

                closest = lineman;
            }
        }

        return closest;
    }

    private void AssignDesignatedSacker(FootballFrontDefenderController sacker)
    {
        if (sacker == null)
        {
            return;
        }

        FootballOffensivePlayerController
            nearestLineman =
                FindClosestLineman(
                    sacker.transform.position);

        sacker.AssignBulldozeTarget(
            nearestLineman);
    }

    private void BeginQuarterbackScramble()
    {
        if (playIsDead ||
            ballHasBeenThrown ||
            handoffCompleted)
        {
            return;
        }

        if (selectedPlay == null ||
            selectedPlay.playType !=
            FootballPlayType.Pass)
        {
            return;
        }

        if (quarterbackObject == null ||
            quarterbackRunner == null)
        {
            Debug.LogWarning(
                "Cannot begin scramble because the " +
                "quarterback is missing.");

            return;
        }

        FootballRoute scrambleRoute =
            SelectRandomScrambleRoute();

        if (scrambleRoute == null)
        {
            Debug.LogWarning(
                "Cannot begin scramble because no valid " +
                "scramble routes are assigned.");

            return;
        }

        quarterbackIsScrambling = true;
        runWasStarted = true;
        activeBallCarrierRole = OffensiveRole.Quarterback;

        quarterbackRunner.StopMovement();

        quarterbackRunner.SetHasBall(true);

        quarterbackRunner.PlayerAnimator.Scramble();

        if (targetCamera != null)
        {
            targetCamera.SetTarget(
                quarterbackObject.transform,
                receiverCameraOffset);
        }

        quarterbackRunner.StartPreparedRoute(
            scrambleRoute,
            playOrigin,
            scrambleRouteDelaySeconds);

        onScramble?.Invoke();

        Debug.Log(
            $"Quarterback scramble started using " +
            $"route '{scrambleRoute.routeName}'.");
    }

    private FootballRoute SelectRandomScrambleRoute()
    {
        if (scrambleRoutes == null ||
            scrambleRoutes.Count == 0)
        {
            return null;
        }

        int validRouteCount = 0;

        foreach (FootballRoute route
                 in scrambleRoutes)
        {
            if (route != null)
            {
                validRouteCount++;
            }
        }

        if (validRouteCount == 0)
        {
            return null;
        }

        int selectedValidIndex =
            Random.Range(
                0,
                validRouteCount);

        foreach (FootballRoute route
                 in scrambleRoutes)
        {
            if (route == null)
            {
                continue;
            }

            if (selectedValidIndex == 0)
            {
                return route;
            }

            selectedValidIndex--;
        }

        return null;
    }

    private void FinishPlay(
    FootballPlayResult result,
    Vector3 finalWorldPosition,
    OffensiveRole ballCarrierRole,
    bool wasPass,
    bool wasRun,
    bool wasScramble)
    {
        if (playIsDead)
        {
            return;
        }

        playIsDead = true;

        float finalYards =
            CalculateYardsFromLineOfScrimmage(
                finalWorldPosition);

        finalOutcome =
            new FootballPlayOutcome
            {
                result = result,
                yards = finalYards,
                ballCarrierRole = ballCarrierRole,
                wasPass = wasPass,
                wasRun = wasRun,
                wasScramble = wasScramble
            };

        if (receiverChoiceCoroutine != null)
        {
            StopCoroutine(receiverChoiceCoroutine);
            receiverChoiceCoroutine = null;
        }

        if (postSnapDecisionCoroutine != null)
        {
            StopCoroutine(postSnapDecisionCoroutine);
            postSnapDecisionCoroutine = null;
        }

        if (ballArrivalAnimationCoroutine != null)
        {
            StopCoroutine(ballArrivalAnimationCoroutine);
            ballArrivalAnimationCoroutine = null;
        }

        receiverChoiceIsOpen = false;
        pendingThrowTarget = null;

        receiverSelectionPanel?.Hide();

        RestoreNormalTime();
        StopAllPlayerMovement();

        onFinalOutcome?.Invoke(finalOutcome);
        onPlayEnded?.Invoke();
    }

    private float CalculateYardsFromLineOfScrimmage(
    Vector3 finalWorldPosition)
    {
        if (playOrigin == null)
        {
            return 0f;
        }

        Vector3 displacement =
            finalWorldPosition -
            playOrigin.position;

        /*
         * Projects the final position onto the offense's forward
         * field direction.
         *
         * Lateral movement does not count as yardage.
         */
        float forwardUnits =
            Vector3.Dot(
                displacement,
                playOrigin.forward);

        float oneYardInUnits =
            FootballUnits.YardsToUnits(1f);

        if (Mathf.Abs(oneYardInUnits) <=
            0.0001f)
        {
            return 0f;
        }

        float yards =
            forwardUnits /
            oneYardInUnits;

        /*
         * Prevent tiny floating-point values such as 4.999997.
         */
        return Mathf.Round(yards * 10f) / 10f;
    }
}


public static class OffensiveRoleUtility
{
    public static bool IsOffensiveLineman(OffensiveRole role)
    {
        return role == OffensiveRole.LeftTackle ||
               role == OffensiveRole.LeftGuard ||
               role == OffensiveRole.Center ||
               role == OffensiveRole.RightGuard ||
               role == OffensiveRole.RightTackle;
    }

    public static bool IsSkillPlayer(OffensiveRole role)
    {
        return role != OffensiveRole.Quarterback &&
               !IsOffensiveLineman(role);
    }
}