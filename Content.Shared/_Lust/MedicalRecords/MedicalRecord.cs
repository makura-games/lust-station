using Content.Shared.StationRecords;
using Robust.Shared.Serialization;

namespace Content.Shared._Lust.MedicalRecords;

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial record MedicalRecord : StationRecord
{
    [DataField]
    public string Notes = string.Empty;
}
