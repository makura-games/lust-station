// Lust edit
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._Lust.Administration;

/// <summary>
/// Lust — компонент жертвы кары «Кабанизатор».
/// Свинья крутится на костре, периодически хрюкает и пукает, и не может быть отстегнута со столба.
/// </summary>
[RegisterComponent]
public sealed partial class KabanizatorVictimComponent : Component
{
    /// <summary>
    /// Скорость вращения свиньи в градусах в секунду.
    /// </summary>
    [DataField]
    public float SpinSpeed = 360f;

    /// <summary>
    /// Интервал между хрюканьями.
    /// </summary>
    [DataField]
    public TimeSpan OinkInterval = TimeSpan.FromSeconds(2.8f);

    public TimeSpan NextOinkTime = TimeSpan.Zero;

    /// <summary>
    /// Интервал между пуками.
    /// </summary>
    [DataField]
    public TimeSpan FartInterval = TimeSpan.FromSeconds(3.8f);

    public TimeSpan NextFartTime = TimeSpan.Zero;
}
