using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class FootballEditorUtility
{
    public const string RootDataFolder =
        "Assets/Football/Data";

    public const string RouteFolder =
        "Assets/Football/Data/Routes";

    public const string PlayFolder =
        "Assets/Football/Data/Plays";

    public static void EnsureDataFoldersExist()
    {
        EnsureFolder("Assets", "Football");
        EnsureFolder("Assets/Football", "Data");
        EnsureFolder(RootDataFolder, "Routes");
        EnsureFolder(RootDataFolder, "Plays");
    }

    private static void EnsureFolder(
        string parent,
        string folderName)
    {
        string fullPath = $"{parent}/{folderName}";

        if (!AssetDatabase.IsValidFolder(fullPath))
        {
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }

    public static List<FootballRoute> FindAllRoutes()
    {
        EnsureDataFoldersExist();

        string[] guids =
            AssetDatabase.FindAssets(
                "t:FootballRoute",
                new[] { RouteFolder });

        var routes = new List<FootballRoute>();

        foreach (string guid in guids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(guid);

            FootballRoute route =
                AssetDatabase.LoadAssetAtPath<FootballRoute>(
                    path);

            if (route != null)
            {
                routes.Add(route);
            }
        }

        routes.Sort(
            (a, b) =>
                string.Compare(
                    a.routeName,
                    b.routeName,
                    System.StringComparison.OrdinalIgnoreCase));

        return routes;
    }

    public static List<FootballPlay> FindAllPlays()
    {
        EnsureDataFoldersExist();

        string[] guids =
            AssetDatabase.FindAssets(
                "t:FootballPlay",
                new[] { PlayFolder });

        var plays = new List<FootballPlay>();

        foreach (string guid in guids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(guid);

            FootballPlay play =
                AssetDatabase.LoadAssetAtPath<FootballPlay>(
                    path);

            if (play != null)
            {
                plays.Add(play);
            }
        }

        plays.Sort(
            (a, b) =>
                string.Compare(
                    a.playName,
                    b.playName,
                    System.StringComparison.OrdinalIgnoreCase));

        return plays;
    }

    public static List<Vector2> BuildRoutePreviewPoints(
        FootballRoute route)
    {
        var points = new List<Vector2>
        {
            Vector2.zero
        };

        if (route == null)
        {
            return points;
        }

        Vector2 current = Vector2.zero;

        foreach (RouteStep step in route.steps)
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

                // Shallow 30-degree slants

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

            current += direction * step.distanceYards;
            points.Add(current);
        }

        return points;
    }

    public static string MakeSafeFileName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Unnamed";
        }

        foreach (char invalidCharacter
                 in Path.GetInvalidFileNameChars())
        {
            value = value.Replace(
                invalidCharacter.ToString(),
                string.Empty);
        }

        value = value.Trim();

        return string.IsNullOrWhiteSpace(value)
            ? "Unnamed"
            : value;
    }

    public static void DrawFieldBackground(Rect rect)
    {
        EditorGUI.DrawRect(
            rect,
            new Color(0.08f, 0.25f, 0.12f));

        Handles.BeginGUI();

        Color previousColor = Handles.color;

        Handles.color =
            new Color(1f, 1f, 1f, 0.16f);

        const int horizontalLines = 10;

        for (int i = 1; i < horizontalLines; i++)
        {
            float normalized =
                i / (float)horizontalLines;

            float y =
                Mathf.Lerp(
                    rect.yMax,
                    rect.yMin,
                    normalized);

            Handles.DrawLine(
                new Vector3(rect.xMin, y),
                new Vector3(rect.xMax, y));
        }

        float centerX = rect.center.x;

        Handles.DrawLine(
            new Vector3(centerX, rect.yMin),
            new Vector3(centerX, rect.yMax));

        Handles.color = previousColor;

        Handles.EndGUI();
    }

    public static void DrawDot(
        Vector2 center,
        float radius,
        Color color,
        bool selected = false)
    {
        Color previousColor = GUI.color;

        GUI.color = color;

        Rect dotRect = new Rect(
            center.x - radius,
            center.y - radius,
            radius * 2f,
            radius * 2f);

        GUI.DrawTexture(
            dotRect,
            EditorGUIUtility.whiteTexture);

        if (selected)
        {
            GUI.color = Color.white;

            float border = 3f;

            GUI.DrawTexture(
                new Rect(
                    dotRect.x - border,
                    dotRect.y - border,
                    dotRect.width + border * 2f,
                    border),
                EditorGUIUtility.whiteTexture);

            GUI.DrawTexture(
                new Rect(
                    dotRect.x - border,
                    dotRect.yMax,
                    dotRect.width + border * 2f,
                    border),
                EditorGUIUtility.whiteTexture);

            GUI.DrawTexture(
                new Rect(
                    dotRect.x - border,
                    dotRect.y,
                    border,
                    dotRect.height),
                EditorGUIUtility.whiteTexture);

            GUI.DrawTexture(
                new Rect(
                    dotRect.xMax,
                    dotRect.y,
                    border,
                    dotRect.height),
                EditorGUIUtility.whiteTexture);
        }

        GUI.color = previousColor;
    }

    public static Vector2 RoutePointToScreen(
        Vector2 routePoint,
        Vector2 playerScreenPosition,
        float pixelsPerYard)
    {
        return new Vector2(
            playerScreenPosition.x +
            routePoint.x * pixelsPerYard,

            playerScreenPosition.y -
            routePoint.y * pixelsPerYard);
    }
}