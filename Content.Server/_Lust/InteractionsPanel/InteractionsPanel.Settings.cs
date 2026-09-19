using Content.Shared._Sunrise.InteractionsPanel.Data.Components;
using Content.Shared._Sunrise.InteractionsPanel.Data.UI;

// Lust edit - расширение Sunrise-панели хранится в папке форка.
#pragma warning disable IDE0130
namespace Content.Server._Sunrise.InteractionsPanel;

public partial class InteractionsPanel
{
    private void OnSetInteractionPanelEnabled(
        Entity<InteractionsComponent> ent,
        ref SetInteractionPanelEnabledMessage args)
    {
        TrySetInteractionPanelEnabled(ent.AsNullable(), args.Enabled);
    }

    private void OnSetLoveDecayEnabled(
        Entity<InteractionsComponent> ent,
        ref SetLoveDecayEnabledMessage args)
    {
        TrySetLoveDecayEnabled(ent.AsNullable(), args.Enabled);
    }

    /// <summary>
    /// Tries to change whether the owner's interaction panel is available.
    /// </summary>
    public bool TrySetInteractionPanelEnabled(Entity<InteractionsComponent?> ent, bool enabled)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (!CanSetInteractionPanelEnabled(ent, enabled))
            return false;

        DoSetInteractionPanelEnabled((ent.Owner, ent.Comp), enabled);
        return true;
    }

    /// <summary>
    /// Checks whether the owner's interaction-panel availability can be changed.
    /// </summary>
    public bool CanSetInteractionPanelEnabled(
        Entity<InteractionsComponent?> ent,
        bool enabled,
        bool quiet = false)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        return ent.Comp.Erp != enabled;
    }

    private void DoSetInteractionPanelEnabled(Entity<InteractionsComponent> ent, bool enabled)
    {
        ent.Comp.Erp = enabled;
        Dirty(ent);

        if (!enabled)
        {
            ClosePanelsTargeting(ent);

            if (_ui.IsUiOpen(ent.Owner, InteractionWindowUiKey.Key))
                _ui.CloseUi(ent.Owner, InteractionWindowUiKey.Key);

            return;
        }

        UpdateOwnerUIState(ent);
    }

    /// <summary>
    /// Tries to change the owner's automatic interaction-progress decay preference.
    /// </summary>
    public bool TrySetLoveDecayEnabled(Entity<InteractionsComponent?> ent, bool enabled)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (!CanSetLoveDecayEnabled(ent, enabled))
            return false;

        DoSetLoveDecayEnabled((ent.Owner, ent.Comp), enabled);
        return true;
    }

    /// <summary>
    /// Checks whether the owner's automatic interaction-progress decay preference can be changed.
    /// </summary>
    public bool CanSetLoveDecayEnabled(
        Entity<InteractionsComponent?> ent,
        bool enabled,
        bool quiet = false)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        return ent.Comp.LoveDecayEnabled != enabled;
    }

    private void DoSetLoveDecayEnabled(Entity<InteractionsComponent> ent, bool enabled)
    {
        SetLoveDecayEnabledValue(ent, enabled);
        UpdateOwnerUIState(ent);
    }

    private bool ShouldDecayLove(Entity<InteractionsComponent> ent)
    {
        if (ent.Comp.LoveDecayEnabled)
            return true;

        if (ent.Comp.CurrentTarget is not { } target || target == ent.Owner)
            return true;

        return !TryComp<InteractionsComponent>(target, out var targetInteractions) ||
               targetInteractions.LoveDecayEnabled;
    }

    private void SetLoveDecayEnabledValue(Entity<InteractionsComponent> ent, bool enabled)
    {
        if (ent.Comp.LoveDecayEnabled == enabled)
            return;

        ent.Comp.LoveDecayEnabled = enabled;
        Dirty(ent);
    }

    private void UpdateOwnerUIState(Entity<InteractionsComponent> ent)
    {
        if (ent.Comp.CurrentTarget is not { } target)
            return;

        if (!_ui.IsUiOpen(ent.Owner, InteractionWindowUiKey.Key))
            return;

        _ui.SetUiState(ent.Owner, InteractionWindowUiKey.Key, PrepareUIState(ent, target));
    }

    private void ClosePanelsTargeting(EntityUid target)
    {
        var panelsToClose = new List<EntityUid>();
        var query = EntityQueryEnumerator<InteractionsComponent>();
        while (query.MoveNext(out var observer, out var interactions))
        {
            if (interactions.CurrentTarget != target)
                continue;

            if (!_ui.IsUiOpen(observer, InteractionWindowUiKey.Key))
                continue;

            panelsToClose.Add(observer);
        }

        foreach (var observer in panelsToClose)
        {
            _ui.CloseUi(observer, InteractionWindowUiKey.Key);
        }
    }

    private bool TryOpenUI(Entity<UserInterfaceComponent?> user, EntityUid target)
    {
        if (!CanOpenUI(user, target))
            return false;

        OpenUI(user, target);
        return true;
    }

    private bool CanOpenUI(EntityUid user, EntityUid target)
    {
        if (user == target)
            return true;

        return TryComp<InteractionsComponent>(target, out var interactions) && interactions.Erp;
    }
}
