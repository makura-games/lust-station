using Content.Server.EUI;
using Content.Shared.Eui;
using Content.Shared._Lust.DmArena;
using Robust.Shared.Player;

namespace Content.Server._Lust.DmArena;

public sealed class DmArenaKitEui : BaseEui
{
    private readonly DmArenaMatchSystem _arena;
    private readonly EntityUid _ruleEntity;
    private readonly ICommonSession _session;

    public DmArenaKitEui(DmArenaMatchSystem arena, EntityUid ruleEntity, ICommonSession session)
    {
        _arena = arena;
        _ruleEntity = ruleEntity;
        _session = session;
    }

    public override EuiStateBase GetNewState()
    {
        return _arena.GetLoadoutState(_ruleEntity, _session.UserId);
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        switch (msg)
        {
            case DmArenaKitConfirmMessage selected:
                if (_arena.SpawnPlayer(_session, _ruleEntity, selected.Items))
                    Close();
                else
                    StateDirty();
                break;
            case DmArenaKitSaveMessage preset:
                _arena.SavePreset(_ruleEntity, _session.UserId, preset.Items);
                StateDirty();
                break;
        }
    }

    public override void Opened()
    {
        base.Opened();
        StateDirty();
    }

    public override void Closed()
    {
        base.Closed();
        _arena.OnEuiClosed(_session, this);
    }
}
