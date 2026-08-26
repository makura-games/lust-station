using Content.Shared.Humanoid;

namespace Content.Shared._Sunrise.InteractionsPanel.Data.Conditions;

public enum GenitalSlot : byte
{
    Penis,
    Vagina,
    Anus,
    Mouth,
    Boobs
}

public static class GenitalsHelper
{
    public static List<GenitalSlot> GetGenitals(Sex sex)
    {
        var genitals = new List<GenitalSlot>
        {
            GenitalSlot.Mouth,
            GenitalSlot.Anus,
        };

        switch (sex)
        {
            case Sex.Male:
                genitals.Add(GenitalSlot.Penis);
                break;
            case Sex.Female:
                genitals.Add(GenitalSlot.Vagina);
                genitals.Add(GenitalSlot.Boobs);
                break;
            case Sex.Futanari:
                genitals.Add(GenitalSlot.Boobs);
                genitals.Add(GenitalSlot.Penis);
                //genitals.Add(GenitalSlot.Vagina);  // Lust edit // :: Сомнения
                break;
            case Sex.Unsexed:
                break;
        }

        return genitals;
    }
}
