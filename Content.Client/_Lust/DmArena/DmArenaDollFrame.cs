using System.Linq;
using System.Numerics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Content.Client._Lust.DmArena;

public sealed class DmArenaDollFrame : Control
{
    public SpriteView View { get; }

    public DmArenaDollFrame()
    {
        RectClipContent = true;
        HorizontalExpand = true;
        VerticalExpand = true;

        View = new SpriteView
        {
            OverrideDirection = Direction.South,
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Center,
        };
        AddChild(View);
    }

    protected override Vector2 MeasureOverride(Vector2 availableSize)
    {
        View.Measure(availableSize);
        return Vector2.Zero;
    }

    protected override void Resized()
    {
        base.Resized();

        var scale = Math.Clamp((int) (Math.Min(Size.X, Size.Y) / 36f), 3, 12);
        View.Scale = new Vector2(scale, scale);
        View.SetSize = new Vector2(scale * 40, scale * 40);
    }
}
