using Content.Shared.Preferences;
using Robust.Shared.Enums;
using Robust.Shared.Serialization;

namespace Content.Shared._Lust.MedicalRecords;

/// <summary>
/// Ключ интерфейса медицинских записей.
/// </summary>
[Serializable, NetSerializable]
public enum LustMedicalRecordsUiKey : byte
{
    Key
}

/// <summary>
/// Данные медицинской консоли, которые сервер отправляет клиенту.
/// Содержит только листинг и выбор: детали записи едут отдельным
/// направленным сообщением только проверенному актору.
/// </summary>
[Serializable, NetSerializable]
public sealed class LustMedicalRecordsUiState : BoundUserInterfaceState
{
    public readonly Dictionary<uint, string>? RecordListing;
    public readonly uint? SelectedKey;
    public readonly bool CanEditBiometrics;


    /// <summary>
    /// Создаёт пустое состояние, например, когда у станции нет записей.
    /// </summary>
    public LustMedicalRecordsUiState()
    {
        RecordListing = null;
        SelectedKey = null;
        CanEditBiometrics = false;
    }

    /// <summary>
    /// Создаёт состояние со списком и текущим выбором.
    /// </summary>
    public LustMedicalRecordsUiState(Dictionary<uint, string> recordListing, uint? selectedKey)
    {
        RecordListing = recordListing;
        SelectedKey = selectedKey;
        CanEditBiometrics = false;
    }

    /// <summary>
    /// Создаёт состояние со списком, выбором и флагом доступа к биометрии.
    /// </summary>
    public LustMedicalRecordsUiState(
        Dictionary<uint, string> recordListing,
        uint? selectedKey,
        bool canEditBiometrics)
    {
        RecordListing = recordListing;
        SelectedKey = selectedKey;
        CanEditBiometrics = canEditBiometrics;
    }
}

/// <summary>
/// Детали выбранной медицинской карты. Отправляется направленным сообщением
/// только актору, прошедшему проверку доступа, а не всем смотрящим.
/// </summary>
[Serializable, NetSerializable]
public sealed class LustMedicalRecordsRecordDetailsMessage : BoundUserInterfaceMessage
{
    public readonly uint SelectedKey;

    public readonly string? Name;
    public readonly int? Age;
    public readonly Gender? Gender;
    public readonly string? Species;
    public readonly string? JobTitle;
    public readonly string? Fingerprint;
    public readonly string? Dna;

    public readonly string? Notes;
    public readonly bool CanEditBiometrics;
    public readonly HumanoidCharacterProfile? HumanoidProfile;

    public LustMedicalRecordsRecordDetailsMessage(
        uint selectedKey,
        string? name,
        int? age,
        Gender? gender,
        string? species,
        string? jobTitle,
        string? fingerprint,
        string? dna,
        string? notes,
        bool canEditBiometrics,
        HumanoidCharacterProfile? humanoidProfile)
    {
        SelectedKey = selectedKey;
        Name = name;
        Age = age;
        Gender = gender;
        Species = species;
        JobTitle = jobTitle;
        Fingerprint = fingerprint;
        Dna = dna;
        Notes = notes;
        CanEditBiometrics = canEditBiometrics;
        HumanoidProfile = humanoidProfile;
    }
}

/// <summary>
/// Клиент просит сервер выбрать запись сотрудника.
/// </summary>
[Serializable, NetSerializable]
public sealed class LustMedicalRecordsSelectRecordMessage : BoundUserInterfaceMessage
{
    public readonly uint? SelectedKey;

    public LustMedicalRecordsSelectRecordMessage(uint? selectedKey)
    {
        SelectedKey = selectedKey;
    }
}

/// <summary>
/// Сохраняет редактируемые поля выбранной медицинской карты.
/// </summary>
[Serializable, NetSerializable]
public sealed class LustMedicalRecordsSaveMessage : BoundUserInterfaceMessage
{
    public readonly uint RecordId;
    public readonly Gender Gender;
    public readonly string Species;
    public readonly string PhysiologicalTraits;
    public readonly string PsychologicalTraits;
    public readonly string Notes;
    public readonly string Fingerprint;
    public readonly string DNA;

    public LustMedicalRecordsSaveMessage(
        uint recordId,
        Gender gender,
        string species,
        string physiologicalTraits,
        string psychologicalTraits,
        string notes,
        string fingerprint,
        string dna)
    {
        RecordId = recordId;
        Gender = gender;
        Species = species;
        PhysiologicalTraits = physiologicalTraits;
        PsychologicalTraits = psychologicalTraits;
        Notes = notes;
        Fingerprint = fingerprint;
        DNA = dna;
    }
}

/// <summary>
/// Просит сервер распечатать медицинскую карту выбранного сотрудника.
/// </summary>
[Serializable, NetSerializable]
public sealed class LustMedicalRecordsPrintMessage(uint recordId) : BoundUserInterfaceMessage
{
    public readonly uint RecordId = recordId;
}
