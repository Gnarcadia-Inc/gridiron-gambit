using UnityEngine;

public class FootballFieldVisual : MonoBehaviour
{
    [SerializeField]
    private SpriteRenderer fieldSpriteRenderer;

    [SerializeField]
    private Material foamMaterial;
    [SerializeField]
    private Material rowsMaterial;
    [SerializeField]
    private Material seatBackMaterial;
    [SerializeField]
    private Material seatBottomMaterial;
    [SerializeField]
    private Material standsMaterial;
    [SerializeField]
    private Material standsAltMaterial;

    private void Reset()
    {
        fieldSpriteRenderer =
            GetComponent<SpriteRenderer>();
    }

    private void Awake()
    {
        if (fieldSpriteRenderer == null)
        {
            fieldSpriteRenderer =
                GetComponent<SpriteRenderer>();
        }
    }

    public void ApplyTeamField(
        FootballTeamDefinition team)
    {
        if (team == null)
        {
            Debug.LogWarning(
                "Cannot apply field visual because " +
                "the team is null.");

            return;
        }

        if (fieldSpriteRenderer == null)
        {
            Debug.LogWarning(
                "No field SpriteRenderer is assigned.");

            return;
        }

        if (team.fieldSprite == null)
        {
            Debug.LogWarning(
                $"{team.teamName} does not have a " +
                $"field sprite assigned.");

            return;
        }

        fieldSpriteRenderer.sprite =
            team.fieldSprite;

        foamMaterial.color = team.fieldFoamColour;
        rowsMaterial.color = team.fieldRowsColour;
        seatBackMaterial.color = team.fieldSeatBackColour;
        seatBottomMaterial.color = team.fieldSeatBottomColour;
        standsMaterial.color = team.fieldStandsColour;
        standsAltMaterial.color = team.fieldStandsAltColour;
    }
}