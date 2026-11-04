using Robust.Shared.Serialization;

namespace Content.Shared._Lust.DmArena;

/// <summary>
/// Призрак просит войти на ДМ арену.
/// </summary>
[Serializable, NetSerializable]
public sealed class DmArenaJoinRequestEvent : EntityEventArgs;

/// <summary>
/// Игрок просит покинуть ДМ арену.
/// </summary>
[Serializable, NetSerializable]
public sealed class DmArenaLeaveRequestEvent : EntityEventArgs;

/// <summary>
/// Сервер рассылает призракам текущее число игроков на ДМ арене.
/// </summary>
[Serializable, NetSerializable]
public sealed class DmArenaPlayerCountEvent : EntityEventArgs
{
    public int Count { get; }

    /// <summary>
    /// Включена ли арена. Решает только сервер.
    /// </summary>
    public bool Enabled { get; }

    public DmArenaPlayerCountEvent(int count, bool enabled)
    {
        Count = count;
        Enabled = enabled;
    }
}

/// <summary>
/// Сервер сообщает игроку на арене, что его прежнее тело на станции снова живо.
/// </summary>
[Serializable, NetSerializable]
public sealed class DmArenaRevivalOfferEvent : EntityEventArgs;

/// <summary>
/// Игрок принимает предложение и хочет вернуться в своё прежнее тело.
/// </summary>
[Serializable, NetSerializable]
public sealed class DmArenaRevivalAcceptEvent : EntityEventArgs;

/// <summary>
/// Сервер предлагает покинувшему арену игроку вернуться в бой или в своё тело.
/// </summary>
[Serializable, NetSerializable]
public sealed class DmArenaExitOfferEvent : EntityEventArgs;

/// <summary>
/// Ответ игрока на предложение: вернуться на арену или в своё прежнее тело.
/// </summary>
[Serializable, NetSerializable]
public sealed class DmArenaExitChoiceEvent : EntityEventArgs
{
    public bool Stay { get; }

    public DmArenaExitChoiceEvent(bool stay)
    {
        Stay = stay;
    }
}
