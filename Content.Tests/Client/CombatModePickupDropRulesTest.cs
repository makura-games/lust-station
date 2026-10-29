using Content.Client._Sunrise.CombatMode;
using NUnit.Framework;

namespace Content.Tests.Client;

[TestFixture]
public sealed class CombatModePickupDropRulesTest
{
    [TestCase(true, true, true, true)]
    [TestCase(false, true, true, false)]
    [TestCase(true, false, true, false)]
    [TestCase(true, true, false, false)]
    [TestCase(false, false, false, false)]
    public void OnlyBlocksWhenPreferenceCombatModeAndLocalPlayerAreAllActive(
        bool preferenceEnabled,
        bool combatModeEnabled,
        bool localPlayer,
        bool expected)
    {
        Assert.That(
            CombatModePickupDropRules.ShouldBlock(preferenceEnabled, combatModeEnabled, localPlayer),
            Is.EqualTo(expected));
    }
}
