using Content.Shared._Lust.Borgs.Components;
using Content.Shared.Alert;
using Content.Shared.PowerCell;
using Robust.Client.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._Lust.Borgs;

public sealed partial class LustBorgPassiveRechargeAlertSystem : EntitySystem
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private AlertsSystem _alerts = default!;
    [Dependency] private PowerCellSystem _powerCell = default!;

    private static readonly ProtoId<AlertPrototype> PassiveRechargeAlert = "LustPassiveRecharge";
    private static readonly TimeSpan UpdateDelay = TimeSpan.FromSeconds(0.5);

    private EntityUid? _previousPlayer;
    private TimeSpan _nextUpdate;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var player = _player.LocalEntity;
        if (_previousPlayer != player)
        {
            if (_previousPlayer is { } previousPlayer && Exists(previousPlayer))
                _alerts.ClearAlert(previousPlayer, PassiveRechargeAlert);

            _previousPlayer = player;
            _nextUpdate = TimeSpan.Zero;
        }

        if (player is not { } uid || _timing.CurTime < _nextUpdate)
            return;

        _nextUpdate = _timing.CurTime + UpdateDelay;

        if (HasComp<BorgRestingComponent>(uid) && _powerCell.TryGetBatteryFromSlot(uid, out _))
        {
            _alerts.ShowAlert(uid, PassiveRechargeAlert);
            return;
        }

        _alerts.ClearAlert(uid, PassiveRechargeAlert);
    }
}
