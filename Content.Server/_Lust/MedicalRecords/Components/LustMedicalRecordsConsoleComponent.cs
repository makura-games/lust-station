using Content.Server._Lust.MedicalRecords.Systems;
using Content.Shared.Access;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._Lust.MedicalRecords.Components;

/// <summary>
/// Состояние медицинской консоли: выбранная запись, режим биометрии и
/// настройки печати
/// </summary>
[RegisterComponent, Access(typeof(LustMedicalRecordsSystem))]
public sealed partial class LustMedicalRecordsConsoleComponent : Component
{
    /// <summary>
    /// Ключ выбранной записи сотрудника. <c>null</c> — ничего не выбрано
    /// (например, после снятия выбора или если листинг пуст).
    /// Источник истины для <c>LustMedicalRecordsUiState.SelectedKey</c>.
    /// </summary>
    [DataField]
    public uint? ActiveKey;

    /// <summary>
    /// Разрешено ли редактирование ДНК и отпечатков на этой консоли.
    /// Переключается ID-картой с доступом <see cref="BiometricsAccess"/>
    /// или эмагом. Влияет на флаг <c>CanEditBiometrics</c> в стейте.
    /// </summary>
    [DataField]
    public bool BiometricsUnlocked;

    /// <summary>
    /// Тег доступа, открывающий редактирование биометрии.
    /// Настраивается на прототипе консоли, по умолчанию — <c>Captain</c>.
    /// </summary>
    [DataField]
    public ProtoId<AccessLevelPrototype> BiometricsAccess = "Captain";

    /// <summary>
    /// Время, когда консоль снова сможет печатать карту.
    /// </summary>
    public TimeSpan NextPrintTime;

    /// <summary>
    /// Задержка между печатями одной консоли.
    /// </summary>
    [DataField]
    public TimeSpan PrintCooldown = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Прототип бумаги, которая появляется при печати карты.
    /// </summary>
    [DataField]
    public EntProtoId Paper = "Paper";

    /// <summary>
    /// Звук при неудачном действии (недостаточно прав, нет доступа, кулдаун).
    /// </summary>
    [DataField]
    public SoundSpecifier FailedSound = new SoundPathSpecifier("/Audio/Effects/Cargo/buzz_sigh.ogg");

    /// <summary>
    /// Звук при успешном действии.
    /// </summary>
    [DataField]
    public SoundSpecifier SuccessfulSound = new SoundPathSpecifier("/Audio/Effects/Cargo/ping.ogg");

    /// <summary>
    /// Звук при вставке ID-карты.
    /// </summary>
    [DataField]
    public SoundSpecifier CardInsertSound = new SoundPathSpecifier("/Audio/Weapons/Guns/MagIn/batrifle_magin.ogg");

    /// <summary>
    /// Звук при печати карты.
    /// </summary>
    [DataField]
    public SoundSpecifier SoundPrint = new SoundPathSpecifier("/Audio/Machines/short_print_and_rip.ogg");
}
