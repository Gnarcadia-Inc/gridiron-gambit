using UnityEngine;

public class FootballFieldVisual : MonoBehaviour
{
    [SerializeField]
    private SpriteRenderer fieldSpriteRenderer;

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
    }
}