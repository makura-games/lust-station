using Content.Server.Chat.Systems;
using Content.Server.Jittering;
using Content.Server._Lust.Toys.Components;
using Content.Server._Sunrise.InteractionsPanel;
using Content.Shared._Sunrise.InteractionsPanel.Data.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Lust.Toys.Systems;

public sealed partial class VibratingSystem : EntitySystem
{
    [Dependency] private MobStateSystem _mobStateSystem = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ChatSystem _chatSystem = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private JitteringSystem _jittering = default!;
    [Dependency] private InteractionsPanel _panel = default!;

    // Добавляет стоны и заполняет панель
    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var curTime = _timing.CurTime;

        var query = EntityQueryEnumerator<VibratingComponent, InteractionsComponent>();
        while (query.MoveNext(out var uid, out var comp, out _))
        {
            if (!_mobStateSystem.IsAlive(uid))
                continue;

            if (curTime < comp.NextMoanTime)
                continue;

            var raw = comp.AddedLove + _random.Next(-comp.AddedLove / 2, comp.AddedLove / 2);
            var amount = FixedPoint2.New(raw / 100f); // Переводим в FixedPoint2
            _panel.ModifyLove(uid, amount);

            comp.NextMoanTime = curTime + TimeSpan.FromSeconds(comp.MoanInterval);

            _chatSystem.TryEmoteWithChat(uid, "Moan", ignoreActionBlocker: true);
            _jittering.AddJitter(uid, comp.Amplitude, comp.Frequency);
        }
    }
}
