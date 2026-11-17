using System.Linq;
using Content.Server.Hands.Systems;
using Content.Server._Lust.MedicalRecords.Components;
using Content.Server.Popups;
using Content.Server.Power.EntitySystems;
using Content.Server.Station.Systems;
using Content.Shared.StationRecords.Events;
using Content.Shared._Lust.Preferences;
using Content.Shared._Lust.MedicalRecords;
using Content.Shared._Sunrise.Helpers;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Emag.Systems;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Interaction;
using Content.Shared.Paper;
using Content.Shared.Preferences;
using Content.Shared.StationRecords;
using Content.Shared.StationRecords.Components;
using Content.Shared.StationRecords.Systems;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._Lust.MedicalRecords.Systems;

/// <summary>
/// Серверная логика медицинской консоли: выбор записи, редактирование
/// физиологических/психологических черт, заметок и биометрии, печать бумаги.
/// </summary>
public sealed partial class LustMedicalRecordsSystem : EntitySystem
{
    [Dependency] private StationRecordsSystem _records = default!;
    [Dependency] private StationSystem _station = default!;
    [Dependency] private AccessReaderSystem _access = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private HandsSystem _hands = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private PaperSystem _paper = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private IGameTiming _timing = default!;

    /// <summary>
    /// Подписывает консоль на BUI-сообщения. LocalEvent-обработчики
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();

        Subs.BuiEvents<LustMedicalRecordsConsoleComponent>(LustMedicalRecordsUiKey.Key,
            subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnOpened);
            subs.Event<LustMedicalRecordsSelectRecordMessage>(OnSelectRecord);
            subs.Event<LustMedicalRecordsSaveMessage>(OnSaveRecord);
            subs.Event<LustMedicalRecordsPrintMessage>(OnPrintRecord);
        });

    }

    /// <summary>
    /// Создаёт пустую медкарту при появлении сотрудника в станционном учёте.
    /// Пустая запись потом заполняется через Save.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnGeneralRecordCreated(ref GeneralRecordCreatedEvent ev)
    {
        _records.AddRecordEntry(ev.Key, new MedicalRecord());
        _records.Synchronize(ev.Key);
    }

    /// <summary>
    /// Обновляет стейт консоли, когда любая из записей станции меняется.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnRecordModified(
        Entity<LustMedicalRecordsConsoleComponent> ent,
        ref RecordModifiedEvent args)
    {
        UpdateUserInterface(ent.Owner, ent.Comp, ent.Comp.BiometricsUnlocked);
    }

    /// <summary>
    /// Переключает режим редактирования биометрии по ID-карте с доступом
    /// <see cref="LustMedicalRecordsConsoleComponent.BiometricsAccess"/>.
    /// После переключения пересылает свежие детали актору.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnCardUsed(
        Entity<LustMedicalRecordsConsoleComponent> ent,
        ref AfterInteractUsingEvent args)
    {
        if (args.Handled
            || !args.CanReach
            || !this.IsPowered(ent.Owner, EntityManager)
            || !TryComp<IdCardComponent>(args.Used, out _))
        {
            return;
        }

        if (!_access.FindAccessTags(args.Used).Contains(ent.Comp.BiometricsAccess))
        {
            // Карта не подходит для биометрии — фидбек, без переключения режима
            _audio.PlayPvs(ent.Comp.FailedSound, ent.Owner);
            _popup.PopupEntity(
                Loc.GetString("lust-medical-records-access-denied"),
                ent.Owner,
                args.User);
            args.Handled = true;
            return;
        }

        ent.Comp.BiometricsUnlocked = !ent.Comp.BiometricsUnlocked;
        _audio.PlayPvs(ent.Comp.CardInsertSound, ent.Owner);
        var message = ent.Comp.BiometricsUnlocked
            ? "lust-medical-records-biometrics-unlocked"
            : "lust-medical-records-biometrics-locked";

        _popup.PopupEntity(Loc.GetString(message), ent.Owner, args.User);

        UpdateUserInterface(ent.Owner, ent.Comp, ent.Comp.BiometricsUnlocked);
        SendRecordDetails(ent.Owner, ent.Comp, args.User);
        args.Handled = true;
    }

    /// <summary>
    /// Эмаг разблокирует биометрию.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnEmagged(
        Entity<LustMedicalRecordsConsoleComponent> ent,
        ref GotEmaggedEvent args)
    {
        if (args.Handled || ent.Comp.BiometricsUnlocked)
            return;

        ent.Comp.BiometricsUnlocked = true;
        UpdateUserInterface(ent.Owner, ent.Comp, ent.Comp.BiometricsUnlocked);
        args.Handled = true;
    }

    /// <summary>
    /// Обрабатывает выбор/снятие выбора записи, издаёт звук отказа и поап при попытки выбрать запись без доступа.
    /// </summary>
    private void OnSelectRecord(Entity<LustMedicalRecordsConsoleComponent> ent, ref LustMedicalRecordsSelectRecordMessage msg)
    {
        if (msg.SelectedKey == null)
        {
            if (ent.Comp.ActiveKey != null)
            {
                ent.Comp.ActiveKey = null;
                UpdateUserInterface(ent.Owner, ent.Comp, ent.Comp.BiometricsUnlocked);
            }
            return;
        }

        if (!CheckAccessOrDeny(ent, msg.Actor))
            return;

        if (_station.GetOwningStation(ent.Owner) is not { } station)
            return;

        if (msg.SelectedKey is { } id
            && !_records.TryGetRecord<GeneralStationRecord>(
                new StationRecordKey(id, station),
                out _))
        {
            return;
        }

        // Повторный выбор той же записи не меняет листинг, детали шлём только спросившему.
        if (ent.Comp.ActiveKey == msg.SelectedKey)
        {
            SendRecordDetails(ent.Owner, ent.Comp, msg.Actor);
            return;
        }

        ent.Comp.ActiveKey = msg.SelectedKey;
        UpdateUserInterface(ent.Owner, ent.Comp, ent.Comp.BiometricsUnlocked);
        SendRecordDetails(ent.Owner, ent.Comp, msg.Actor);
    }

    /// <summary>
    /// При открытии консоли проверяет доступ и отправляет стейт + детали актору.
    /// </summary>
    private void OnOpened(Entity<LustMedicalRecordsConsoleComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUserInterface(ent.Owner, ent.Comp, ent.Comp.BiometricsUnlocked);

        if (_access.IsAllowed(args.Actor, ent.Owner))
            SendRecordDetails(ent.Owner, ent.Comp, args.Actor);
    }

    /// <summary>
    /// Сохраняет редактируемые поля медкарты.
    /// обновляется только при разблокированном режиме.
    /// </summary>
    private void OnSaveRecord(Entity<LustMedicalRecordsConsoleComponent> ent, ref LustMedicalRecordsSaveMessage msg)
    {
        if (!CheckAccessOrDeny(ent, msg.Actor))
            return;

        if (ent.Comp.ActiveKey != msg.RecordId
            || !Enum.IsDefined(typeof(Gender), msg.Gender)
            || _station.GetOwningStation(ent.Owner) is not { } station
            || !_prototype.TryIndex<SpeciesPrototype>(msg.Species, out _))
            return;

        var key = new StationRecordKey(msg.RecordId, station);
        if (!_records.TryGetRecord<GeneralStationRecord>(key, out var record))
            return;

        _records.TryGetRecord<MedicalRecord>(key, out var medicalRecord);
        medicalRecord ??= new MedicalRecord();

        var portfolio = new PortfolioProfile(record.HumanoidProfile?.Portfolio ?? new PortfolioProfile())
        {
            PhysiologicalTraits = msg.PhysiologicalTraits.SanitizeInput(512),
            PsychologicalTraits = msg.PsychologicalTraits.SanitizeInput(512),
        };

        var profile = (record.HumanoidProfile ?? new HumanoidCharacterProfile()).WithPortfolio(portfolio);
        var updatedRecord = GeneralStationRecord.SanitizeRecord(record with
        {
            Gender = msg.Gender,
            Species = msg.Species,
            HumanoidProfile = profile,
        },
        in _prototype);

        var canEditBiometrics = ent.Comp.BiometricsUnlocked;
        if (canEditBiometrics)
        {
            updatedRecord = GeneralStationRecord.SanitizeRecord(original: updatedRecord with
            {
                Fingerprint = SanitizeBiometricCode(msg.Fingerprint, 32),
                DNA = SanitizeBiometricCode(msg.DNA, 16),
            },
            in _prototype);
        }

        medicalRecord.Notes = msg.Notes.SanitizeInput(2048);
        _records.AddRecordEntry(key, updatedRecord);
        _records.AddRecordEntry(key, medicalRecord);
        _records.Synchronize(key);
        // Свежие детали — только сохранившему. Остальным смотрящим уйдёт листинг через RecordModified.
        SendRecordDetails(ent.Owner, ent.Comp, msg.Actor);
        _audio.PlayPvs(ent.Comp.SuccessfulSound, ent.Owner);
    }

    /// <summary>
    /// Печатает бумажную медкарту выбранной записи. Кулдаун между печатями —
    /// <see cref="LustMedicalRecordsConsoleComponent.PrintCooldown"/>.
    /// </summary>
    private void OnPrintRecord(Entity<LustMedicalRecordsConsoleComponent> ent, ref LustMedicalRecordsPrintMessage msg)
    {
        if (!CheckAccessOrDeny(ent, msg.Actor))
            return;

        if (ent.Comp.ActiveKey != msg.RecordId)
            return;

        if (_timing.CurTime < ent.Comp.NextPrintTime)
        {
            _popup.PopupEntity(Loc.GetString("forensic-scanner-printer-not-ready"), ent.Owner, msg.Actor);
            _audio.PlayPvs(ent.Comp.FailedSound, ent.Owner);
            return;
        }

        if (_station.GetOwningStation(ent.Owner) is not { } station)
            return;

        var key = new StationRecordKey(msg.RecordId, station);
        if (!_records.TryGetRecord<GeneralStationRecord>(key, out var record))
            return;

        _records.TryGetRecord<MedicalRecord>(key, out var medicalRecord);

        var printed = Spawn(ent.Comp.Paper, Transform(ent).Coordinates);
        _hands.PickupOrDrop(msg.Actor, printed, checkActionBlocker: false);

        if (!TryComp<PaperComponent>(printed, out var paper))
        {
            _audio.PlayPvs(ent.Comp.FailedSound, ent.Owner);
            return;
        }

        var unrecognized = Loc.GetString("printed-station-records-unrecognized");
        string Printable(string? value) => string.IsNullOrWhiteSpace(value)
            ? unrecognized
            : FormattedMessage.EscapeText(value);

        var speciesName = _prototype.TryIndex<SpeciesPrototype>(record.Species, out var species)
            ? Loc.GetString(species.Name)
            : unrecognized;
        var age = record.Age > 0
            ? record.Age.ToString()
            : unrecognized;
        var gender = Loc.GetString("station-records-gender", ("gender", record.Gender.ToString()));
        var portfolio = record.HumanoidProfile?.Portfolio;

        var content = Loc.GetString(
            "lust-medical-records-print-content",
            ("name", Printable(record.Name)),
            ("age", age),
            ("gender", gender),
            ("species", Printable(speciesName)),
            ("job", Printable(record.JobTitle)),
            ("fingerprint", Printable(record.Fingerprint)),
            ("dna", Printable(record.DNA)),
            ("closeRelatives", Printable(portfolio?.CloseRelatives)),
            ("emergencyContact", Printable(portfolio?.EmergencyContact)),
            ("physiologicalTraits", Printable(portfolio?.PhysiologicalTraits)),
            ("psychologicalTraits", Printable(portfolio?.PsychologicalTraits)),
            ("notes", Printable(medicalRecord?.Notes)));

        _metaData.SetEntityName(printed, Loc.GetString("lust-medical-records-print-name", ("name", record.Name)));
        _paper.SetContent((printed, paper), content);
        _audio.PlayPvs(ent.Comp.SoundPrint, ent);
        ent.Comp.NextPrintTime = _timing.CurTime + ent.Comp.PrintCooldown;
    }

    /// <summary>
    /// Формирует и отправляет общий стейт консоли: листинг + выбранный ключ +
    /// флаг доступа к биометрии. Если ключ больше не валиден (запись удалена),
    /// сбрасывается в <c>null</c> без автовыбора.
    /// </summary>
    private void UpdateUserInterface(EntityUid uid, LustMedicalRecordsConsoleComponent component, bool canEditBiometrics)
    {
        if (_station.GetOwningStation(uid) is not { } station
            || !TryComp<StationRecordsComponent>(station, out var stationRecords))
        {
            _ui.SetUiState(uid, LustMedicalRecordsUiKey.Key, new LustMedicalRecordsUiState());
            return;
        }

        var listingRecords = new List<LustMedicalRecordListing>();
        foreach (var (id, _) in _records.BuildListing((station, stationRecords), null))
        {
            var key = new StationRecordKey(id, station);
            if (_records.TryGetRecord<GeneralStationRecord>(key, out var general))
            {
                listingRecords.Add(new LustMedicalRecordListing(
                    id,
                    general.Name,
                    general.DNA,
                    general.Fingerprint,
                    general.Species,
                    general.Gender.ToString(),
                    general.JobTitle));
            }
        }

        if (listingRecords.Count == 0)
        {
            _ui.SetUiState(
                uid,
                LustMedicalRecordsUiKey.Key,
                new LustMedicalRecordsUiState(
                    listingRecords,
                    null,
                    canEditBiometrics));
            return;
        }

        if (component.ActiveKey is { } selectedKey && !listingRecords.Any(r => r.Id == selectedKey))
            component.ActiveKey = null;

        // В общий стейт детали не кладём: их смотрит только проверенный актор через SendRecordDetails.
        _ui.SetUiState(
            uid,
            LustMedicalRecordsUiKey.Key,
            new LustMedicalRecordsUiState(
                listingRecords,
                component.ActiveKey,
                canEditBiometrics));
    }

    /// <summary>
    /// Отправляет детали выбранной записи направленным сообщением одному актору.
    /// Вызывать только после проверки доступа вызывающей стороны.
    /// </summary>
    private void SendRecordDetails(EntityUid uid, LustMedicalRecordsConsoleComponent component, EntityUid actor)
    {
        if (_station.GetOwningStation(uid) is not { } station)
            return;

        if (component.ActiveKey is not { } selectedKey)
            return;

        var recordKey = new StationRecordKey(selectedKey, station);
        _records.TryGetRecord<GeneralStationRecord>(recordKey, out var generalRecord);
        _records.TryGetRecord<MedicalRecord>(recordKey, out var medicalRecord);

        _ui.ServerSendUiMessage(
            uid,
            LustMedicalRecordsUiKey.Key,
            new LustMedicalRecordsRecordDetailsMessage(
                selectedKey,
                generalRecord?.Name,
                generalRecord?.Age,
                generalRecord?.Gender,
                generalRecord?.Species,
                generalRecord?.JobTitle,
                generalRecord?.Fingerprint,
                generalRecord?.DNA,
                medicalRecord?.Notes,
                component.BiometricsUnlocked,
                generalRecord?.HumanoidProfile),
            actor);
    }

    /// <summary>
    /// Нормализует биометрический код: A–Z и 0–9, верхний регистр,
    /// обрезка до <paramref name="maxLength"/>.
    /// </summary>
    private static string SanitizeBiometricCode(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var result = new System.Text.StringBuilder(Math.Min(value.Length, maxLength));
        foreach (var character in value)
        {
            if (result.Length >= maxLength)
                break;

            if (character is >= 'a' and <= 'z')
                result.Append((char) (character - ('a' - 'A')));
            else if (character is >= 'A' and <= 'Z' or >= '0' and <= '9')
                result.Append(character);
        }

        return result.ToString();
    }

    /// <summary>
    /// Проверяет доступ актора к консоли. При отказе играет звук,
    /// показывает попап только актору и возвращает false.
    /// </summary>
    private bool CheckAccessOrDeny(
        Entity<LustMedicalRecordsConsoleComponent> ent,
        EntityUid actor)
    {
        if (_access.IsAllowed(actor, ent.Owner))
            return true;

        _audio.PlayPvs(ent.Comp.FailedSound, ent.Owner);
        _popup.PopupEntity(
            Loc.GetString("lust-medical-records-access-denied"),
            ent.Owner,
            actor);

        return false;
    }
}
