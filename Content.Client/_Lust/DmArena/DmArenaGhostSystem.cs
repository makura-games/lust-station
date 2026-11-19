using Content.Shared._Lust.DmArena;

namespace Content.Client._Lust.DmArena;

/// <summary>
/// Хранит состояние ДМ арены для кнопки призрака и живёт вместе с подключением к серверу.
/// </summary>
public sealed class DmArenaGhostSystem : EntitySystem
{
    public int PlayerCount { get; private set; }

    public bool Enabled { get; private set; }

    public event Action? StateChanged;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<DmArenaPlayerCountEvent>(OnPlayerCount);
    }

    public void RequestJoin()
    {
        RaiseNetworkEvent(new DmArenaJoinRequestEvent());
    }

    private void OnPlayerCount(DmArenaPlayerCountEvent ev)
    {
        PlayerCount = ev.Count;
        Enabled = ev.Enabled;
        StateChanged?.Invoke();
    }
}
