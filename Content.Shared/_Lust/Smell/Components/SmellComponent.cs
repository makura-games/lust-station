namespace Content.Shared._Lust.Smell.Components;

/// <summary>
/// Маркер носителя, способного нюхать entity (вульпканины, таяраны).
/// Только entity с этим компонентом получают верб «понюхать» и могут
/// улавливать запахи других существ. Сам по себе компонент не задаёт
/// собственный запах — для этого есть ScentComponent.
/// </summary>
[RegisterComponent]
public sealed partial class SmellComponent : Component
{
}
