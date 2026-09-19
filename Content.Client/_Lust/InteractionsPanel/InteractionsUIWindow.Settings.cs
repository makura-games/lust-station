using System.Numerics;
using Content.Client.Stylesheets;
using Content.Shared._Sunrise.InteractionsPanel.Data.UI;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;

// Lust edit - расширение Sunrise-панели хранится в папке форка.
#pragma warning disable IDE0130
namespace Content.Client._Sunrise.InteractionsPanel;

public sealed partial class InteractionsUIWindow
{
    // Эта часть хранит состояние настроек и диалог подтверждения отключения панели.
    private bool _panelEnabled = true;
    private bool _updatingPanelSetting;
    private bool _loveDecayEnabled = true;
    private bool _updatingLoveDecaySetting;

    private void OnDisableInteractionPanelToggled(BaseButton.ButtonToggledEventArgs args)
    {
        if (_updatingPanelSetting)
            return;

        if (!args.Pressed)
        {
            if (!_panelEnabled)
                _owner?.SendBoundUserInterfaceMessage(new SetInteractionPanelEnabledMessage(true));

            return;
        }

        if (!_panelEnabled)
            return;

        OpenDisablePanelConfirmation();
    }

    private void OnDisableLoveDecayToggled(BaseButton.ButtonToggledEventArgs args)
    {
        if (_updatingLoveDecaySetting)
            return;

        var enabled = !args.Pressed;
        if (enabled == _loveDecayEnabled)
            return;

        _loveDecayEnabled = enabled;
        _owner?.SendBoundUserInterfaceMessage(new SetLoveDecayEnabledMessage(enabled));
    }

    private void OpenDisablePanelConfirmation()
    {
        DisableInteractionPanelCheckBox.Disabled = true;

        var confirmDialog = new DefaultWindow
        {
            Title = Loc.GetString("interaction-panel-disable-confirmation-title"),
            MinSize = new Vector2(340, 150)
        };

        var dialogPanel = new PanelContainer
        {
            VerticalExpand = true,
            HorizontalExpand = true,
            StyleClasses = { StyleClass.PanelDark }
        };

        var dialogBox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Margin = new Thickness(12),
            VerticalExpand = true
        };

        var questionLabel = new Label
        {
            Text = Loc.GetString("interaction-panel-disable-confirmation-text"),
            HorizontalExpand = true,
            HorizontalAlignment = HAlignment.Center,
            StyleClasses = { StyleClass.Highlight }
        };

        var buttonsBox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Bottom,
            VerticalExpand = true,
            Margin = new Thickness(0, 12, 0, 0)
        };

        var cancelButton = new Button
        {
            Text = Loc.GetString("interaction-panel-disable-cancel"),
            StyleClasses = { StyleClass.ButtonSquare },
            Margin = new Thickness(0, 0, 6, 0)
        };

        var confirmButton = new Button
        {
            Text = Loc.GetString("interaction-panel-disable-confirm"),
            StyleClasses = { StyleClass.ButtonSquare }
        };

        var confirmed = false;
        confirmDialog.OnClose += () =>
        {
            DisableInteractionPanelCheckBox.Disabled = false;
            if (!confirmed)
                DisableInteractionPanelCheckBox.Pressed = false;
        };

        cancelButton.OnPressed += _ => confirmDialog.Close();

        confirmButton.OnPressed += _ =>
        {
            confirmed = true;
            _owner?.SendBoundUserInterfaceMessage(new SetInteractionPanelEnabledMessage(false));
            confirmDialog.Close();
        };

        buttonsBox.AddChild(cancelButton);
        buttonsBox.AddChild(confirmButton);
        dialogBox.AddChild(questionLabel);
        dialogBox.AddChild(buttonsBox);
        dialogPanel.AddChild(dialogBox);
        confirmDialog.AddChild(dialogPanel);
        confirmDialog.OpenCentered();
    }
}
