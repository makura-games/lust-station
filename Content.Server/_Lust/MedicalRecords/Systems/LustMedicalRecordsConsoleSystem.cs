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
/// Серверная логика медицинской консоли.
/// </summary>
public sealed partial class LustMedicalRecordsConsoleSystem : EntitySystem
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
    /// Подписывает консоль на BUI-сообщения
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
    /// После переключения свежий стейт уходит всем смотрящим консоль.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnCardUsed(
        Entity<LustMedicalRecordsConsoleComponent> ent,
        ref AfterInteractUsingEvent args)
    {
        if (args.Handled
            || !args.CanReach
            || !this.IsPowered(ent.Owner, EntityManager)
            || !HasComp<IdCardComponent>(args.Used))
        {
            return;
        }

        TryToggleBiometrics(ent, args.Used, args.User);
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
        if (args.Handled)
            return;

        args.Handled = TryUnlockBiometrics(ent);
    }

    /// <summary>
    /// Обрабатывает выбор/снятие выбора записи, издаёт звук отказа и поап при попытки выбрать запись без доступа.
    /// </summary>
    private void OnSelectRecord(Entity<LustMedicalRecordsConsoleComponent> ent, ref LustMedicalRecordsSelectRecordMessage msg)
    {
        TrySelectRecord(ent, msg.SelectedKey, msg.Actor);
    }

    /// <summary>
    /// При открытии консоли отправляет общий стейт (листинг + детали) всем смотрящим.
    /// </summary>
    private void OnOpened(Entity<LustMedicalRecordsConsoleComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUserInterface(ent.Owner, ent.Comp, ent.Comp.BiometricsUnlocked);
    }

    /// <summary>
    /// Сохраняет редактируемые поля медкарты.
    /// </summary>
    private void OnSaveRecord(Entity<LustMedicalRecordsConsoleComponent> ent, ref LustMedicalRecordsSaveMessage msg)
    {
        TrySaveRecord(ent, msg, msg.Actor);
    }

    /// <summary>
    /// Печатает бумажную медкарту выбранной записи. Кулдаун между печатями —
    /// <see cref="LustMedicalRecordsConsoleComponent.PrintCooldown"/>.
    /// </summary>
    private void OnPrintRecord(Entity<LustMedicalRecordsConsoleComponent> ent, ref LustMedicalRecordsPrintMessage msg)
    {
        TryPrintRecord(ent, msg.RecordId, msg.Actor);
    }

    /// <summary>
    /// Переключение режима биометрии.
    /// </summary>
    private bool TryToggleBiometrics(
        Entity<LustMedicalRecordsConsoleComponent> ent,
        EntityUid used,
        EntityUid user)
    {
        if (!CanToggleBiometrics(ent, used, user))
            return false;

        DoToggleBiometrics(ent, user);
        return true;
    }

    private bool CanToggleBiometrics(
        Entity<LustMedicalRecordsConsoleComponent> ent,
        EntityUid used,
        EntityUid user)
    {

        if (!_access.FindAccessTags(used).Contains(ent.Comp.BiometricsAccess))
        {
            _audio.PlayPvs(ent.Comp.FailedSound, ent.Owner);
            _popup.PopupEntity(
                Loc.GetString("lust-medical-records-access-denied"),
                ent.Owner,
                user);
            return false;
        }

        return true;
    }

    private void DoToggleBiometrics(Entity<LustMedicalRecordsConsoleComponent> ent, EntityUid user)
    {
        ent.Comp.BiometricsUnlocked = !ent.Comp.BiometricsUnlocked;
        _audio.PlayPvs(ent.Comp.CardInsertSound, ent.Owner);
        var message = ent.Comp.BiometricsUnlocked
            ? "lust-medical-records-biometrics-unlocked"
            : "lust-medical-records-biometrics-locked";
        _popup.PopupEntity(Loc.GetString(message), ent.Owner, user);
        UpdateUserInterface(ent.Owner, ent.Comp, ent.Comp.BiometricsUnlocked);
    }

    /// <summary>
    /// Разблокировка биометрии эмагом.
    /// </summary>
    private bool TryUnlockBiometrics(Entity<LustMedicalRecordsConsoleComponent> ent)
    {
        if (IsBiometricsUnlocked(ent))
            return false;

        DoUnlockBiometrics(ent);
        return true;
    }

    private bool IsBiometricsUnlocked(Entity<LustMedicalRecordsConsoleComponent> ent)
    {
        return ent.Comp.BiometricsUnlocked;
    }

    private void DoUnlockBiometrics(Entity<LustMedicalRecordsConsoleComponent> ent)
    {
        ent.Comp.BiometricsUnlocked = true;
        UpdateUserInterface(ent.Owner, ent.Comp, ent.Comp.BiometricsUnlocked);
    }

    /// <summary>
    /// Выбор или снятие выбора записи.
    /// </summary>
    private bool TrySelectRecord(
        Entity<LustMedicalRecordsConsoleComponent> ent,
        uint? selectedKey,
        EntityUid actor)
    {
        if (!CanSelectRecord(ent, selectedKey, actor))
            return false;

        DoSelectRecord(ent, selectedKey);
        return true;
    }

    private bool CanSelectRecord(
        Entity<LustMedicalRecordsConsoleComponent> ent,
        uint? selectedKey,
        EntityUid actor)
    {
        if (!CanAccess(ent, actor))
            return false;

        if (selectedKey is null)
            return ent.Comp.ActiveKey is not null;

        if (_station.GetOwningStation(ent.Owner) is not { } station)
            return false;

        return _records.TryGetRecord<GeneralStationRecord>(
            new StationRecordKey(selectedKey.Value, station),
            out _);
    }

    private void DoSelectRecord(Entity<LustMedicalRecordsConsoleComponent> ent, uint? selectedKey)
    {
        ent.Comp.ActiveKey = selectedKey;
        UpdateUserInterface(ent.Owner, ent.Comp, ent.Comp.BiometricsUnlocked);
    }

    /// <summary>
    /// Сохранение редактируемых полей медкарты.
    /// </summary>
    private bool TrySaveRecord(
        Entity<LustMedicalRecordsConsoleComponent> ent,
        LustMedicalRecordsSaveMessage msg,
        EntityUid actor)
    {
        var success = CanSaveRecord(ent, msg, actor) && DoSaveRecord(ent, msg);

        _ui.ServerSendUiMessage(
            ent.Owner,
            LustMedicalRecordsUiKey.Key,
            new LustMedicalRecordsSaveResultMessage(msg.RecordId, success),
            actor);

        return success;
    }

    private bool CanSaveRecord(
        Entity<LustMedicalRecordsConsoleComponent> ent,
        LustMedicalRecordsSaveMessage msg,
        EntityUid actor)
    {
        if (!CanAccess(ent, actor))
            return false;

        if (ent.Comp.ActiveKey != msg.RecordId
            || (msg.Gender is { } gender && !Enum.IsDefined(typeof(Gender), gender))
            || _station.GetOwningStation(ent.Owner) is not { } station
            || (msg.Species is { } species && !_prototype.TryIndex<SpeciesPrototype>(species, out _)))
        {
            return false;
        }

        return _records.TryGetRecord<GeneralStationRecord>(
            new StationRecordKey(msg.RecordId, station),
            out _);
    }

    private bool DoSaveRecord(Entity<LustMedicalRecordsConsoleComponent> ent, LustMedicalRecordsSaveMessage msg)
    {
        if (_station.GetOwningStation(ent.Owner) is not { } station)
            return false;

        var key = new StationRecordKey(msg.RecordId, station);
        if (!_records.TryGetRecord<GeneralStationRecord>(key, out var record))
            return false;

        var profile = record.HumanoidProfile;
        if (msg.PhysiologicalTraits is not null || msg.PsychologicalTraits is not null)
        {
            var portfolio = new PortfolioProfile(profile?.Portfolio);
            if (msg.PhysiologicalTraits is { } physiological)
                portfolio.PhysiologicalTraits = physiological.SanitizeInput(512);
            if (msg.PsychologicalTraits is { } psychological)
                portfolio.PsychologicalTraits = psychological.SanitizeInput(512);
            portfolio.EnsureValid();

            profile = (profile ?? new HumanoidCharacterProfile()).WithPortfolio(portfolio);
        }

        var fingerprint = record.Fingerprint;
        var dna = record.DNA;
        if (ent.Comp.BiometricsUnlocked)
        {
            if (msg.Fingerprint is { } newFingerprint)
                fingerprint = SanitizeBiometricCode(newFingerprint, 32);
            if (msg.DNA is { } newDna)
                dna = SanitizeBiometricCode(newDna, 16);
        }

        var updatedRecord = record with
        {
            Gender = msg.Gender ?? record.Gender,
            Species = msg.Species ?? record.Species,
            HumanoidProfile = profile,
            Fingerprint = fingerprint,
            DNA = dna,
        };

        _records.AddRecordEntry(key, updatedRecord);

        if (msg.Notes is { } notes)
        {
            _records.TryGetRecord<MedicalRecord>(key, out var medicalRecord);
            medicalRecord ??= new MedicalRecord();
            medicalRecord.Notes = notes.SanitizeInput(2048);
            _records.AddRecordEntry(key, medicalRecord);
        }

        // Synchronize поднимает RecordModifiedEvent в OnRecordModified а он сам шлёт свежий стейт.
        _records.Synchronize(key);
        _audio.PlayPvs(ent.Comp.SuccessfulSound, ent.Owner);
        return true;
    }

    /// <summary>
    /// Печать бумажной медкарты.
    /// </summary>
    private bool TryPrintRecord(
        Entity<LustMedicalRecordsConsoleComponent> ent,
        uint recordId,
        EntityUid actor)
    {
        if (!CanPrintRecord(ent, recordId, actor))
            return false;

        DoPrintRecord(ent, recordId, actor);
        return true;
    }

    private bool CanPrintRecord(
        Entity<LustMedicalRecordsConsoleComponent> ent,
        uint recordId,
        EntityUid actor)
    {
        if (!CanAccess(ent, actor))
            return false;

        if (ent.Comp.ActiveKey != recordId)
            return false;

        if (_timing.CurTime < ent.Comp.NextPrintTime)
        {
            _popup.PopupEntity(Loc.GetString("forensic-scanner-printer-not-ready"), ent.Owner, actor);
            _audio.PlayPvs(ent.Comp.FailedSound, ent.Owner);
            return false;
        }

        if (_station.GetOwningStation(ent.Owner) is not { } station)
            return false;

        return _records.TryGetRecord<GeneralStationRecord>(
            new StationRecordKey(recordId, station),
            out _);
    }

    private void DoPrintRecord(
        Entity<LustMedicalRecordsConsoleComponent> ent,
        uint recordId,
        EntityUid actor)
    {
        if (_station.GetOwningStation(ent.Owner) is not { } station)
            return;

        var key = new StationRecordKey(recordId, station);
        if (!_records.TryGetRecord<GeneralStationRecord>(key, out var record))
            return;

        _records.TryGetRecord<MedicalRecord>(key, out var medicalRecord);

        var printed = Spawn(ent.Comp.Paper, Transform(ent).Coordinates);
        _hands.PickupOrDrop(actor, printed, checkActionBlocker: false);

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
        var age = record.Age.ToString();

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
    /// флаг доступа к биометрии. Если ключ больше не валиден,
    /// сбрасывается в <c>null</c> не предлагая другую запись.
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

        if (component.ActiveKey is { } selectedKey && listingRecords.All(r => r.Id != selectedKey))
            component.ActiveKey = null;

        // Детали выбранной записи — в том же broadcast-стейте.
        string? name = null, jobTitle = null, species = null, fingerprint = null, dna = null, notes = null;
        int? age = null;
        Gender? gender = null;
        LustMedicalPortfolioView? portfolio = null;

        if (component.ActiveKey is { } selected)
        {
            var recordKey = new StationRecordKey(selected, station);
            if (_records.TryGetRecord<GeneralStationRecord>(recordKey, out var general))
            {
                name = general.Name;
                age = general.Age;
                gender = general.Gender;
                species = general.Species;
                jobTitle = general.JobTitle;
                fingerprint = general.Fingerprint;
                dna = general.DNA;

                // В стейт уходит только нужная консоли часть портфолио.
                portfolio = LustMedicalPortfolioView.FromProfile(general.HumanoidProfile);
            }

            if (_records.TryGetRecord<MedicalRecord>(recordKey, out var medical))
                notes = medical.Notes;
        }

        _ui.SetUiState(
            uid,
            LustMedicalRecordsUiKey.Key,
            new LustMedicalRecordsUiState(
                listingRecords,
                component.ActiveKey,
                canEditBiometrics,
                name: name,
                age: age,
                gender: gender,
                species: species,
                jobTitle: jobTitle,
                fingerprint: fingerprint,
                dna: dna,
                notes: notes,
                portfolio: portfolio));
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
    /// Чистая проверка доступа: состояние не меняет, при отказе
    /// показывает звук и попап только актору.
    /// </summary>
    private bool CanAccess(
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
