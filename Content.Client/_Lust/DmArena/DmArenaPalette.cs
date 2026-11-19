using Content.Client.Resources;
using Content.Client.Stylesheets;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Maths;

namespace Content.Client._Lust.DmArena;

public static class DmArenaPalette
{
    public static StyleBoxTexture Rounded(Color color, float horizontal = 12, float vertical = 6)
    {
        var texture = IoCManager.Resolve<IResourceCache>().GetTexture("/Textures/Interface/Nano/button.svg.96dpi.png");
        var box = new StyleBoxTexture { Texture = texture, Modulate = color };
        box.SetPatchMargin(StyleBox.Margin.All, 10);
        box.SetContentMarginOverride(StyleBox.Margin.Horizontal, horizontal);
        box.SetContentMarginOverride(StyleBox.Margin.Vertical, vertical);
        return box;
    }

    public static Font GetFont(bool bold, int size)
    {
        return IoCManager.Resolve<IResourceCache>().NotoStack(bold ? "Bold" : "Regular", size);
    }

    public static readonly Color BodyBg = Color.FromHex("#04080D");
    public static readonly Color TitleBarBg = Color.FromHex("#08111A");
    public static readonly Color Divider = Color.FromHex("#17394D");

    public static readonly Color CardBg = Color.FromHex("#0A141E");
    public static readonly Color CardHoverBg = Color.FromHex("#123350B3");
    public static readonly Color CardSelectedBg = Color.FromHex("#0F2E3C");
    public static readonly Color CardLockedBg = Color.FromHex("#120F13");

    public static readonly Color Accent = Color.FromHex("#4A9DB8");
    public static readonly Color AccentDim = Color.FromHex("#1F4F63");
    public static readonly Color CostPillBg = Color.FromHex("#0E2A38");
    public static readonly Color AccentSoft = Color.FromHex("#86BFD4");

    public static readonly Color ButtonBg = Color.FromHex("#0F2A44");
    public static readonly Color ButtonHoverBg = Color.FromHex("#18436A");
    public static readonly Color ButtonSelectedBg = Color.FromHex("#1F5A78");
    public static readonly Color ButtonDisabledBg = Color.FromHex("#0A1118");
    public static readonly Color ButtonDisabledFg = Color.FromHex("#47596A");
    public static readonly Color ButtonText = Color.FromHex("#E9F0F5");

    public static readonly Color TextMuted = Color.FromHex("#6F8A9C");
    public static readonly Color Cost = Color.FromHex("#E6EEF2");

    public static Color Approach(Color from, Color to, float frameTime)
    {
        var next = Color.InterpolateBetween(from, to, Math.Clamp(frameTime * 14f, 0f, 1f));
        var diff = Math.Abs(next.R - to.R) + Math.Abs(next.G - to.G) + Math.Abs(next.B - to.B) + Math.Abs(next.A - to.A);
        return diff < 0.01f ? to : next;
    }
}
