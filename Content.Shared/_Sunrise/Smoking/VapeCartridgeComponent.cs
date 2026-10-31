using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Smoking;

/// <summary>
/// Сменный картридж для вейпа с жидкостью.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class VapeCartridgeComponent : Component
{
    /// <summary>
    /// Имя контейнера растворов картриджа.
    /// </summary>
    [DataField]
    public string Solution = "smokable";

    /// <summary>
    /// Текущий преобладающий вкус жидкости для визуализатора.
    /// </summary>
    public string CurrentFlavor = "empty";

    /// <summary>
    /// Маппинг реагентов на визуальные вкусы картриджа.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<ReagentPrototype>, string> FlavorVisuals = new()
    {
        { "JuiceLime", "mint" },
        { "JuiceBerry", "berry" },
        { "Coffee", "tobacco" },
        { "JuiceBanana", "banana" },
    };
}
