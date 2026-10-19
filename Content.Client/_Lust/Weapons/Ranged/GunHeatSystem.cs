using System.Linq;
using Content.Client.Items;
using Content.Client.Weapons.Ranged.Systems;
using Content.Shared._Lust.Weapons.Ranged.Components;
using Content.Shared._Lust.Weapons.Ranged.Systems;
using Content.Shared.Hands;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.GameStates;
using Robust.Shared.GameObjects;

namespace Content.Client._Lust.Weapons.Ranged;

public sealed partial class GunHeatSystem : SharedGunHeatSystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    private readonly Dictionary<EntityUid, HeldGlow> _held = new();
    private static readonly Color HeatGlowColor = Color.FromHex("#ff3b1f");

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GunHeatComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<GunHeatComponent, AfterAutoHandleStateEvent>(OnHandleState);
        SubscribeLocalEvent<GunHeatComponent, HeldVisualsUpdatedEvent>(OnHeldVisualsUpdated);
        SubscribeLocalEvent<GunHeatComponent, ItemStatusCollectMessage>(OnItemStatus, after: [typeof(GunSystem)]);
    }

    private void OnItemStatus(Entity<GunHeatComponent> ent, ref ItemStatusCollectMessage args)
    {
        args.Controls.Add(new GunHeatStatusControl(ent.Comp));
    }

    private void OnShutdown(Entity<GunHeatComponent> ent, ref ComponentShutdown args)
    {
        ClearHeldGlow(ent);
    }

    private void OnHandleState(Entity<GunHeatComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateHeldGlow(ent, SharedGunHeatSystem.GetGlow(ent.Comp, ent.Comp.Temperature));
    }

    private void OnHeldVisualsUpdated(Entity<GunHeatComponent> ent, ref HeldVisualsUpdatedEvent args)
    {
        ClearHeldGlow(ent);
        if (args.RevealedLayers.Count == 0)
            return;

        var layers = args.RevealedLayers.Where(key => !key.EndsWith("-displacement", StringComparison.Ordinal)).ToArray();
        if (layers.Length == 0)
            return;

        _held[ent] = new HeldGlow(args.User, layers);
        UpdateHeldGlow(ent, SharedGunHeatSystem.GetGlow(ent.Comp, ent.Comp.Temperature));
    }

    private void UpdateHeldGlow(Entity<GunHeatComponent> ent, float glow)
    {
        if (!_held.TryGetValue(ent, out var held) || !TryComp<SpriteComponent>(held.Holder, out var sprite))
            return;

        foreach (var key in held.Layers)
        {
            if (!sprite.LayerMapTryGet(key, out var index) || sprite[index] is not SpriteComponent.Layer layer)
                continue;

            if (glow <= 0f)
            {
                if (held.OriginalColors.Remove(key, out var original))
                    _sprite.LayerSetColor((held.Holder, sprite), index, original);

                continue;
            }

            if (!held.OriginalColors.TryGetValue(key, out var baseColor))
            {
                baseColor = layer.Color;
                held.OriginalColors[key] = baseColor;
            }

            _sprite.LayerSetColor((held.Holder, sprite), index, Color.InterpolateBetween(baseColor, HeatGlowColor, glow));
        }
    }

    private void ClearHeldGlow(Entity<GunHeatComponent> ent)
    {
        if (!_held.Remove(ent, out var held) || !Exists(held.Holder) || !TryComp<SpriteComponent>(held.Holder, out var sprite))
            return;

        foreach (var (key, color) in held.OriginalColors)
        {
            if (sprite.LayerMapTryGet(key, out var index))
                _sprite.LayerSetColor((held.Holder, sprite), index, color);
        }
    }

    private sealed class HeldGlow(EntityUid holder, string[] layers)
    {
        public readonly EntityUid Holder = holder;
        public readonly string[] Layers = layers;
        public readonly Dictionary<string, Color> OriginalColors = new();
    }
}
