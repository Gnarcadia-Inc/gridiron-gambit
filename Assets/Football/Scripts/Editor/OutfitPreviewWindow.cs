using UnityEditor;
using UnityEngine;

public class OutfitPreviewWindow : EditorWindow
{
    private PreviewRenderUtility preview;
    private GameObject previewInstance;
    private OutfitMaterialTargets targets;

    private Material material1;
    private Material material2;
    private Material material3;
    private Material material4;

    private string previewError;
    private Vector2 rotation = new(150f, -10f);
    private float previewSize = 1f;

    [MenuItem("Tools/Outfit Preview")]
    private static void OpenWindow()
    {
        GetWindow<OutfitPreviewWindow>("Outfit Preview");
    }

    private void OnEnable()
    {
        RebuildPreview();
    }

    private void OnDisable()
    {
        CleanupPreview();
    }

    private void OnSelectionChange()
    {
        RebuildPreview();
        Repaint();
    }

    private void OnGUI()
    {
        EditorGUI.BeginChangeCheck();

        material1 = (Material)EditorGUILayout.ObjectField(
            "Material 1", material1, typeof(Material), false);

        material2 = (Material)EditorGUILayout.ObjectField(
            "Material 2", material2, typeof(Material), false);

        material3 = (Material)EditorGUILayout.ObjectField(
            "Material 3", material3, typeof(Material), false);

        material4 = (Material)EditorGUILayout.ObjectField(
            "Material 4", material4, typeof(Material), false);

        if (EditorGUI.EndChangeCheck())
            ApplyMaterials();

        GUILayout.Space(8);

        Rect previewRect = GUILayoutUtility.GetRect(
            200,
            10000,
            200,
            10000,
            GUILayout.ExpandWidth(true),
            GUILayout.ExpandHeight(true));

        if (previewInstance == null)
        {
            EditorGUI.HelpBox(
                previewRect,
                previewError ??
                "Select your outfit prefab in the Project window.",
                MessageType.Info);

            return;
        }

        HandleInput(previewRect);
        DrawPreview(previewRect);
    }

    private void RebuildPreview()
    {
        CleanupPreview();
        previewError = null;

        GameObject selectedObject = Selection.activeGameObject;

        if (selectedObject == null)
        {
            previewError = "Select an outfit prefab in the Project window.";
            return;
        }

        GameObject prefabAsset = selectedObject;

        // Also permits selecting an instance in the Hierarchy.
        if (!PrefabUtility.IsPartOfPrefabAsset(prefabAsset))
        {
            GameObject source =
                PrefabUtility.GetCorrespondingObjectFromSource(prefabAsset);

            if (source != null)
                prefabAsset = source;
        }

        OutfitMaterialTargets assetTargets =
            prefabAsset.GetComponentInChildren<OutfitMaterialTargets>(true);

        if (assetTargets == null)
        {
            previewError =
                "The selected prefab does not contain OutfitMaterialTargets.";
            return;
        }

        preview = new PreviewRenderUtility();

        preview.cameraFieldOfView = 30f;
        preview.camera.backgroundColor = new Color(0.18f, 0.18f, 0.18f);
        preview.ambientColor = new Color(0.5f, 0.5f, 0.5f);

        preview.lights[0].intensity = 1.4f;
        preview.lights[0].transform.rotation =
            Quaternion.Euler(35f, 35f, 0f);

        preview.lights[1].intensity = 1f;
        preview.lights[1].transform.rotation =
            Quaternion.Euler(340f, 220f, 180f);

        previewInstance = Instantiate(prefabAsset);
        previewInstance.name = prefabAsset.name + " Preview";
        previewInstance.hideFlags = HideFlags.HideAndDontSave;
        previewInstance.SetActive(true);

        previewInstance.transform.position = Vector3.zero;
        previewInstance.transform.rotation = Quaternion.identity;
        previewInstance.transform.localScale = Vector3.one;

        targets =
            previewInstance.GetComponentInChildren<OutfitMaterialTargets>(true);

        SetChildrenActive(previewInstance);
        preview.AddSingleGO(previewInstance);

        CenterPreviewObject();

        material1 = GetMaterial(targets.Target1);
        material2 = GetMaterial(targets.Target2);
        material3 = GetMaterial(targets.Target3);
        material4 = GetMaterial(targets.Target4);

        ApplyMaterials();
    }

    private void CenterPreviewObject()
    {
        Bounds bounds = CalculateBounds(previewInstance);

        previewInstance.transform.position -= bounds.center;

        bounds = CalculateBounds(previewInstance);

        previewSize = Mathf.Max(
            bounds.extents.x,
            bounds.extents.y,
            bounds.extents.z,
            0.5f);
    }

    private void ApplyMaterials()
    {
        if (targets == null)
            return;

        SetMaterial(targets.Target1, material1);
        SetMaterial(targets.Target2, material2);
        SetMaterial(targets.Target3, material3);
        SetMaterial(targets.Target4, material4);

        Repaint();
    }

    private static Material GetMaterial(Renderer renderer)
    {
        return renderer != null ? renderer.sharedMaterial : null;
    }

    private static void SetMaterial(Renderer renderer, Material material)
    {
        if (renderer != null)
            renderer.sharedMaterial = material;
    }

    private void HandleInput(Rect rect)
    {
        Event current = Event.current;

        if (current.type == EventType.MouseDrag &&
            current.button == 0 &&
            rect.Contains(current.mousePosition))
        {
            rotation.x += current.delta.x;
            rotation.y += current.delta.y;
            rotation.y = Mathf.Clamp(rotation.y, -80f, 80f);

            current.Use();
            Repaint();
        }
    }

    private void DrawPreview(Rect rect)
    {
        if (Event.current.type != EventType.Repaint)
            return;

        preview.BeginPreview(rect, GUIStyle.none);

        previewInstance.transform.rotation =
            Quaternion.Euler(rotation.y, rotation.x, 0f);

        float distance = previewSize * 3.5f;

        preview.camera.transform.position =
            new Vector3(0f, 0f, -distance);

        preview.camera.transform.LookAt(Vector3.zero);

        preview.camera.nearClipPlane = 0.01f;
        preview.camera.farClipPlane = distance + previewSize * 5f;
        preview.camera.aspect = rect.width / Mathf.Max(rect.height, 1f);

        preview.Render(true);

        Texture texture = preview.EndPreview();
        GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, false);
    }

    private static Bounds CalculateBounds(GameObject root)
    {
        Renderer[] renderers =
            root.GetComponentsInChildren<Renderer>(true);

        bool foundRenderer = false;
        Bounds bounds = new Bounds(root.transform.position, Vector3.zero);

        foreach (Renderer renderer in renderers)
        {
            if (!renderer.enabled)
                continue;

            if (!foundRenderer)
            {
                bounds = renderer.bounds;
                foundRenderer = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return foundRenderer
            ? bounds
            : new Bounds(root.transform.position, Vector3.one);
    }

    private static void SetChildrenActive(GameObject root)
    {
        foreach (Transform child in
                 root.GetComponentsInChildren<Transform>(true))
        {
            child.gameObject.hideFlags = HideFlags.HideAndDontSave;
        }
    }

    private void CleanupPreview()
    {
        if (preview != null)
        {
            preview.Cleanup();
            preview = null;
        }

        previewInstance = null;
        targets = null;
    }
}