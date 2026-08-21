namespace Content.Shared._Lust.Smell.Components;

/// <summary>
/// Маркер носителя, способного нюхать entity.
/// Только entity с этим компонентом получают верб «понюхать» и могут
/// улавливать запахи других существ.
/// </summary>
[RegisterComponent]
public sealed partial class SmellComponent : Component
{
}
