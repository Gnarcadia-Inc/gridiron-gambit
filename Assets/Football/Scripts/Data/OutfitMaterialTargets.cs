using UnityEngine;

public class OutfitMaterialTargets : MonoBehaviour
{
    [Header("Renderers controlled by the preview window")]
    [SerializeField] private Renderer target1;
    [SerializeField] private Renderer target2;
    [SerializeField] private Renderer target3;
    [SerializeField] private Renderer target4;

    public Renderer Target1 => target1;
    public Renderer Target2 => target2;
    public Renderer Target3 => target3;
    public Renderer Target4 => target4;
}