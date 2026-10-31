using Robust.Shared.GameStates;

namespace Content.Shared._Lust.Anomaly.Components;

/// <summary>
/// Компонент для предметов экипировки (наушники, каски, шлемы скафандров),
/// обеспечивающий защиту владельца от акустических эффектов звуковой аномалии.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AcousticProtectionComponent : Component
{
}
