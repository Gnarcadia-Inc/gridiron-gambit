using UnityEngine;

public class FootballUniformVisual : MonoBehaviour
{
    [Header("Jersey Renderers")]

    [SerializeField]
    private SkinnedMeshRenderer[] jerseyRenderers;

    [Header("Material Slot")]

    [Tooltip(
        "The material index containing the jersey. " +
        "Use 0 if the renderer only has one material.")]
    [SerializeField]
    [Min(0)]
    private int jerseyMaterialIndex;

    public void ApplyMaterial(
        Material jerseyMaterial)
    {
        if (jerseyMaterial == null)
        {
            Debug.LogWarning(
                $"No jersey material was provided " +
                $"for {gameObject.name}.");

            return;
        }

        foreach (
            SkinnedMeshRenderer jerseyRenderer
            in jerseyRenderers)
        {
            if (jerseyRenderer == null)
            {
                continue;
            }

            Material[] materials =
                jerseyRenderer.sharedMaterials;

            if (materials == null ||
                materials.Length == 0)
            {
                Debug.LogWarning(
                    $"{jerseyRenderer.name} has no materials.");

                continue;
            }

            if (jerseyMaterialIndex >=
                materials.Length)
            {
                Debug.LogWarning(
                    $"Jersey material index " +
                    $"{jerseyMaterialIndex} is outside " +
                    $"the material array on " +
                    $"{jerseyRenderer.name}.");

                continue;
            }

            materials[jerseyMaterialIndex] =
                jerseyMaterial;

            jerseyRenderer.sharedMaterials =
                materials;
        }
    }
}