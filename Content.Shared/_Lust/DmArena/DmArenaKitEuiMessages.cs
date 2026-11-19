using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._Lust.DmArena;

[Serializable, NetSerializable]
public sealed class DmArenaKitEuiState : EuiStateBase
{
    public List<DmArenaStoreOption> Items { get; }
    public int PlayerCount { get; }
    public int Budget { get; }
    public List<int>? Preset { get; }
    public List<int>? Last { get; }
    public string BaseGear { get; }
    public List<DmArenaStoreSetOption> Sets { get; }

    public DmArenaKitEuiState(List<DmArenaStoreOption> items, List<DmArenaStoreSetOption> sets, int playerCount, int budget, List<int>? preset, List<int>? last, string baseGear)
    {
        Sets = sets;
        BaseGear = baseGear;
        Items = items;
        PlayerCount = playerCount;
        Budget = budget;
        Preset = preset;
        Last = last;
    }
}

[Serializable, NetSerializable]
public sealed class DmArenaStoreOption
{
    public int Index { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public string SpritePrototype { get; set; } = string.Empty;
    public string GearPrototype { get; set; } = string.Empty;
    public int Cost { get; set; }
    public bool EquipFirst { get; set; }
    public bool InHand { get; set; }

    public List<string> Slots { get; set; } = new();

    public int Hands { get; set; }
    public int MaxCount { get; set; } = 1;
    public string AmmoType { get; set; } = string.Empty;
    public string AmmoFor { get; set; } = string.Empty;
}

[Serializable, NetSerializable]
public sealed class DmArenaStoreSetOption
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<int> Items { get; set; } = new();
    public int Cost { get; set; }
}

[Serializable, NetSerializable]
public sealed class DmArenaKitConfirmMessage : EuiMessageBase
{
    public List<int> Items { get; }

    public DmArenaKitConfirmMessage(List<int> items)
    {
        Items = items;
    }
}

[Serializable, NetSerializable]
public sealed class DmArenaKitSaveMessage : EuiMessageBase
{
    public List<int> Items { get; }

    public DmArenaKitSaveMessage(List<int> items)
    {
        Items = items;
    }
}
