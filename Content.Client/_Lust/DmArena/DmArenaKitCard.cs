using Content.Shared._Lust.DmArena;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.Client._Lust.DmArena;

public sealed class DmArenaKitCard : PanelContainer
{
    public event Action<DmArenaStoreSetOption>? OnChosen;

    private readonly StyleBoxTexture _styleBox;

    public DmArenaKitCard(DmArenaStoreSetOption option, IReadOnlyList<string> itemNames)
    {
        MouseFilter = MouseFilterMode.Stop;
        HorizontalExpand = true;

        _styleBox = DmArenaPalette.Rounded(DmArenaPalette.CardBg, 14, 12);
        PanelOverride = _styleBox;

        var vbox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            SeparationOverride = 6,
        };

        var top = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Horizontal };
        top.AddChild(new Label
        {
            Text = option.Name,
            FontOverride = DmArenaPalette.GetFont(true, 14),
            FontColorOverride = DmArenaPalette.ButtonText,
            HorizontalExpand = true,
        });
        top.AddChild(new Label
        {
            Text = Loc.GetString("dm-arena-set-cost", ("cost", option.Cost)),
            FontOverride = DmArenaPalette.GetFont(false, 12),
            FontColorOverride = DmArenaPalette.Cost,
        });
        vbox.AddChild(top);

        if (!string.IsNullOrEmpty(option.Description))
            vbox.AddChild(MakeText(option.Description, DmArenaPalette.TextMuted));

        vbox.AddChild(MakeText(string.Join(", ", itemNames), DmArenaPalette.AccentSoft));

        var apply = new DmArenaPillButton
        {
            Text = Loc.GetString("dm-arena-set-apply"),
            MinHeight = 34,
            Margin = new Thickness(0, 6, 0, 0),
        };
        apply.OnPressed += () => OnChosen?.Invoke(option);
        vbox.AddChild(apply);

        AddChild(vbox);

        OnMouseEntered += _ => _styleBox.Modulate = DmArenaPalette.CardHoverBg;
        OnMouseExited += _ => _styleBox.Modulate = DmArenaPalette.CardBg;
    }

    private static RichTextLabel MakeText(string text, Color color)
    {
        var label = new RichTextLabel { HorizontalExpand = true };
        label.SetMessage(FormattedMessage.FromMarkupOrThrow(
            $"[font size=11][color={color.ToHex()}]{FormattedMessage.EscapeText(text)}[/color][/font]"));
        return label;
    }
}
