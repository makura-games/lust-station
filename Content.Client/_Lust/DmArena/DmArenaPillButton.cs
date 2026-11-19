using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Content.Client._Lust.DmArena;

public sealed class DmArenaPillButton : PanelContainer
{
    public event Action? OnPressed;

    private readonly StyleBoxTexture _styleBox;
    private readonly Label _label;
    private Color _targetColor;
    private bool _disabled;
    private bool _selected;
    private bool _hovered;
    private bool _pressed;

    public bool Disabled
    {
        get => _disabled;
        set
        {
            _disabled = value;
            UpdateVisual();
        }
    }

    public bool Selected
    {
        get => _selected;
        set
        {
            _selected = value;
            UpdateVisual();
        }
    }

    public string Text
    {
        get => _label.Text ?? string.Empty;
        set => _label.Text = value;
    }

    public DmArenaPillButton()
    {
        MouseFilter = MouseFilterMode.Stop;
        HorizontalExpand = true;
        MinHeight = 45;

        _styleBox = DmArenaPalette.Rounded(DmArenaPalette.ButtonBg, 14, 8);
        PanelOverride = _styleBox;

        _label = new Label
        {
            Text = string.Empty,
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Center,
            HorizontalExpand = true,
            FontOverride = DmArenaPalette.GetFont(true, 12),
            FontColorOverride = DmArenaPalette.ButtonText,
        };
        AddChild(_label);

        OnKeyBindDown += OnClicked;
        OnKeyBindUp += args =>
        {
            if (args.Function == EngineKeyFunctions.UIClick)
                SetPressed(false);
        };
        OnMouseEntered += _ => SetHovered(true);
        OnMouseExited += _ =>
        {
            SetHovered(false);
            SetPressed(false);
        };

        UpdateVisual();
        _styleBox.Modulate = _targetColor;
    }

    private void OnClicked(GUIBoundKeyEventArgs args)
    {
        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        if (!_disabled)
        {
            SetPressed(true);
            OnPressed?.Invoke();
        }

        args.Handle();
    }

    private void SetHovered(bool hovered)
    {
        _hovered = hovered;
        UpdateVisual();
    }

    private void SetPressed(bool pressed)
    {
        _pressed = pressed;
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (_disabled)
        {
            _targetColor = DmArenaPalette.ButtonDisabledBg;
            _label.FontColorOverride = DmArenaPalette.ButtonDisabledFg;
            return;
        }

        var color = _selected ? DmArenaPalette.ButtonSelectedBg
            : _hovered ? DmArenaPalette.ButtonHoverBg : DmArenaPalette.ButtonBg;

        _targetColor = _pressed ? Color.InterpolateBetween(color, Color.Black, 0.2f) : color;
        _label.FontColorOverride = DmArenaPalette.ButtonText;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        _styleBox.Modulate = DmArenaPalette.Approach(_styleBox.Modulate, _targetColor, args.DeltaSeconds);
    }
}
