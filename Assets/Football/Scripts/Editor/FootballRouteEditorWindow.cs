using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class FootballRouteEditorWindow : EditorWindow
{
    private const float SidebarWidth = 220f;
    private const float MinimumPreviewHeight = 220f;
    private const float PreferredControlsHeight = 340f;
    private const float DotRadius = 9f;

    private readonly List<RouteStep> workingSteps = new();

    private string routeName = "New Route";
    private string commandInput = string.Empty;
    private string validationMessage = string.Empty;

    private Vector2 stepsScroll;
    private Vector2 routesScroll;
    private Vector2 controlsScroll;

    private List<FootballRoute> savedRoutes = new();

    private GUIStyle titleStyle;
    private GUIStyle centeredLabelStyle;
    private GUIStyle sidebarButtonStyle;

    private FootballRoute selectedSavedRoute;

    [MenuItem("Football/Route Editor")]
    public static void OpenWindow()
    {
        FootballRouteEditorWindow window =
            GetWindow<FootballRouteEditorWindow>();

        window.titleContent =
            new GUIContent("Route Editor");

        window.minSize =
            new Vector2(850f, 600f);

        window.Show();
    }

    private void OnEnable()
    {
        FootballEditorUtility.EnsureDataFoldersExist();
        RefreshSavedRoutes();
    }

    private void OnFocus()
    {
        RefreshSavedRoutes();
        Repaint();
    }

    private void InitializeStyles()
    {
        titleStyle ??= new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 17,
            alignment = TextAnchor.MiddleLeft
        };

        centeredLabelStyle ??=
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

        Rect sidebarRect =
            new Rect(
                0f,
                0f,
                SidebarWidth,
                fullRect.height);

        Rect mainRect =
            new Rect(
                SidebarWidth,
                0f,
                fullRect.width - SidebarWidth,
                fullRect.height);

        DrawSavedRouteSidebar(sidebarRect);
        DrawMainArea(mainRect);
    }

    private void DrawSavedRouteSidebar(Rect rect)
    {
        GUILayout.BeginArea(rect, EditorStyles.helpBox);

        GUILayout.Label("Saved Routes", titleStyle);

        if (GUILayout.Button("Refresh"))
        {
            RefreshSavedRoutes();
        }

        GUILayout.Space(4f);

        routesScroll =
            GUILayout.BeginScrollView(routesScroll);

        if (savedRoutes.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "No routes have been saved yet.",
                MessageType.Info);
        }

        foreach (FootballRoute route in savedRoutes)
        {
            if (route == null)
            {
                continue;
            }

            Color previousBackgroundColor =
    GUI.backgroundColor;

            if (route == selectedSavedRoute)
            {
                GUI.backgroundColor =
                    new Color(0.35f, 0.65f, 1f);
            }

            if (GUILayout.Button(
                    route.routeName,
                    sidebarButtonStyle))
            {
                selectedSavedRoute = route;
                LoadRouteIntoEditor(route);
            }

            GUI.backgroundColor =
                previousBackgroundColor;
        }

        GUILayout.EndScrollView();

        GUI.enabled = selectedSavedRoute != null;

        if (GUILayout.Button("Delete Selected Route"))
        {
            DeleteSelectedRoute();
        }

        GUI.enabled = true;

        GUILayout.EndArea();
    }

    private void DeleteSelectedRoute()
    {
        if (selectedSavedRoute == null)
        {
            return;
        }

        string routeNameToDelete =
            selectedSavedRoute.routeName;

        bool confirmed =
            EditorUtility.DisplayDialog(
                "Delete Route",
                $"Delete the route \"{routeNameToDelete}\"?\n\n" +
                "Any saved plays using this route may lose " +
                "their reference to it.",
                "Delete",
                "Cancel");

        if (!confirmed)
        {
            return;
        }

        string assetPath =
            AssetDatabase.GetAssetPath(
                selectedSavedRoute);

        bool deleted =
            AssetDatabase.DeleteAsset(assetPath);

        if (!deleted)
        {
            validationMessage =
                $"Unity could not delete {routeNameToDelete}.";

            return;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        selectedSavedRoute = null;

        RefreshSavedRoutes();

        validationMessage =
            $"Deleted route: {routeNameToDelete}";

        Repaint();
    }

    private void DrawMainArea(Rect rect)
    {
        const float padding = 12f;
        const float spacing = 8f;

        float availableHeight =
            rect.height - padding * 2f - spacing;

        float controlsHeight =
            Mathf.Min(
                PreferredControlsHeight,
                Mathf.Max(
                    180f,
                    availableHeight - MinimumPreviewHeight));

        float previewHeight =
            availableHeight - controlsHeight;

        Rect previewRect =
            new Rect(
                rect.x + padding,
                rect.y + padding,
                rect.width - padding * 2f,
                previewHeight);

        Rect controlsRect =
            new Rect(
                rect.x + padding,
                previewRect.yMax + spacing,
                rect.width - padding * 2f,
                controlsHeight);

        DrawRoutePreview(previewRect);
        DrawRouteControls(controlsRect);
    }

    private void DrawRoutePreview(Rect rect)
    {
        GUI.Box(rect, GUIContent.none);

        FootballEditorUtility.DrawFieldBackground(rect);

        Vector2 playerPosition =
            new Vector2(
                rect.center.x,
                rect.yMax - 34f);

        List<Vector2> routePoints =
            BuildWorkingPreviewPoints();

        float pixelsPerYard =
            CalculatePreviewScale(
                rect,
                routePoints);

        if (routePoints.Count > 1)
        {
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
            Handles.color = Color.yellow;

            Handles.DrawAAPolyLine(
                5f,
                screenPoints);

            Handles.color = oldColor;

            Handles.EndGUI();

            DrawRouteNodeDots(screenPoints);
        }

        FootballEditorUtility.DrawDot(
            playerPosition,
            DotRadius,
            new Color(0.2f, 0.65f, 1f));

        GUI.Label(
            new Rect(
                playerPosition.x - 45f,
                playerPosition.y + 14f,
                90f,
                22f),
            "Player",
            centeredLabelStyle);
    }

    private void DrawRouteNodeDots(Vector3[] screenPoints)
    {
        for (int i = 1; i < screenPoints.Length; i++)
        {
            FootballEditorUtility.DrawDot(
                new Vector2(
                    screenPoints[i].x,
                    screenPoints[i].y),
                4f,
                Color.yellow);
        }
    }

    private void DrawRouteControls(Rect rect)
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

        GUILayout.Label("Route Builder", titleStyle);

        routeName =
            EditorGUILayout.TextField(
                "Route Name",
                routeName);

        GUILayout.Space(4f);

        EditorGUILayout.LabelField(
            "Enter commands such as:",
            EditorStyles.boldLabel);

        EditorGUILayout.LabelField(
            "\"run 10 forward\", \"run 5 forward-left\", " +
            "\"slant 5 forward-left\", or " +
            "\"slant 5 left-forward\"",
            EditorStyles.wordWrappedLabel);

        GUILayout.Space(4f);

        GUILayout.BeginHorizontal();

        GUI.SetNextControlName("RouteCommandField");

        commandInput =
            EditorGUILayout.TextField(
                commandInput);

        if (GUILayout.Button(
                "Add Step",
                GUILayout.Width(100f)))
        {
            AddCommand();
        }

        GUILayout.EndHorizontal();

        HandleEnterKey();

        if (!string.IsNullOrWhiteSpace(validationMessage))
        {
            EditorGUILayout.HelpBox(
                validationMessage,
                MessageType.Warning);
        }

        GUILayout.Space(4f);

        DrawWorkingStepsList();

        GUILayout.Space(8f);

        GUILayout.BeginHorizontal();

        if (GUILayout.Button(
                "Clear Route",
                GUILayout.Height(28f)))
        {
            ClearWorkingRoute();
        }

        GUI.enabled =
            !string.IsNullOrWhiteSpace(routeName) &&
            workingSteps.Count > 0;

        if (GUILayout.Button(
                "Save Route",
                GUILayout.Height(28f)))
        {
            SaveRouteAsset();
        }

        GUI.enabled = true;

        GUILayout.EndHorizontal();

        GUILayout.Space(4f);

        EditorGUILayout.EndScrollView();

        GUILayout.EndArea();
    }

    private void DrawWorkingStepsList()
    {
        EditorGUILayout.LabelField(
            $"Steps ({workingSteps.Count})",
            EditorStyles.boldLabel);

        stepsScroll =
            EditorGUILayout.BeginScrollView(
                stepsScroll,
                GUILayout.MinHeight(70f),
                GUILayout.MaxHeight(130f));

        for (int i = 0; i < workingSteps.Count; i++)
        {
            RouteStep step = workingSteps[i];

            GUILayout.BeginHorizontal(
                EditorStyles.helpBox);

            GUILayout.Label(
                $"{i + 1}. Run " +
                $"{step.distanceYards:0.##} yd " +
                $"{step.direction}",
                GUILayout.ExpandWidth(true));

            if (GUILayout.Button(
                    "Remove",
                    GUILayout.Width(70f)))
            {
                workingSteps.RemoveAt(i);
                validationMessage = string.Empty;
                Repaint();
                GUIUtility.ExitGUI();
            }

            GUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }

    private void HandleEnterKey()
    {
        Event currentEvent = Event.current;

        if (currentEvent.type != EventType.KeyDown ||
            currentEvent.keyCode != KeyCode.Return)
        {
            return;
        }

        if (GUI.GetNameOfFocusedControl() !=
            "RouteCommandField")
        {
            return;
        }

        AddCommand();
        currentEvent.Use();
    }

    private void AddCommand()
    {
        if (!TryParseCommand(
                commandInput,
                out RouteStep step,
                out string error))
        {
            validationMessage = error;
            return;
        }

        workingSteps.Add(step);

        commandInput = string.Empty;
        validationMessage = string.Empty;

        GUI.FocusControl("RouteCommandField");

        Repaint();
    }

    private static bool TryParseCommand(
    string command,
    out RouteStep step,
    out string error)
    {
        step = null;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(command))
        {
            error = "Enter a route command first.";
            return false;
        }

        string normalized = command
            .Trim()
            .ToLowerInvariant()
            .Replace("-", " ");

        normalized = Regex.Replace(
            normalized,
            @"\s+",
            " ");

        Match match = Regex.Match(
            normalized,
            @"^run\s+" +
            @"(?<distance>\d+(\.\d+)?)\s*" +
            @"(?<unit>yards?|yd|y|feet|foot|ft|f)?\s+" +
            @"(?<direction>" +
            @"forward\s+left|" +
            @"forward\s+right|" +
            @"backward\s+left|" +
            @"backward\s+right|" +
            @"north\s+west|" +
            @"north\s+east|" +
            @"south\s+west|" +
            @"south\s+east|" +
            @"forward|backward|left|right|" +
            @"north|south|east|west)$",
            RegexOptions.IgnoreCase);

        if (!match.Success)
        {
            error =
                "Command not understood. Examples: " +
                "\"run 10 forward\", " +
                "\"run 5 left\", or " +
                "\"run 8 forward left\".";

            return false;
        }

        if (!float.TryParse(
                match.Groups["distance"].Value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float distance))
        {
            error = "The distance is invalid.";
            return false;
        }

        string unit =
            match.Groups["unit"].Value
                .ToLowerInvariant();

        float distanceYards =
            unit is "f" or "ft" or "foot" or "feet"
                ? distance / 3f
                : distance;

        if (distanceYards <= 0f)
        {
            error = "Distance must be greater than zero.";
            return false;
        }

        string directionText =
            match.Groups["direction"].Value
                .ToLowerInvariant();

        RouteDirection direction =
            directionText switch
            {
                "forward" or "north" =>
                    RouteDirection.Forward,

                "backward" or "south" =>
                    RouteDirection.Backward,

                "left" or "west" =>
                    RouteDirection.Left,

                "right" or "east" =>
                    RouteDirection.Right,

                "forward left" or "north west" =>
                    RouteDirection.ForwardLeft,

                "forward right" or "north east" =>
                    RouteDirection.ForwardRight,

                "backward left" or "south west" =>
                    RouteDirection.BackwardLeft,

                "backward right" or "south east" =>
                    RouteDirection.BackwardRight,

                _ => RouteDirection.Forward
            };

        step = new RouteStep
        {
            direction = direction,
            distanceYards = distanceYards,
            speedYardsPerSecond = 6f
        };

        return true;
    }

    private List<Vector2> BuildWorkingPreviewPoints()
    {
        var points = new List<Vector2>
        {
            Vector2.zero
        };

        Vector2 current = Vector2.zero;

        foreach (RouteStep step in workingSteps)
        {
            const float slantHorizontal = 0.8660254f;
            const float slantVertical = 0.5f;

            Vector2 direction = step.direction switch
            {
                RouteDirection.Forward =>
                    Vector2.up,

                RouteDirection.Backward =>
                    Vector2.down,

                RouteDirection.Left =>
                    Vector2.left,

                RouteDirection.Right =>
                    Vector2.right,

                // Normal 45-degree diagonals

                RouteDirection.ForwardLeft =>
                    new Vector2(
                        -1f,
                        1f).normalized,

                RouteDirection.ForwardRight =>
                    new Vector2(
                        1f,
                        1f).normalized,

                RouteDirection.BackwardLeft =>
                    new Vector2(
                        -1f,
                        -1f).normalized,

                RouteDirection.BackwardRight =>
                    new Vector2(
                        1f,
                        -1f).normalized,

                // Shallow slants

                RouteDirection.SlantForwardLeft =>
                    new Vector2(
                        -slantHorizontal,
                        slantVertical),

                RouteDirection.SlantForwardRight =>
                    new Vector2(
                        slantHorizontal,
                        slantVertical),

                RouteDirection.SlantBackwardLeft =>
                    new Vector2(
                        -slantHorizontal,
                        -slantVertical),

                RouteDirection.SlantBackwardRight =>
                    new Vector2(
                        slantHorizontal,
                        -slantVertical),

                _ => Vector2.zero
            };

            current +=
                direction * step.distanceYards;

            points.Add(current);
        }

        return points;
    }

    private static float CalculatePreviewScale(
        Rect previewRect,
        List<Vector2> routePoints)
    {
        float largestHorizontalDistance = 1f;
        float largestVerticalDistance = 1f;

        foreach (Vector2 point in routePoints)
        {
            largestHorizontalDistance =
                Mathf.Max(
                    largestHorizontalDistance,
                    Mathf.Abs(point.x));

            largestVerticalDistance =
                Mathf.Max(
                    largestVerticalDistance,
                    Mathf.Abs(point.y));
        }

        float availableHalfWidth =
            previewRect.width * 0.42f;

        float availableHeight =
            previewRect.height - 80f;

        float horizontalScale =
            availableHalfWidth /
            largestHorizontalDistance;

        float verticalScale =
            availableHeight /
            largestVerticalDistance;

        return Mathf.Clamp(
            Mathf.Min(horizontalScale, verticalScale),
            4f,
            18f);
    }

    private void SaveRouteAsset()
    {
        FootballEditorUtility.EnsureDataFoldersExist();

        FootballRoute route =
            CreateInstance<FootballRoute>();

        route.routeName = routeName.Trim();

        foreach (RouteStep workingStep in workingSteps)
        {
            route.steps.Add(
                new RouteStep
                {
                    direction =
                        workingStep.direction,

                    distanceYards =
                        workingStep.distanceYards,

                    speedYardsPerSecond =
                        workingStep.speedYardsPerSecond,

                    delayBeforeStep =
                        workingStep.delayBeforeStep,

                    stopAtEnd =
                        workingStep.stopAtEnd
                });
        }

        string safeName =
            FootballEditorUtility.MakeSafeFileName(
                route.routeName);

        string requestedPath =
            $"{FootballEditorUtility.RouteFolder}/" +
            $"{safeName}.asset";

        string uniquePath =
            AssetDatabase.GenerateUniqueAssetPath(
                requestedPath);

        AssetDatabase.CreateAsset(
            route,
            uniquePath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorGUIUtility.PingObject(route);
        Selection.activeObject = route;

        RefreshSavedRoutes();

        validationMessage =
            $"Saved route: {route.routeName}";
    }

    private void LoadRouteIntoEditor(
        FootballRoute route)
    {
        routeName = route.routeName;

        workingSteps.Clear();

        foreach (RouteStep savedStep in route.steps)
        {
            workingSteps.Add(
                new RouteStep
                {
                    direction =
                        savedStep.direction,

                    distanceYards =
                        savedStep.distanceYards,

                    speedYardsPerSecond =
                        savedStep.speedYardsPerSecond,

                    delayBeforeStep =
                        savedStep.delayBeforeStep,

                    stopAtEnd =
                        savedStep.stopAtEnd
                });
        }

        validationMessage =
            $"Loaded {route.routeName}. " +
            "Saving will create another route asset.";

        Repaint();
    }

    private void ClearWorkingRoute()
    {
        routeName = "New Route";
        commandInput = string.Empty;
        validationMessage = string.Empty;

        workingSteps.Clear();

        selectedSavedRoute = null;

        Repaint();
    }

    private void RefreshSavedRoutes()
    {
        savedRoutes =
            FootballEditorUtility.FindAllRoutes();
    }
}