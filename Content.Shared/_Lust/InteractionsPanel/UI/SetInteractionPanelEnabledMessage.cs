using Robust.Shared.Serialization;

// Lust edit - сетевое сообщение расширяет Sunrise-панель из папки форка.
#pragma warning disable IDE0130
namespace Content.Shared._Sunrise.InteractionsPanel.Data.UI;

/// <summary>
/// Requests changing the availability of the owner's interaction panel.
/// </summary>
/// <param name="enabled">Whether the owner's interaction panel should be available.</param>
[Serializable, NetSerializable]
public sealed class SetInteractionPanelEnabledMessage(bool enabled) : BoundUserInterfaceMessage
{
    /// <summary>
    /// Whether the owner's interaction panel should be available.
    /// </summary>
    public bool Enabled { get; } = enabled;
}
