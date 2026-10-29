using System.Numerics;
using Content.Client._Lust.Weapons.Ranged;
using Content.Client.CombatMode;
using Content.Client.Hands.Systems;
using Content.Shared._Lust.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Shared.Enums;

namespace Content.Client._Lust.Combat;

public sealed class GunCombatIndicatorsOverlay : Overlay
{
    private static readonly Color JamIndicatorColor = Color.FromHex("#ff4d4d");
    private static readonly Color BoltIndicatorColor = Color.FromHex("#ffd34d");

    private readonly IInputManager _input;
    private readonly IEntityManager _entityManager;
    private readonly IEyeManager _eye;
    private readonly CombatModeSystem _combatMode;
    private readonly HandsSystem _hands;
    private readonly bool _showHeat;
    private readonly bool _showJam;
    private readonly bool _showBolt;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    public GunCombatIndicatorsOverlay(
        IInputManager input,
        IEntityManager entityManager,
        IEyeManager eye,
        CombatModeSystem combatMode,
        HandsSystem hands,
        bool showHeat,
        bool showJam,
        bool showBolt)
    {
        _input = input;
        _entityManager = entityManager;
        _eye = eye;
        _combatMode = combatMode;
        _hands = hands;
        _showHeat = showHeat;
        _showJam = showJam;
        _showBolt = showBolt;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (!_combatMode.IsInCombatMode())
            return false;

        return base.BeforeDraw(in args);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var mouse = _input.MouseScreenPosition;
        if (_eye.PixelToMap(mouse).MapId != args.MapId)
            return;

        args.ScreenHandle.SetTransform(Matrix3x2.Identity);
        var gun = _hands.GetActiveHandEntity();
        if (!_entityManager.HasComponent<GunComponent>(gun))
            return;

        var boltOpen = _entityManager.TryGetComponent(gun, out ChamberMagazineAmmoProviderComponent? chamber)
                       && chamber.BoltClosed == false;

        var scale = (args.ViewportControl as Control)?.UIScale ?? 1f;
        scale = MathF.Min(scale, 1.25f);
        var position = mouse.Position + new Vector2(14f, 12f) * scale;
        var yOffset = 0f;

        if (_entityManager.TryGetComponent<GunHeatComponent>(gun, out var heat))
        {
            if (_showHeat)
            {
                var (fill, jam) = GunHeatStatusControl.GetFractions(heat);
                if (fill > 0.01f)
                {
                    var size = new Vector2(30f, 5f) * scale;
                    GunHeatStatusControl.DrawGauge(
                        args.ScreenHandle,
                        UIBox2.FromDimensions(position, size),
                        fill,
                        jam,
                        MathF.Max(1f, scale));
                    yOffset += size.Y + 3f * scale;
                }
            }

            if (_showJam && heat.Jammed)
            {
                args.ScreenHandle.DrawCircle(
                    position + new Vector2(5f * scale, yOffset + 5f * scale),
                    4f * scale,
                    JamIndicatorColor);
                yOffset += 10f * scale;
            }
        }

        if (!_showBolt || !boltOpen)
            return;

        var boltSize = 10f * scale;
        var boltBox = UIBox2.FromDimensions(position + new Vector2(0f, yOffset), new Vector2(boltSize));
        args.ScreenHandle.DrawRect(boltBox, BoltIndicatorColor);
        args.ScreenHandle.DrawRect(
            UIBox2.FromDimensions(
                new Vector2(boltBox.Left + boltSize * 0.3f, boltBox.Top + boltSize * 0.3f),
                new Vector2(boltSize * 0.4f)),
            Color.Black);
    }
}
