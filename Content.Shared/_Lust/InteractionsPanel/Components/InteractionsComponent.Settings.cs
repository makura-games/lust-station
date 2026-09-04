using Robust.Shared.GameStates;

// Lust edit - настройки Sunrise-панели хранятся в папке форка.
#pragma warning disable IDE0130
namespace Content.Shared._Sunrise.InteractionsPanel.Data.Components;

public sealed partial class InteractionsComponent
{
    /// <summary>
    /// Whether this character allows interaction progress to decay over time.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public bool LoveDecayEnabled = true;
}
