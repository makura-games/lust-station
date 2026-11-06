using Robust.Shared.Serialization;

// Lust edit - сетевое сообщение расширяет Sunrise-панель из папки форка.
#pragma warning disable IDE0130
namespace Content.Shared._Sunrise.InteractionsPanel.Data.UI;

/// <summary>
/// Requests changing the owner's automatic interaction-progress decay preference.
/// </summary>
/// <param name="enabled">Whether automatic interaction-progress decay should remain enabled for the owner.</param>
[Serializable, NetSerializable]
public sealed class SetLoveDecayEnabledMessage(bool enabled) : BoundUserInterfaceMessage
{
    /// <summary>
    /// Whether automatic interaction-progress decay should remain enabled for the owner.
    /// </summary>
    public bool Enabled { get; } = enabled;
}
