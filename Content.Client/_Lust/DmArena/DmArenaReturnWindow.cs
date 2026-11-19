using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client._Lust.DmArena;

public sealed class DmArenaReturnWindow : DmArenaFrameWindow
{
    public event Action? OnAccepted;

    public DmArenaReturnWindow()
    {
        WindowTitle = Loc.GetString("dm-arena-revival-title");
        SetSize = new Vector2(380, 190);
        Resizable = false;

        var message = new RichTextLabel
        {
            HorizontalAlignment = HAlignment.Center,
            MaxWidth = 320,
            Margin = new Thickness(0, 18),
        };
        message.SetMessage(FormattedMessage.FromMarkupOrThrow(
            $"[color={DmArenaPalette.ButtonText.ToHex()}]{FormattedMessage.EscapeText(Loc.GetString("dm-arena-revival-offer"))}[/color]"));
        Contents.AddChild(message);
        Contents.AddChild(new PanelContainer
        {
            MinHeight = 1,
            Margin = new Thickness(14, 0),
            PanelOverride = new StyleBoxFlat { BackgroundColor = DmArenaPalette.Divider },
        });

        var buttons = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalExpand = true,
            SeparationOverride = 8,
            Margin = new Thickness(8, 4),
        };

        var acceptButton = new DmArenaPillButton
        {
            Text = Loc.GetString("dm-arena-revival-accept"),
        };
        acceptButton.OnPressed += () =>
        {
            OnAccepted?.Invoke();
            Close();
        };
        buttons.AddChild(acceptButton);

        var declineButton = new DmArenaPillButton
        {
            Text = Loc.GetString("dm-arena-revival-decline"),
        };
        declineButton.OnPressed += () => Close();
        buttons.AddChild(declineButton);

        Contents.AddChild(buttons);
    }
}
