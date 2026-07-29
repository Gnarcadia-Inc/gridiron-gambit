using UnityEngine;
using System.Collections.Generic;

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

    public Color fieldFoamColour;
    public Color fieldRowsColour;
    public Color fieldSeatBackColour;
    public Color fieldSeatBottomColour;
    public Color fieldStandsColour;
    public Color fieldStandsAltColour;

    public List<RosterSpot> roster;
}