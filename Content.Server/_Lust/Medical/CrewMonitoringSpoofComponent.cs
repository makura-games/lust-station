using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._Lust.Medical;

public enum CrewMonitoringFakeState : byte
{
    Critical,
    Dead,
    Poor,
}

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class CrewMonitoringSpoofComponent : Component
{
    /// <summary>
    /// Сущность игрового правила, назначившего текущую подмену.
    /// Используется при завершении события, чтобы не удалить подмену, назначенную другим правилом.
    /// </summary>
    public EntityUid Rule;

    /// <summary>
    /// Ложное состояние здоровья, выбранное для персонажа.
    /// </summary>

    public CrewMonitoringFakeState FakeState;

    /// <summary>
    /// Момент окончания подмены по игровому времени.
    /// После его наступления передаются настоящие показания здоровья.
    /// </summary>

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan EndTime;
}