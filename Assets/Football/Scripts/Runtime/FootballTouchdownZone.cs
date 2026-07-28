using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class FootballTouchdownZone : MonoBehaviour
{
    [SerializeField]
    private FootballPlaySequenceController sequenceController;

    [Tooltip(
        "The player's center must enter the trigger before " +
        "the touchdown is registered.")]
    [SerializeField]
    private bool requirePlayerCenterInside = true;

    private BoxCollider touchdownCollider;

    private void Awake()
    {
        touchdownCollider =
            GetComponent<BoxCollider>();

        touchdownCollider.isTrigger = true;
    }

    private void OnTriggerEnter(
        Collider other)
    {
        TryRegisterTouchdown(other);
    }

    private void OnTriggerStay(
        Collider other)
    {
        TryRegisterTouchdown(other);
    }

    private void TryRegisterTouchdown(
        Collider other)
    {
        if (sequenceController == null ||
            other == null)
        {
            return;
        }

        FootballRouteRunner runner =
            other.GetComponentInParent<
                FootballRouteRunner>();

        if (runner == null ||
            !runner.HasBall)
        {
            return;
        }

        Debug.LogError("WOK");

        sequenceController.RegisterTouchdown(
            runner);
    }

    public bool ContainsBallCarrier(FootballRouteRunner runner)
    {
        return runner != null &&
               runner.HasBall;
    }
}