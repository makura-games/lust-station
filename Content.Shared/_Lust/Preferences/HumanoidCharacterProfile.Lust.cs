using Content.Shared.Humanoid;
using Robust.Shared.Serialization;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Shared.Preferences;

public sealed partial class HumanoidCharacterProfile
{
    [DataField]
    public Erp Erp { get; set; } = Erp.Ask;

    [DataField]
    public Virginity Virginity { get; set; } = Virginity.No;

    [DataField]
    public Virginity AnalVirginity { get; set; } = Virginity.Yes;

    public HumanoidCharacterProfile WithErp(Erp erp)
    {
        return new(this) { Erp = erp };
    }

    public HumanoidCharacterProfile WithVirginity(Virginity virginity)
    {
        return new(this) { Virginity = virginity };
    }

    public HumanoidCharacterProfile WithAnalVirginity(Virginity analVirginity)
    {
        return new(this) { AnalVirginity = analVirginity };
    }

    private void CopyLustProfile(HumanoidCharacterProfile other)
    {
        Erp = other.Erp;
        Virginity = other.Virginity;
        AnalVirginity = other.AnalVirginity;
    }

    private bool LustProfileEquals(HumanoidCharacterProfile other)
    {
        return Erp == other.Erp &&
               Virginity == other.Virginity &&
               AnalVirginity == other.AnalVirginity;
    }

    private void AddLustHashCode(ref HashCode hashCode)
    {
        hashCode.Add((int) Erp);
        hashCode.Add((int) Virginity);
        hashCode.Add((int) AnalVirginity);
    }
}
