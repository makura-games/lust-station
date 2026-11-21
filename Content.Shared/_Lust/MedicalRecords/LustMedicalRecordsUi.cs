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
/// Упрощённая запись для листинга медицинской консоли.
/// </summary>
[Serializable, NetSerializable]
public sealed record LustMedicalRecordListing(
    uint Id,
    string Name,
    string? DNA,
    string? Fingerprints,
    string? Species,
    string? Gender,
    string? JobTitle);

/// <summary>
/// Данные медицинской консоли.
/// </summary>
[Serializable, NetSerializable]
public sealed class LustMedicalRecordsUiState : BoundUserInterfaceState
{
    public readonly List<LustMedicalRecordListing>? RecordListing;
    public readonly uint? SelectedKey;
    public readonly bool CanEditBiometrics;

    public readonly string? Name;
    public readonly int? Age;
    public readonly Gender? Gender;
    public readonly string? Species;
    public readonly string? JobTitle;
    public readonly string? Fingerprint;
    public readonly string? Dna;
    public readonly string? Notes;
    public readonly HumanoidCharacterProfile? HumanoidProfile;

    public LustMedicalRecordsUiState()
    {
        RecordListing = null;
        SelectedKey = null;
        CanEditBiometrics = false;
    }

    public LustMedicalRecordsUiState(
        List<LustMedicalRecordListing> recordListing,
        uint? selectedKey,
        bool canEditBiometrics,
        string? name = null,
        int? age = null,
        Gender? gender = null,
        string? species = null,
        string? jobTitle = null,
        string? fingerprint = null,
        string? dna = null,
        string? notes = null,
        HumanoidCharacterProfile? humanoidProfile = null)
    {
        RecordListing = recordListing;
        SelectedKey = selectedKey;
        CanEditBiometrics = canEditBiometrics;
        Name = name;
        Age = age;
        Gender = gender;
        Species = species;
        JobTitle = jobTitle;
        Fingerprint = fingerprint;
        Dna = dna;
        Notes = notes;
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
