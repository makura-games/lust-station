using Content.Shared._Lust.Weapons.Ranged.Components;
using Content.Shared.Atmos;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;

namespace Content.Client._Lust.Weapons.Ranged;

public sealed class GunHeatStatusControl : Control
{
    private readonly GunHeatComponent _heat;

    private static readonly Color Background = Color.Black.WithAlpha(0.45f);
    private static readonly Color Cool = Color.FromHex("#e0b050");
    private static readonly Color Hot = Color.FromHex("#ff3b1f");
    private static readonly Color JamMarker = Color.FromHex("#ffd34d");
    private static readonly Color CriticalMarker = Color.FromHex("#ff4d4d");

    public GunHeatStatusControl(GunHeatComponent heat)
    {
        _heat = heat;
        MinHeight = 7;
        HorizontalExpand = true;
        Margin = new Thickness(0, 2, 0, 0);
    }

    public static (float Fill, float Jam) GetFractions(GunHeatComponent heat)
    {
        var range = heat.MeltTemperature - Atmospherics.T20C;
        if (range <= 0f)
            return (0f, 1f);

        return (Math.Clamp((heat.Temperature - Atmospherics.T20C) / range, 0f, 1f),
            Math.Clamp((heat.JamTemperature - Atmospherics.T20C) / range, 0f, 1f));
    }

    public static void DrawGauge(DrawingHandleScreen handle, UIBox2 box, float fill, float jam, float marker)
    {
        handle.DrawRect(box, Background);
        if (fill > 0f)
        {
            var color = fill < jam ? Color.InterpolateBetween(Cool, Hot, fill / MathF.Max(jam, 0.01f) * 0.5f) : Hot;
            handle.DrawRect(new UIBox2(box.Left + 1, box.Top + 1, box.Left + 1 + ((box.Width - 2) * fill), box.Bottom - 1), color);
        }

        var jamX = box.Left + ((box.Width - 2) * jam);
        handle.DrawRect(new UIBox2(jamX, box.Top, jamX + marker, box.Bottom), JamMarker);
        handle.DrawRect(new UIBox2(box.Right - marker, box.Top, box.Right, box.Bottom), CriticalMarker);
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);
        var (fill, jam) = GetFractions(_heat);
        DrawGauge(handle, new UIBox2(0, 0, PixelWidth, PixelHeight), fill, jam, MathF.Max(1f, UIScale));
    }
}
