using Content.Shared.Atmos;

namespace Content.Server._Sunrise.Smoking;

/// <summary>
/// Компонент летящего колечка дыма или пара, реагирующий на столкновения и распад.
/// </summary>
[RegisterComponent]
public sealed partial class SmokeRingComponent : Component
{
    /// <summary>
    /// Флаг распада колечка.
    /// </summary>
    public bool Dissipated = false;

    /// <summary>
    /// Персонаж, выпустивший колечко.
    /// </summary>
    public EntityUid? Shooter;

    /// <summary>
    /// Стул, кресло или мебель, на которой сидит персонаж при выстреле.
    /// </summary>
    public EntityUid? IgnoredSeat;

    /// <summary>
    /// Время создания колечка.
    /// </summary>
    public TimeSpan SpawnTime = TimeSpan.Zero;

    /// <summary>
    /// Атмосферный газ, выделяемый при распаде колечка (водяной пар или табачный дым).
    /// </summary>
    [DataField]
    public Gas GasType = Gas.WaterVapor;
}
