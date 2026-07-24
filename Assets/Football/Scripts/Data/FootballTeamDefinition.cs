using UnityEngine;

[CreateAssetMenu(
    fileName = "New Football Team",
    menuName = "Football/Team")]
public class FootballTeamDefinition : ScriptableObject
{
    [Header("Identity")]

    public string teamName;

    public string abbreviation;

    public Sprite menuLogo;

    [Header("Uniforms")]

    public Material offensiveJerseyMaterial;

    public Material defensiveJerseyMaterial;

    [Header("Stadium")]

    public Sprite fieldSprite;

    [Header("Optional UI")]

    public Color primaryColor = Color.white;
    public Color secondaryColor = Color.black;
}