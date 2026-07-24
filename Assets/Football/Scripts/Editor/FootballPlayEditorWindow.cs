using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class FootballPlayEditorWindow : EditorWindow
{
    private const float LeftSidebarWidth = 220f;
    private const float RightSidebarWidth = 220f;
    private const float MinimumFieldHeight = 260f;
    private const float PreferredControlsHeight = 190f;
    private const float PlayerDotRadius = 11f;

    private readonly Dictionary<OffensiveRole, Vector2>
        formationOffsets = new();

    private readonly Dictionary<OffensiveRole, FootballRoute>
        routeAssignments = new();

    private readonly Dictionary<OffensiveRole, Vector2>
        playerScreenPositions = new();

    private static readonly OffensiveRole[] SkillPlayerRoles =
    {
        OffensiveRole.WideReceiverLeft,
        OffensiveRole.SlotReceiver,
        OffensiveRole.RunningBack,
        OffensiveRole.TightEnd,
        OffensiveRole.WideReceiverRight
    };

    private static readonly OffensiveRole[] OffensiveLineRoles =
    {
        OffensiveRole.LeftTackle,
        OffensiveRole.LeftGuard,
        OffensiveRole.Center,
        OffensiveRole.RightGuard,
        OffensiveRole.RightTackle
    };

    private static readonly OffensiveRole[] EditableOffensiveRoles =
    {
        OffensiveRole.WideReceiverLeft,
        OffensiveRole.SlotReceiver,
        OffensiveRole.LeftTackle,
        OffensiveRole.LeftGuard,
        OffensiveRole.Center,
        OffensiveRole.RightGuard,
        OffensiveRole.RightTackle,
        OffensiveRole.TightEnd,
        OffensiveRole.WideReceiverRight,
        OffensiveRole.RunningBack
    };

    private static readonly Dictionary<OffensiveRole, Vector2>
        DefaultFormation = new()
        {
            /*
             * X: negative is left, positive is right.
             * Y: positive is toward the defense, negative is behind the QB.
             */
            {
                OffensiveRole.Quarterback,
                new Vector2(0f, -2f)
            },
            {
                OffensiveRole.WideReceiverLeft,
                new Vector2(-14f, 0f)
            },
            {
                OffensiveRole.SlotReceiver,
                new Vector2(-8f, 0f)
            },
            {
                OffensiveRole.LeftTackle,
                new Vector2(-4f, 0f)
            },
            {
                OffensiveRole.LeftGuard,
                new Vector2(-2f, 0f)
            },
            {
                OffensiveRole.Center,
                new Vector2(0f, 0f)
            },
            {
                OffensiveRole.RightGuard,
                new Vector2(2f, 0f)
            },
            {
                OffensiveRole.RightTackle,
                new Vector2(4f, 0f)
            },
            {
                OffensiveRole.TightEnd,
                new Vector2(8f, 0f)
            },
            {
                OffensiveRole.WideReceiverRight,
                new Vector2(14f, 0f)
            },
            {
                OffensiveRole.RunningBack,
                new Vector2(0f, -4f)
            }
        };

    private List<FootballRoute> savedRoutes = new();
    private List<FootballPlay> savedPlays = new();

    private OffensiveRole selectedRole =
        OffensiveRole.WideReceiverLeft;

    private string playName = "New Play";
    private string message = string.Empty;

    private Vector2 routeSidebarScroll;
    private Vector2 playSidebarScroll;
    private Vector2 controlsScroll;

    private GUIStyle titleStyle;
    private GUIStyle centeredStyle;
    private GUIStyle sidebarButtonStyle;

    private FootballPlay selectedSavedPlay;

    [MenuItem("Football/Play Editor")]
    public static void OpenWindow()
    {
        FootballPlayEditorWindow window =
            GetWindow<FootballPlayEditorWindow>();

        window.titleContent =
            new GUIContent("Play Editor");

        window.minSize =
            new Vector2(1050f, 650f);

        window.Show();
    }

    private void OnEnable()
    {
        FootballEditorUtility.EnsureDataFoldersExist();

        SetDefaultFormation();
        RefreshAssets();
    }

    private void OnFocus()
    {
        RefreshAssets();
        Repaint();
    }

    private void InitializeStyles()
    {
        titleStyle ??= new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 17,
            alignment = TextAnchor.MiddleLeft
        };

        centeredStyle ??=
            new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter
            };

        sidebarButtonStyle ??=
            new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleLeft,
                fixedHeight = 28f
            };
    }

    private void OnGUI()
    {
        InitializeStyles();

        Rect fullRect =
            new Rect(
                0f,
                0f,
                position.width,
                position.height);

        Rect routeSidebarRect =
            new Rect(
                0f,
                0f,
                LeftSidebarWidth,
                fullRect.height);

        Rect playSidebarRect =
            new Rect(
                fullRect.width - RightSidebarWidth,
                0f,
                RightSidebarWidth,
                fullRect.height);

        Rect centerRect =
            new Rect(
                routeSidebarRect.xMax,
                0f,
                fullRect.width -
                LeftSidebarWidth -
                RightSidebarWidth,
                fullRect.height);

        DrawRouteSidebar(routeSidebarRect);
        DrawPlaySidebar(playSidebarRect);
        DrawCenterArea(centerRect);
    }

    private void DrawRouteSidebar(Rect rect)
    {
        GUILayout.BeginArea(rect, EditorStyles.helpBox);

        GUILayout.Label("Saved Routes", titleStyle);

        EditorGUILayout.HelpBox(
            "Select any offensive player, then click a route.",
            MessageType.Info);

        if (GUILayout.Button("Refresh"))
        {
            RefreshAssets();
        }

        routeSidebarScroll =
            GUILayout.BeginScrollView(
                routeSidebarScroll);

        if (savedRoutes.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "Create routes in the Route Editor first.",
                MessageType.Warning);
        }

        foreach (FootballRoute route in savedRoutes)
        {
            if (route == null)
            {
                continue;
            }

            if (GUILayout.Button(
                    route.routeName,
                    sidebarButtonStyle))
            {
                AssignRouteToSelectedPlayer(route);
            }
        }

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void DrawPlaySidebar(Rect rect)
    {
        GUILayout.BeginArea(rect, EditorStyles.helpBox);

        GUILayout.Label("Saved Plays", titleStyle);

        EditorGUILayout.HelpBox(
            "Click a play to preview it. Saving creates a new asset.",
            MessageType.Info);

        playSidebarScroll =
            GUILayout.BeginScrollView(
                playSidebarScroll);

        if (savedPlays.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "No plays have been saved yet.",
                MessageType.Info);
        }

        foreach (FootballPlay play in savedPlays)
        {
            if (play == null)
            {
                continue;
            }

            Color previousBackgroundColor =
                GUI.backgroundColor;

            if (play == selectedSavedPlay)
            {
                GUI.backgroundColor =
                    new Color(0.35f, 0.65f, 1f);
            }

            if (GUILayout.Button(
                    play.playName,
                    sidebarButtonStyle))
            {
                selectedSavedPlay = play;
                PreviewSavedPlay(play);
            }

            GUI.backgroundColor =
                previousBackgroundColor;
        }

        GUILayout.EndScrollView();

        GUI.enabled = selectedSavedPlay != null;

        if (GUILayout.Button("Delete Selected Play"))
        {
            DeleteSelectedPlay();
        }

        GUI.enabled = true;

        GUILayout.EndArea();
    }

    private void DeleteSelectedPlay()
    {
        if (selectedSavedPlay == null)
        {
            return;
        }

        string playNameToDelete =
            selectedSavedPlay.playName;

        bool confirmed =
            EditorUtility.DisplayDialog(
                "Delete Play",
                $"Delete the play \"{playNameToDelete}\"?",
                "Delete",
                "Cancel");

        if (!confirmed)
        {
            return;
        }

        string assetPath =
            AssetDatabase.GetAssetPath(
                selectedSavedPlay);

        bool deleted =
            AssetDatabase.DeleteAsset(assetPath);

        if (!deleted)
        {
            message =
                $"Unity could not delete {playNameToDelete}.";

            return;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        selectedSavedPlay = null;

        ClearWorkingPlay();
        RefreshAssets();

        message =
            $"Deleted play: {playNameToDelete}";

        Repaint();
    }

    private void DrawCenterArea(Rect rect)
    {
        const float padding = 12f;
        const float spacing = 8f;

        float availableHeight =
            rect.height - padding * 2f - spacing;

        float controlsHeight =
            Mathf.Min(
                PreferredControlsHeight,
                Mathf.Max(
                    150f,
                    availableHeight - MinimumFieldHeight));

        float fieldHeight =
            availableHeight - controlsHeight;

        Rect fieldRect =
            new Rect(
                rect.x + padding,
                rect.y + padding,
                rect.width - padding * 2f,
                fieldHeight);

        Rect controlsRect =
            new Rect(
                rect.x + padding,
                fieldRect.yMax + spacing,
                rect.width - padding * 2f,
                controlsHeight);

        DrawPlayField(fieldRect);
        DrawPlayControls(controlsRect);
    }

    private void DrawPlayField(Rect rect)
    {
        GUI.Box(rect, GUIContent.none);

        FootballEditorUtility.DrawFieldBackground(rect);

        CalculatePlayerScreenPositions(rect);
        DrawAssignedRoutes(rect);
        DrawPlayers();
        HandlePlayerClicks();
    }

    private void CalculatePlayerScreenPositions(Rect rect)
    {
        playerScreenPositions.Clear();

        float pixelsPerYard =
            Mathf.Clamp(
                rect.width / 55f,
                8f,
                16f);

        const float maximumBackfieldDepthYards = 10f;
        const float bottomPadding = 35f;

        float requiredBackfieldPixels = maximumBackfieldDepthYards *pixelsPerYard;

        Vector2 lineOfScrimmageOrigin = new Vector2(rect.center.x, rect.yMax - bottomPadding - requiredBackfieldPixels);

        AddPlayerScreenPosition(
            OffensiveRole.Quarterback,
            lineOfScrimmageOrigin,
            pixelsPerYard,
            rect);

        foreach (OffensiveRole role
                 in EditableOffensiveRoles)
        {
            AddPlayerScreenPosition(
                role,
                lineOfScrimmageOrigin,
                pixelsPerYard,
                rect);
        }
    }

    private void AddPlayerScreenPosition(
        OffensiveRole role,
        Vector2 quarterbackPosition,
        float pixelsPerYard,
        Rect fieldRect)
    {
        if (!formationOffsets.TryGetValue(
                role,
                out Vector2 offset))
        {
            offset = GetDefaultOffset(role);
            formationOffsets[role] = offset;
        }

        Vector2 screenPosition =
            new Vector2(
                quarterbackPosition.x +
                offset.x * pixelsPerYard,

                quarterbackPosition.y -
                offset.y * pixelsPerYard);

        const float margin = 35f;

        screenPosition.x =
            Mathf.Clamp(
                screenPosition.x,
                fieldRect.xMin + margin,
                fieldRect.xMax - margin);

        screenPosition.y =
            Mathf.Clamp(
                screenPosition.y,
                fieldRect.yMin + margin,
                fieldRect.yMax - margin);

        playerScreenPositions[role] =
            screenPosition;
    }

    private void DrawAssignedRoutes(Rect fieldRect)
    {
        foreach (KeyValuePair<
                     OffensiveRole,
                     FootballRoute> assignment
                 in routeAssignments)
        {
            OffensiveRole role = assignment.Key;
            FootballRoute route = assignment.Value;

            if (route == null ||
                !playerScreenPositions.TryGetValue(
                    role,
                    out Vector2 playerPosition))
            {
                continue;
            }

            List<Vector2> routePoints =
                FootballEditorUtility
                    .BuildRoutePreviewPoints(route);

            if (routePoints.Count < 2)
            {
                continue;
            }

            float pixelsPerYard =
                CalculatePlayRouteScale(
                    fieldRect,
                    routePoints);

            var screenPoints =
                new Vector3[routePoints.Count];

            for (int i = 0; i < routePoints.Count; i++)
            {
                Vector2 screenPoint =
                    FootballEditorUtility.RoutePointToScreen(
                        routePoints[i],
                        playerPosition,
                        pixelsPerYard);

                screenPoints[i] =
                    new Vector3(
                        screenPoint.x,
                        screenPoint.y,
                        0f);
            }

            Handles.BeginGUI();

            Color oldColor = Handles.color;

            Handles.color =
                role == selectedRole
                    ? Color.yellow
                    : IsOffensiveLineman(role)
                        ? new Color(1f, 0.7f, 0.3f)
                        : new Color(0.55f, 0.85f, 1f);

            Handles.DrawAAPolyLine(
                role == selectedRole ? 5f : 3f,
                screenPoints);

            Handles.color = oldColor;

            Handles.EndGUI();
        }
    }

    private static float CalculatePlayRouteScale(
        Rect fieldRect,
        List<Vector2> points)
    {
        float maximumDistance = 1f;

        foreach (Vector2 point in points)
        {
            maximumDistance =
                Mathf.Max(
                    maximumDistance,
                    Mathf.Abs(point.x),
                    Mathf.Abs(point.y));
        }

        float availableSize =
            Mathf.Min(
                fieldRect.width * 0.25f,
                fieldRect.height * 0.58f);

        return Mathf.Clamp(
            availableSize / maximumDistance,
            3f,
            11f);
    }

    private void DrawPlayers()
    {
        foreach (KeyValuePair<
                     OffensiveRole,
                     Vector2> player
                 in playerScreenPositions)
        {
            OffensiveRole role = player.Key;
            Vector2 center = player.Value;

            bool isQuarterback =
                role == OffensiveRole.Quarterback;

            bool selected = role == selectedRole;

            Color dotColor;

            if (isQuarterback)
            {
                dotColor =
                    new Color(1f, 0.4f, 0.25f);
            }
            else if (IsOffensiveLineman(role))
            {
                dotColor =
                    new Color(1f, 0.62f, 0.2f);
            }
            else
            {
                dotColor =
                    new Color(0.2f, 0.65f, 1f);
            }

            FootballEditorUtility.DrawDot(
                center,
                PlayerDotRadius,
                dotColor,
                selected);

            GUI.Label(
                new Rect(
                    center.x - 65f,
                    center.y + 14f,
                    130f,
                    23f),
                GetRoleDisplayName(role),
                centeredStyle);

            if (!isQuarterback &&
                routeAssignments.TryGetValue(
                    role,
                    out FootballRoute route) &&
                route != null)
            {
                GUI.Label(
                    new Rect(
                        center.x - 70f,
                        center.y + 34f,
                        140f,
                        20f),
                    route.routeName,
                    centeredStyle);
            }
        }
    }

    private void HandlePlayerClicks()
    {
        Event currentEvent = Event.current;

        if (currentEvent.type != EventType.MouseDown ||
            currentEvent.button != 0)
        {
            return;
        }

        foreach (KeyValuePair<OffensiveRole, Vector2> player in playerScreenPositions)
        {

            Rect clickableRect =
                new Rect(
                    player.Value.x -
                    PlayerDotRadius - 8f,

                    player.Value.y -
                    PlayerDotRadius - 8f,

                    (PlayerDotRadius + 8f) * 2f,
                    (PlayerDotRadius + 8f) * 2f);

            if (!clickableRect.Contains(
                    currentEvent.mousePosition))
            {
                continue;
            }

            selectedRole = player.Key;

            message =
                $"Selected " +
                $"{GetRoleDisplayName(selectedRole)}.";

            currentEvent.Use();
            Repaint();
            break;
        }
    }

    private void DrawPlayControls(Rect rect)
    {
        GUI.Box(rect, GUIContent.none);

        Rect scrollRect =
            new Rect(
                rect.x + 6f,
                rect.y + 6f,
                rect.width - 12f,
                rect.height - 12f);

        GUILayout.BeginArea(scrollRect);

        controlsScroll =
            EditorGUILayout.BeginScrollView(
                controlsScroll);

        GUILayout.Label("Play Builder", titleStyle);

        playName =
            EditorGUILayout.TextField(
                "Play Name",
                playName);

        FootballRoute assignedRoute =
            routeAssignments.TryGetValue(
                selectedRole,
                out FootballRoute route)
                ? route
                : null;

        EditorGUILayout.LabelField(
            "Selected Player",
            GetRoleDisplayName(selectedRole));

        EditorGUILayout.LabelField(
            "Player Group",
            IsOffensiveLineman(selectedRole)
                ? "Offensive Line"
                : "Skill Player");

        bool selectedPlayerIsQuarterback = selectedRole == OffensiveRole.Quarterback;

        EditorGUILayout.LabelField("Assigned Route", selectedPlayerIsQuarterback
            ? "QB dropback handled separately"
            : assignedRoute != null
                ? assignedRoute.routeName
                : "None");

        if (!formationOffsets.TryGetValue(
                selectedRole,
                out Vector2 selectedOffset))
        {
            selectedOffset =
                GetDefaultOffset(selectedRole);
        }

        EditorGUI.BeginChangeCheck();

        Vector2 updatedOffset =
            EditorGUILayout.Vector2Field(
                "Starting Position (Yards)",
                selectedOffset);

        if (EditorGUI.EndChangeCheck())
        {
            formationOffsets[selectedRole] =
                updatedOffset;

            message =
                $"{GetRoleDisplayName(selectedRole)} starts " +
                $"at X {updatedOffset.x:0.##}, " +
                $"Y {updatedOffset.y:0.##}.";

            Repaint();
        }

        EditorGUILayout.LabelField(
            "X: negative is left, positive is right. " +
            "Y: positive is forward, negative is backward.",
            EditorStyles.wordWrappedMiniLabel);

        GUILayout.Space(6f);

        GUILayout.BeginHorizontal();

        GUI.enabled = !selectedPlayerIsQuarterback && assignedRoute != null;

        if (GUILayout.Button(
                "Remove Route",
                GUILayout.Height(26f)))
        {
            routeAssignments.Remove(selectedRole);

            message =
                $"Removed route from " +
                $"{GetRoleDisplayName(selectedRole)}.";
        }

        GUI.enabled = true;

        if (GUILayout.Button(
                "Reset Position",
                GUILayout.Height(26f)))
        {
            formationOffsets[selectedRole] =
                GetDefaultOffset(selectedRole);

            message =
                $"Reset {GetRoleDisplayName(selectedRole)} " +
                "to its default position.";

            Repaint();
        }

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        if (GUILayout.Button(
                "New / Clear Play",
                GUILayout.Height(28f)))
        {
            ClearWorkingPlay();
        }

        GUI.enabled =
            !string.IsNullOrWhiteSpace(playName);

        if (GUILayout.Button(
                "Save Play",
                GUILayout.Height(28f)))
        {
            SavePlayAsset();
        }

        GUI.enabled = true;

        GUILayout.EndHorizontal();

        int assignedSkillRoutes =
            CountAssignedRoutes(SkillPlayerRoles);

        int assignedLineRoutes =
            CountAssignedRoutes(OffensiveLineRoles);

        EditorGUILayout.LabelField(
            "Formation",
            "1 QB + 5 skill players + 5 offensive linemen");

        EditorGUILayout.LabelField(
            "Routes",
            $"{assignedSkillRoutes}/5 skill, " +
            $"{assignedLineRoutes}/5 line");

        if (!string.IsNullOrWhiteSpace(message))
        {
            GUILayout.Space(4f);

            EditorGUILayout.HelpBox(
                message,
                MessageType.Info);
        }

        GUILayout.Space(4f);

        EditorGUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void AssignRouteToSelectedPlayer(
        FootballRoute route)
    {
        if (selectedRole ==
        OffensiveRole.Quarterback)
        {
            message =
                "The quarterback cannot be assigned a route. " +
                "His dropback is controlled separately.";

            Repaint();
            return;
        }

        routeAssignments[selectedRole] = route;

        message =
            $"Assigned {route.routeName} to " +
            $"{GetRoleDisplayName(selectedRole)}.";

        Repaint();
    }

    private void SavePlayAsset()
    {
        FootballEditorUtility.EnsureDataFoldersExist();

        FootballPlay play =
            CreateInstance<FootballPlay>();

        play.playName = playName.Trim();

        formationOffsets.TryGetValue(OffensiveRole.Quarterback, out Vector2 quarterbackOffset);
        play.quarterbackStartingOffsetYards = quarterbackOffset;

        foreach (OffensiveRole role in SkillPlayerRoles)
        {
            formationOffsets.TryGetValue(
                role,
                out Vector2 startingOffset);

            routeAssignments.TryGetValue(
                role,
                out FootballRoute assignedRoute);

            play.assignments.Add(
                new RouteAssignment
                {
                    role = role,
                    route = assignedRoute,
                    startingOffsetYards =
                        startingOffset,
                    releaseDelay = 0f
                });
        }

        /*
         * Always write all five offensive linemen using the same editor
         * formation and route dictionaries as the skill players.
         */
        play.offensiveLine.Clear();

        foreach (OffensiveRole role in OffensiveLineRoles)
        {
            formationOffsets.TryGetValue(
                role,
                out Vector2 startingOffset);

            routeAssignments.TryGetValue(
                role,
                out FootballRoute assignedRoute);

            play.offensiveLine.Add(
                new OffensiveLinePlayEntry
                {
                    role = role,
                    route = assignedRoute,
                    startingOffsetYards =
                        startingOffset,
                    endBehavior =
                        RouteEndBehavior.Block
                });
        }

        string safeName =
            FootballEditorUtility.MakeSafeFileName(
                play.playName);

        string requestedPath =
            $"{FootballEditorUtility.PlayFolder}/" +
            $"{safeName}.asset";

        string uniquePath =
            AssetDatabase.GenerateUniqueAssetPath(
                requestedPath);

        AssetDatabase.CreateAsset(
            play,
            uniquePath);

        EditorUtility.SetDirty(play);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorGUIUtility.PingObject(play);
        Selection.activeObject = play;

        RefreshAssets();

        message =
            $"Saved play: {play.playName} " +
            "(1 QB, 5 skill players, 5 linemen).";
    }

    private void PreviewSavedPlay(
        FootballPlay play)
    {
        if (play == null)
        {
            return;
        }

        playName = play.playName;

        routeAssignments.Clear();
        SetDefaultFormation();

        formationOffsets[OffensiveRole.Quarterback] = play.quarterbackStartingOffsetYards;

        if (play.assignments != null)
        {
            foreach (RouteAssignment assignment
                     in play.assignments)
            {
                if (assignment == null)
                {
                    continue;
                }

                formationOffsets[
                    assignment.role] =
                    assignment.startingOffsetYards;

                if (assignment.route != null)
                {
                    routeAssignments[
                        assignment.role] =
                        assignment.route;
                }
            }
        }

        if (play.offensiveLine != null)
        {
            foreach (OffensiveLinePlayEntry lineman
                     in play.offensiveLine)
            {
                if (lineman == null)
                {
                    continue;
                }

                formationOffsets[
                    lineman.role] =
                    lineman.startingOffsetYards;

                if (lineman.route != null)
                {
                    routeAssignments[
                        lineman.role] =
                        lineman.route;
                }
            }
        }

        selectedSavedPlay = play;

        message =
            $"Previewing saved play: {play.playName}. " +
            "Saving creates a separate asset.";

        Repaint();
    }

    private void ClearWorkingPlay()
    {
        playName = "New Play";

        routeAssignments.Clear();
        SetDefaultFormation();

        selectedRole =
            OffensiveRole.WideReceiverLeft;

        selectedSavedPlay = null;

        message =
            "Started a new 11-player offensive play.";

        Repaint();
    }

    private void SetDefaultFormation()
    {
        formationOffsets.Clear();

        foreach (KeyValuePair<
                     OffensiveRole,
                     Vector2> entry
                 in DefaultFormation)
        {
            formationOffsets[
                entry.Key] =
                entry.Value;
        }
    }

    private static Vector2 GetDefaultOffset(
        OffensiveRole role)
    {
        return DefaultFormation.TryGetValue(
            role,
            out Vector2 offset)
                ? offset
                : Vector2.zero;
    }

    private int CountAssignedRoutes(
        IEnumerable<OffensiveRole> roles)
    {
        int count = 0;

        foreach (OffensiveRole role in roles)
        {
            if (routeAssignments.TryGetValue(
                    role,
                    out FootballRoute route) &&
                route != null)
            {
                count++;
            }
        }

        return count;
    }

    private void RefreshAssets()
    {
        savedRoutes =
            FootballEditorUtility.FindAllRoutes();

        savedPlays =
            FootballEditorUtility.FindAllPlays();
    }

    private static bool IsOffensiveLineman(
        OffensiveRole role)
    {
        return role ==
                   OffensiveRole.LeftTackle ||
               role ==
                   OffensiveRole.LeftGuard ||
               role ==
                   OffensiveRole.Center ||
               role ==
                   OffensiveRole.RightGuard ||
               role ==
                   OffensiveRole.RightTackle;
    }

    private static string GetRoleDisplayName(
        OffensiveRole role)
    {
        return role switch
        {
            OffensiveRole.Quarterback => "QB",
            OffensiveRole.RunningBack => "RB",
            OffensiveRole.TightEnd => "TE",
            OffensiveRole.WideReceiverLeft => "WR Left",
            OffensiveRole.SlotReceiver => "Slot",
            OffensiveRole.WideReceiverRight => "WR Right",
            OffensiveRole.LeftTackle => "LT",
            OffensiveRole.LeftGuard => "LG",
            OffensiveRole.Center => "C",
            OffensiveRole.RightGuard => "RG",
            OffensiveRole.RightTackle => "RT",
            _ => role.ToString()
        };
    }
}
