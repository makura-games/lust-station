using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared._Lust.Thermite.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class DissolvableComponent : Component
{
    /// <summary>
    /// Урон от горящего термита.
    /// </summary>
    [DataField]
    public DamageSpecifier Damage = new();

    /// <summary>
    /// Максимальное количество накопленных стаков термита.
    /// </summary>
    [DataField]
    public float MaximumStacks = 10f;

    /// <summary>
    /// Минимальное количество стаков термита.
    /// </summary>
    [DataField]
    public float MinimumStacks;

    /// <summary>
    /// Изменение количества стаков за один шаг затухания.
    /// </summary>
    [DataField]
    public float StackFade = -0.1f;

    /// <summary>
    /// Текущее количество стаков термита на конструкции.
    /// </summary>
    [AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public float Stacks;

    /// <summary>
    /// Указывает, горит ли термит на конструкции.
    /// </summary>
    [AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public bool IsBurning;

    /// <summary>
    /// Время следующего шага обработки горения.
    /// </summary>
    [NonSerialized]
    public TimeSpan NextUpdate;

    /// <summary>
    /// Сущность визуального эффекта огня.
    /// </summary>
    [NonSerialized]
    public EntityUid? FireEffect;
}
