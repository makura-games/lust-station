using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Smoking;

/// <summary>
/// Компонент для активной затяжки тлеющими сигаретами, сигарами и трубками.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class SmokablePuffComponent : Component
{
    /// <summary>
    /// Время последней затяжки.
    /// </summary>
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan LastPuffTime = TimeSpan.Zero;

    /// <summary>
    /// Минимальный интервал между затяжками.
    /// </summary>
    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(1.5);

    /// <summary>
    /// Объем раствора, сгораемый/вдыхаемый за одну затяжку.
    /// </summary>
    [DataField]
    public float PuffVolume = 0.5f;

    /// <summary>
    /// Длительность затяжки при курении себя.
    /// </summary>
    [DataField]
    public float PuffDelay = 1.2f;

    /// <summary>
    /// Длительность затяжки при принудительном курении другого персонажа.
    /// </summary>
    [DataField]
    public float ForcedPuffDelay = 2.5f;

    /// <summary>
    /// Звук глубокой затяжки тлеющим табаком.
    /// </summary>
    [DataField]
    public SoundSpecifier? PuffSound = new SoundPathSpecifier("/Audio/_Sunrise/Effects/cigarette_puff.ogg");

    /// <summary>
    /// Звук выдоха табачного дыма.
    /// </summary>
    [DataField]
    public SoundSpecifier? ExhaleSound = new SoundPathSpecifier("/Audio/_Sunrise/Effects/cigarette_exhale.ogg");

    /// <summary>
    /// Прототип действия запуска колечка дыма.
    /// </summary>
    [DataField]
    public EntProtoId SmokeRingAction = "ActionBlowSmokeRing";

    /// <summary>
    /// Сущность действия выдувания колечка.
    /// </summary>
    public EntityUid? SmokeRingActionEntity;
}
