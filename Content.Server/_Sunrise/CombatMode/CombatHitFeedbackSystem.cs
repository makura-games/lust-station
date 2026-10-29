using System.Collections.Generic;
using Content.Shared._Sunrise.CombatMode;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;

namespace Content.Server._Sunrise.CombatMode;

#pragma warning disable CS0618 // Событие подтверждает фактически применённое изменение урона после модификаторов и сопротивлений.
public sealed partial class CombatHitFeedbackSystem : EntitySystem
{
    [Dependency] private ISharedPlayerManager _playerManager = default!;

    private readonly Dictionary<(EntityUid Target, EntityUid Origin), CombatHitFeedbackType> _pendingFeedback = new();
    private readonly Dictionary<(EntityUid Target, EntityUid Origin), MobState> _statesBeforeDamage = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MobStateComponent, DamageDealtEvent>(OnBeforeDamage,
            before: new[] { typeof(DamageableSystem) });
        SubscribeLocalEvent<MobStateComponent, DamageChangedEvent>(OnDamageChanged,
            after: new[] { typeof(MobThresholdSystem) });
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var (key, type) in _pendingFeedback)
            SendToOrigin(key.Origin, type);

        _pendingFeedback.Clear();
        _statesBeforeDamage.Clear();
    }

    private void OnBeforeDamage(Entity<MobStateComponent> ent, ref DamageDealtEvent args)
    {
        if (args.Origin is { } origin)
            _statesBeforeDamage[(ent.Owner, origin)] = ent.Comp.CurrentState;
    }

    private void OnDamageChanged(Entity<MobStateComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased || args.Origin == null)
            return;

        // Вложенные обновления порогов могут изменить состояние без Origin.
        // Сравниваем состояние до удара с итоговым только после подтверждения положительного урона.
        CombatHitFeedbackType? feedback = null;
        if (_statesBeforeDamage.Remove((ent.Owner, args.Origin.Value), out var previous))
            feedback = CombatHitFeedbackRules.ClassifyStateChange(previous, ent.Comp.CurrentState, true);

        feedback ??= CombatHitFeedbackRules.ClassifyDamage(ent.Comp.CurrentState, true, true);
        if (feedback is { } type)
            QueueFeedback(ent.Owner, args.Origin.Value, type);
    }

    private void OnMobStateChanged(MobStateChangedEvent args)
    {
        var feedback = CombatHitFeedbackRules.ClassifyStateChange(
            args.OldMobState,
            args.NewMobState,
            hasOrigin: args.Origin != null);

        if (feedback is not { } type || args.Origin == null)
            return;

        QueueFeedback(args.Target, args.Origin.Value, type);
    }

    private void QueueFeedback(EntityUid target, EntityUid origin, CombatHitFeedbackType type)
    {
        // Для одной цели и атакующего за тик убийство важнее крита, а крит — обычного попадания.
        var key = (target, origin);
        if (!_pendingFeedback.TryGetValue(key, out var previous) || type > previous)
            _pendingFeedback[key] = type;
    }

    private void SendToOrigin(EntityUid origin, CombatHitFeedbackType type)
    {
        var current = origin;
        for (var depth = 0; depth < 16; depth++)
        {
            if (_playerManager.TryGetSessionByEntity(current, out var session))
            {
                RaiseNetworkEvent(new CombatHitFeedbackEvent(type), Filter.SinglePlayer(session));
                return;
            }

            if (!TryComp(current, out TransformComponent? transform) || transform == null || transform.ParentUid == current)
                return;

            current = transform.ParentUid;
        }
    }
}
#pragma warning restore CS0618
