namespace Content.Client._Sunrise.CombatMode;

public static class CombatModePickupDropRules
{
    public static bool ShouldBlock(bool preferenceEnabled, bool combatModeEnabled, bool localPlayer)
    {
        return preferenceEnabled && combatModeEnabled && localPlayer;
    }
}
