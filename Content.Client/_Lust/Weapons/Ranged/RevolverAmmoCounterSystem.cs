using Content.Client.Items;
using Content.Client.Items.UI;
using Content.Client.Stylesheets;
using Content.Client.Stylesheets.Palette;
using Content.Client.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._Lust.Weapons.Ranged;

/// <summary>
/// Показывает только количество боевых патронов, не раскрывая положение барабана.
/// </summary>
public sealed class LustRevolverAmmoCounterSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RevolverAmmoProviderComponent, ComponentStartup>(OnRevolverStartup);
        SubscribeLocalEvent<AmmoCounterComponent, ComponentStartup>(OnAmmoCounterStartup);
        SubscribeLocalEvent<RevolverAmmoProviderComponent, ItemStatusCollectMessage>(OnItemStatusCollect);
    }

    private void OnRevolverStartup(Entity<RevolverAmmoProviderComponent> ent, ref ComponentStartup args)
    {
        // У штатного счётчика нет сетевого состояния; заменяем его только на клиенте.
        RemComp<AmmoCounterComponent>(ent);
    }

    private void OnAmmoCounterStartup(Entity<AmmoCounterComponent> ent, ref ComponentStartup args)
    {
        if (HasComp<RevolverAmmoProviderComponent>(ent))
            RemComp<AmmoCounterComponent>(ent);
    }

    private void OnItemStatusCollect(Entity<RevolverAmmoProviderComponent> ent, ref ItemStatusCollectMessage args)
    {
        args.Controls.Add(new RevolverStatusControl(this, ent.Owner));
    }

    private AmmoStatus GetStatus(EntityUid uid)
    {
        if (!TryComp<RevolverAmmoProviderComponent>(uid, out var revolver))
            return default;

        var count = 0;
        var hasCasings = false;

        for (var i = 0; i < revolver.Chambers.Length; i++)
        {
            var chamber = revolver.Chambers[i];
            var ammo = i < revolver.AmmoSlots.Count ? revolver.AmmoSlots[i] : null;

            if (ammo is { } ammoUid)
            {
                if (TryComp<CartridgeAmmoComponent>(ammoUid, out var cartridge) && cartridge.Spent)
                    hasCasings = true;
                else
                    count++;

                continue;
            }

            if (chamber == true)
                count++;
            else if (chamber == false)
                hasCasings = true;
        }

        return new AmmoStatus(count, revolver.Capacity, hasCasings);
    }

    private readonly record struct AmmoStatus(int Count, int Capacity, bool HasCasings);

    private sealed class RevolverStatusControl : PollingItemStatusControl<AmmoStatus>
    {
        private readonly LustRevolverAmmoCounterSystem _system;
        private readonly EntityUid _revolver;
        private readonly Label _ammoCount;

        public RevolverStatusControl(LustRevolverAmmoCounterSystem system, EntityUid revolver)
        {
            _system = system;
            _revolver = revolver;

            MinHeight = 15;
            HorizontalExpand = true;
            VerticalAlignment = VAlignment.Center;
            AddChild(_ammoCount = new Label
            {
                StyleClasses = { StyleClass.ItemStatus },
                HorizontalAlignment = HAlignment.Right,
                VerticalAlignment = VAlignment.Bottom
            });
        }

        protected override AmmoStatus PollData()
        {
            return _system.GetStatus(_revolver);
        }

        protected override void Update(in AmmoStatus status)
        {
            _ammoCount.Text = $"{status.Count}/{status.Capacity}";
            _ammoCount.FontColorOverride = status.Count == 0
                ? status.HasCasings ? Palettes.Status.Critical : Color.Gray
                : status.Count * 3 > status.Capacity * 2
                    ? Palettes.Status.Good
                    : status.Count * 3 > status.Capacity
                        ? Palettes.Status.Warning
                        : Palettes.Status.Critical;
        }
    }
}
