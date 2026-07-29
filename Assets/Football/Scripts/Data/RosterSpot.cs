
using UnityEngine;

[CreateAssetMenu(fileName = "New Roster Spot", menuName = "Football/Roster Spot")]
public class RosterSpot : ScriptableObject
{
    public string playerName;
    public RosterPosition playerPosition;
}
