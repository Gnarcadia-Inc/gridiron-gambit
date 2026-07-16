public static class FootballUnits
{
    public const float FeetPerYard = 3f;

    /*
     * Keep this at 1 if one Unity world unit equals one yard.
     *
     * Change it later if your field model uses a different scale.
     */
    public const float UnityUnitsPerYard = 1f;

    public static float YardsToUnits(float yards)
    {
        return yards * UnityUnitsPerYard;
    }

    public static float FeetToUnits(float feet)
    {
        return feet / FeetPerYard * UnityUnitsPerYard;
    }
}