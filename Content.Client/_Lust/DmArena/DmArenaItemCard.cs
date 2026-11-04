using System.Linq;
using System.Numerics;
using Content.Shared._Lust.DmArena;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._Lust.DmArena;

public sealed class DmArenaItemCard : PanelContainer
{
    public event Action<DmArenaItemCard, int>? OnCountChanged;

    public int ItemIndex { get; }
    public int Cost { get; }
    public int MaxCount { get; }

    private int _count;
    private bool _locked;
    private bool _needsWeapon;
    private bool _hovered;
    private Color _targetColor;
    private readonly StyleBoxTexture _styleBox;
    private readonly PanelContainer _accent;
    private readonly Label _nameLabel;
    private readonly Label _costLabel;
    private readonly Label _countLabel;
    private readonly DmArenaPillButton _minusButton;
    private readonly DmArenaPillButton _plusButton;

    public DmArenaItemCard(DmArenaStoreOption option)
    {
        ItemIndex = option.Index;
        Cost = option.Cost;
        MaxCount = Math.Max(1, option.MaxCount);
        MouseFilter = MouseFilterMode.Stop;

        if (!string.IsNullOrEmpty(option.Description) || !string.IsNullOrEmpty(option.AmmoFor))
            TooltipSupplier = _ => BuildTooltip(option);

        _styleBox = DmArenaPalette.Rounded(DmArenaPalette.CardBg, 12, 6);
        PanelOverride = _styleBox;

        HorizontalExpand = true;
        MinHeight = 56;

        var hbox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalExpand = true,
            VerticalExpand = true,
            SeparationOverride = 8,
        };

        _accent = new PanelContainer
        {
            MinWidth = 4,
            MaxWidth = 4,
            VerticalExpand = true,
            Margin = new Thickness(0, 4),
            PanelOverride = new StyleBoxFlat { BackgroundColor = DmArenaPalette.AccentDim },
        };
        hbox.AddChild(_accent);

        if (!string.IsNullOrEmpty(option.SpritePrototype))
        {
            var spriteView = new EntityPrototypeView
            {
                MinSize = new Vector2(40, 40),
                SetSize = new Vector2(40, 40),
                Stretch = SpriteView.StretchMode.Fit,
                HorizontalAlignment = HAlignment.Center,
                VerticalAlignment = VAlignment.Center,
            };
            spriteView.SetPrototype(new EntProtoId(option.SpritePrototype));
            hbox.AddChild(spriteView);
        }

        _nameLabel = new Label
        {
            Text = option.Name,
            FontOverride = DmArenaPalette.GetFont(true, 13),
            FontColorOverride = DmArenaPalette.ButtonText,
            ClipText = true,
        };
        var nameBox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            VerticalAlignment = VAlignment.Center,
            HorizontalExpand = true,
            Margin = new Thickness(4, 0, 0, 0),
        };
        nameBox.AddChild(_nameLabel);
        hbox.AddChild(nameBox);

        _minusButton = MakeStepButton("−", -1);
        _plusButton = MakeStepButton("+", 1);
        _countLabel = new Label
        {
            FontOverride = DmArenaPalette.GetFont(true, 13),
            FontColorOverride = DmArenaPalette.AccentSoft,
            VerticalAlignment = VAlignment.Center,
            MinWidth = 26,
            Align = Label.AlignMode.Center,
        };

        if (MaxCount > 1)
        {
            hbox.AddChild(_minusButton);
            hbox.AddChild(_countLabel);
            hbox.AddChild(_plusButton);
        }

        _costLabel = new Label
        {
            Text = Loc.GetString("dm-arena-shop-cost", ("cost", option.Cost)),
            FontOverride = DmArenaPalette.GetFont(false, 12),
            FontColorOverride = DmArenaPalette.Cost,
            VerticalAlignment = VAlignment.Center,
            MinWidth = 44,
            Align = Label.AlignMode.Center,
        };
        var costPill = new PanelContainer
        {
            PanelOverride = DmArenaPalette.Rounded(DmArenaPalette.CostPillBg, 8, 2),
            VerticalAlignment = VAlignment.Center,
        };
        costPill.AddChild(_costLabel);
        hbox.AddChild(costPill);

        AddChild(hbox);

        OnKeyBindDown += OnClicked;
        OnMouseEntered += _ => SetHovered(true);
        OnMouseExited += _ => SetHovered(false);

        UpdateVisual();
        _styleBox.Modulate = _targetColor;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);
        _styleBox.Modulate = DmArenaPalette.Approach(_styleBox.Modulate, _targetColor, args.DeltaSeconds);
    }

    private Control BuildTooltip(DmArenaStoreOption option)
    {
        var panel = new PanelContainer { PanelOverride = DmArenaPalette.Rounded(DmArenaPalette.TitleBarBg, 12, 6) };
        panel.AddChild(new Label
        {
            Text = _needsWeapon ? Loc.GetString("dm-arena-ammo-locked") : option.Description,
            FontOverride = DmArenaPalette.GetFont(false, 11),
            FontColorOverride = DmArenaPalette.ButtonText,
        });
        return panel;
    }

    private void OnClicked(GUIBoundKeyEventArgs args)
    {
        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        if (MaxCount == 1)
            OnCountChanged?.Invoke(this, _count > 0 ? -1 : 1);
        else if (_count < MaxCount && !_locked)
            OnCountChanged?.Invoke(this, 1);

        args.Handle();
    }

    private void SetHovered(bool hovered)
    {
        _hovered = hovered;
        UpdateVisual();
    }

    private DmArenaPillButton MakeStepButton(string text, int delta)
    {
        var button = new DmArenaPillButton
        {
            Text = text,
            HorizontalExpand = false,
            VerticalAlignment = VAlignment.Center,
            MinSize = new Vector2(30, 30),
            MinHeight = 30,
        };
        button.OnPressed += () => OnCountChanged?.Invoke(this, delta);
        return button;
    }

    public void SetState(int count, bool locked, bool needsWeapon)
    {
        _count = count;
        _locked = locked;
        _needsWeapon = needsWeapon;
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        var selected = _count > 0;
        var dim = _locked && !selected;

        _targetColor = selected
            ? DmArenaPalette.CardSelectedBg
            : dim ? DmArenaPalette.CardLockedBg
            : _hovered ? DmArenaPalette.CardHoverBg : DmArenaPalette.CardBg;

        ((StyleBoxFlat) _accent.PanelOverride!).BackgroundColor = selected ? DmArenaPalette.Accent : DmArenaPalette.AccentDim;

        _nameLabel.FontColorOverride = dim ? DmArenaPalette.ButtonDisabledFg : DmArenaPalette.ButtonText;
        _costLabel.FontColorOverride = dim ? DmArenaPalette.ButtonDisabledFg : DmArenaPalette.Cost;

        _countLabel.Text = _count > 0 ? $"×{_count}" : string.Empty;
        _minusButton.Disabled = _count == 0;
        _plusButton.Disabled = _count >= MaxCount || _locked;
    }
}
