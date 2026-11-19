using System.Linq;
using System.Numerics;
using Content.Client.Lobby;
using Content.Client.Station;
using Content.Shared.Body;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Preferences;
using Content.Shared._Lust.DmArena;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Client._Lust.DmArena;

public sealed partial class DmArenaKitWindow : DmArenaFrameWindow
{
    [Dependency] private IEntityManager _entMan = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IClientPreferencesManager _prefs = default!;
    [Dependency] private IUserInterfaceManager _ui = default!;

    public event Action<List<int>>? OnLoadoutConfirmed;
    public event Action<List<int>>? OnPresetSaved;

    private DmArenaKitEuiState? _state;

    private readonly Dictionary<int, int> _selected = new();
    private readonly Dictionary<int, DmArenaItemCard> _cards = new();
    private readonly Dictionary<int, DmArenaStoreOption> _options = new();

    private readonly Label _playerCountLabel;
    private readonly Label _balanceLabel;
    private readonly BoxContainer _leftColumn;
    private readonly BoxContainer _rightColumn;
    private readonly DmArenaDollFrame _preview;
    private readonly DmArenaPillButton _confirmButton;
    private readonly DmArenaPillButton _saveButton;
    private readonly DmArenaPillButton _loadButton;
    private readonly DmArenaPillButton _lastButton;
    private readonly DmArenaPillButton _clearButton;
    private readonly DmArenaPillButton _builderTab;
    private readonly DmArenaPillButton _setsTab;

    private EntityUid _dummy;
    private string? _heldCategory;
    private bool _setsOpen;

    public DmArenaKitWindow()
    {
        IoCManager.InjectDependencies(this);

        WindowTitle = Loc.GetString("dm-arena-loadout-title");
        Resizable = true;
        MinSize = new Vector2(920, 600);
        SetSize = _ui.WindowRoot.Size * 0.9f;

        var tabs = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalExpand = true,
            SeparationOverride = 8,
            Margin = new Thickness(12, 10, 12, 0),
        };
        _builderTab = new DmArenaPillButton { Text = Loc.GetString("dm-arena-tab-builder"), HorizontalExpand = false, MinWidth = 190, MinHeight = 38 };
        _setsTab = new DmArenaPillButton { Text = Loc.GetString("dm-arena-tab-sets"), HorizontalExpand = false, MinWidth = 190, MinHeight = 38 };
        tabs.AddChild(_builderTab);
        tabs.AddChild(_setsTab);
        tabs.AddChild(new Control { HorizontalExpand = true });
        Contents.AddChild(tabs);

        var main = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalExpand = true,
            VerticalExpand = true,
            SeparationOverride = 16,
            Margin = new Thickness(16, 12, 16, 16),
        };

        _leftColumn = MakeColumn(main);
        var center = MakeCenter(out _preview, out _balanceLabel, out _playerCountLabel,
            out _confirmButton, out _saveButton, out _loadButton, out _lastButton, out _clearButton);
        main.AddChild(center);
        _rightColumn = MakeColumn(main);

        Contents.AddChild(main);

        _confirmButton.OnPressed += () => OnLoadoutConfirmed?.Invoke(Expand());
        _saveButton.OnPressed += () => OnPresetSaved?.Invoke(Expand());
        _loadButton.OnPressed += () => LoadItems(_state?.Preset);
        _lastButton.OnPressed += () => LoadItems(_state?.Last);
        _clearButton.OnPressed += () =>
        {
            _selected.Clear();
            Refresh();
        };
        _builderTab.OnPressed += () => SwitchTab(false);
        _setsTab.OnPressed += () => SwitchTab(true);
        UpdateTabs();
    }

    private static BoxContainer MakeColumn(BoxContainer parent)
    {
        var column = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            SeparationOverride = 4,
            Margin = new Thickness(0, 0, 8, 0),
        };
        var scroll = new ScrollContainer
        {
            HorizontalExpand = true,
            VerticalExpand = true,
            HScrollEnabled = false,
            MinWidth = 250,
            SizeFlagsStretchRatio = 1.5f,
        };
        scroll.AddChild(column);
        parent.AddChild(scroll);
        return column;
    }

    private static Control MakeHeader(string text)
    {
        var panel = new PanelContainer
        {
            Margin = new Thickness(0, 8, 0, 2),
            PanelOverride = DmArenaPalette.Rounded(DmArenaPalette.TitleBarBg, 10, 4),
        };
        panel.AddChild(new Label
        {
            Text = text,
            FontOverride = DmArenaPalette.GetFont(true, 14),
            FontColorOverride = DmArenaPalette.Accent,
        });
        return panel;
    }

    private BoxContainer MakeCenter(
        out DmArenaDollFrame preview,
        out Label balance,
        out Label players,
        out DmArenaPillButton confirm,
        out DmArenaPillButton save,
        out DmArenaPillButton load,
        out DmArenaPillButton last,
        out DmArenaPillButton clear)
    {
        var center = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            VerticalExpand = true,
            SeparationOverride = 10,
            MinWidth = 220,
            SizeFlagsStretchRatio = 0.7f,
        };

        var previewPanel = new PanelContainer
        {
            VerticalExpand = true,
            HorizontalExpand = true,
            MaxHeight = 420,
            PanelOverride = DmArenaPalette.Rounded(DmArenaPalette.CardBg, 6, 6),
        };
        preview = new DmArenaDollFrame();
        previewPanel.AddChild(preview);
        center.AddChild(previewPanel);

        balance = new Label
        {
            FontOverride = DmArenaPalette.GetFont(true, 18),
            FontColorOverride = DmArenaPalette.ButtonText,
            HorizontalAlignment = HAlignment.Center,
        };
        center.AddChild(balance);

        players = new Label
        {
            FontOverride = DmArenaPalette.GetFont(false, 11),
            FontColorOverride = DmArenaPalette.TextMuted,
            HorizontalAlignment = HAlignment.Center,
        };
        center.AddChild(players);

        var presetRow = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalExpand = true,
            SeparationOverride = 6,
        };
        save = new DmArenaPillButton { Text = Loc.GetString("dm-arena-shop-preset-save") };
        load = new DmArenaPillButton { Text = Loc.GetString("dm-arena-shop-preset-load") };
        clear = new DmArenaPillButton { Text = Loc.GetString("dm-arena-shop-clear") };
        presetRow.AddChild(save);
        presetRow.AddChild(load);
        presetRow.AddChild(clear);
        center.AddChild(presetRow);

        last = new DmArenaPillButton { Text = Loc.GetString("dm-arena-shop-last"), MinHeight = 36 };
        center.AddChild(last);

        confirm = new DmArenaPillButton
        {
            Text = Loc.GetString("dm-arena-loadout-confirm"),
            Disabled = true,
            MinHeight = 50,
        };
        center.AddChild(confirm);

        return center;
    }

    private List<int> Expand()
    {
        return _selected.SelectMany(pair => Enumerable.Repeat(pair.Key, pair.Value)).ToList();
    }

    private void SwitchTab(bool sets)
    {
        if (_setsOpen == sets)
            return;

        _setsOpen = sets;
        UpdateTabs();
        BuildColumns();
        Refresh();
    }

    private void UpdateTabs()
    {
        _builderTab.Selected = !_setsOpen;
        _setsTab.Selected = _setsOpen;
    }

    public void UpdateState(DmArenaKitEuiState state)
    {
        _state = state;
        _playerCountLabel.Text = Loc.GetString("dm-arena-loadout-players", ("count", state.PlayerCount));

        _options.Clear();
        state.Items.ForEach(item => _options[item.Index] = item);

        foreach (var stale in _selected.Keys.Except(_options.Keys).ToList())
        {
            _selected.Remove(stale);
        }

        PruneAmmo();

        _heldCategory = state.Items.Count > 0 ? state.Items[0].Category : null;

        BuildColumns();
        Refresh();
    }

    private void BuildColumns()
    {
        if (_state == null)
            return;

        _leftColumn.RemoveAllChildren();
        _rightColumn.RemoveAllChildren();
        _cards.Clear();

        if (_setsOpen)
            BuildSets();
        else
            BuildShop();
    }

    private void BuildShop()
    {
        if (_state == null)
            return;

        string? lastCategory = null;
        string? lastGroup = null;

        var ordered = _state.Items.GroupBy(item => (item.Category, item.Group)).SelectMany(group => group.OrderBy(item => item.Cost));

        foreach (var item in ordered)
        {
            var column = item.Category == _heldCategory ? _leftColumn : _rightColumn;

            if (item.Category != lastCategory)
            {
                lastCategory = item.Category;
                lastGroup = null;
                column.AddChild(MakeHeader(item.Category));
            }

            if (item.Group != lastGroup && !string.IsNullOrEmpty(item.Group))
            {
                column.AddChild(new Label
                {
                    Text = item.Group,
                    FontOverride = DmArenaPalette.GetFont(true, 11),
                    FontColorOverride = DmArenaPalette.TextMuted,
                    Margin = new Thickness(8, 8, 0, 0),
                });
            }

            lastGroup = item.Group;

            var card = new DmArenaItemCard(item);
            card.OnCountChanged += OnCountChanged;
            _cards[item.Index] = card;
            column.AddChild(card);
        }
    }

    private void BuildSets()
    {
        if (_state == null)
            return;

        if (_state.Sets.Count == 0)
        {
            _leftColumn.AddChild(new Label
            {
                Text = Loc.GetString("dm-arena-set-empty"),
                FontColorOverride = DmArenaPalette.TextMuted,
                Margin = new Thickness(8),
            });
            return;
        }

        _leftColumn.AddChild(MakeHeader(Loc.GetString("dm-arena-tab-sets")));

        var sets = _state.Sets.OrderBy(set => set.Cost).ToList();

        for (var i = 0; i < sets.Count; i++)
        {
            var set = sets[i];
            var names = set.Items
                .Where(_options.ContainsKey)
                .GroupBy(index => index)
                .Select(group => group.Count() > 1
                    ? $"{_options[group.Key].Name} ×{group.Count()}"
                    : _options[group.Key].Name)
                .ToList();

            var card = new DmArenaKitCard(set, names);
            card.OnChosen += OnSetChosen;

            (i % 2 == 0 ? _leftColumn : _rightColumn).AddChild(card);
        }
    }

    private void OnSetChosen(DmArenaStoreSetOption set)
    {
        _selected.Clear();
        foreach (var index in set.Items)
        {
            _selected[index] = _selected.GetValueOrDefault(index) + 1;
        }

        SwitchTab(false);
    }

    private void OnCountChanged(DmArenaItemCard card, int delta)
    {
        var index = card.ItemIndex;
        var count = _selected.GetValueOrDefault(index);

        if (delta > 0)
        {
            if (!_options.TryGetValue(index, out var option) || count >= card.MaxCount || !AmmoAvailable(option))
                return;

            var conflicts = count == 0 ? GetConflicts(index) : new List<int>();
            if (Spent() - RefundFor(conflicts) + card.Cost > _state!.Budget)
                return;

            conflicts.ForEach(conflict => _selected.Remove(conflict));
            _selected[index] = count + 1;
        }
        else if (count > 1)
        {
            _selected[index] = count - 1;
        }
        else
        {
            _selected.Remove(index);
        }

        PruneAmmo();
        Refresh();
    }

    private int RefundFor(IEnumerable<int> indexes)
    {
        return indexes.Sum(index => _options.TryGetValue(index, out var option) ? option.Cost * _selected.GetValueOrDefault(index) : 0);
    }

    private bool AmmoAvailable(DmArenaStoreOption option)
    {
        if (string.IsNullOrEmpty(option.AmmoFor) || option.AmmoFor == "base")
            return true;

        return _selected.Keys.Any(index => _options.TryGetValue(index, out var other) && other.AmmoType == option.AmmoFor);
    }

    private void PruneAmmo()
    {
        while (FindUnsupportedAmmo() is { } index)
        {
            _selected.Remove(index);
        }
    }

    private int? FindUnsupportedAmmo()
    {
        foreach (var index in _selected.Keys)
        {
            if (!_options.TryGetValue(index, out var option) || !AmmoAvailable(option))
                return index;
        }

        return null;
    }

    private List<int> GetConflicts(int index)
    {
        var result = new List<int>();
        if (!_options.TryGetValue(index, out var option))
            return result;

        var others = _selected.Keys.Where(other => other != index && _options.ContainsKey(other)).ToList();

        result.AddRange(others.Where(other => _options[other].Slots.Any(option.Slots.Contains)));

        var hands = option.Hands + others.Where(other => !result.Contains(other)).Sum(other => _options[other].Hands);
        foreach (var other in others)
        {
            if (hands <= 2)
                break;

            var otherHands = _options[other].Hands;
            if (result.Contains(other) || otherHands == 0)
                continue;

            result.Add(other);
            hands -= otherHands;
        }

        return result;
    }

    private void LoadItems(List<int>? items)
    {
        if (items == null)
            return;

        _selected.Clear();
        foreach (var index in items.Where(_options.ContainsKey))
        {
            _selected[index] = _selected.GetValueOrDefault(index) + 1;
        }

        PruneAmmo();
        Refresh();
    }

    private int Spent()
    {
        return _selected.Sum(pair => _options.TryGetValue(pair.Key, out var option) ? option.Cost * pair.Value : 0);
    }

    private void Refresh()
    {
        if (_state == null)
            return;

        var spent = Spent();
        foreach (var (index, card) in _cards)
        {
            var count = _selected.GetValueOrDefault(index);

            var needsWeapon = count == 0 && !AmmoAvailable(_options[index]);
            var conflicts = count == 0 ? GetConflicts(index) : new List<int>();
            var locked = count > 0
                ? spent + card.Cost > _state.Budget
                : spent - RefundFor(conflicts) + card.Cost > _state.Budget || needsWeapon;

            card.SetState(count, locked, needsWeapon);
        }

        _balanceLabel.Text = Loc.GetString("dm-arena-shop-balance", ("left", _state.Budget - spent), ("total", _state.Budget));

        var empty = _selected.Count == 0;
        _confirmButton.Disabled = empty;
        _saveButton.Disabled = empty;
        _loadButton.Disabled = _state.Preset == null;
        _lastButton.Disabled = _state.Last == null;
        _clearButton.Disabled = empty;

        RebuildPreview();
    }

    private void RebuildPreview()
    {
        if (_state == null)
            return;

        DeleteDummy();

        var profile = _prefs.Preferences?.SelectedCharacter as HumanoidCharacterProfile;
        if (!_prototype.TryIndex(profile?.Species ?? HumanoidCharacterProfile.DefaultSpecies, out var species))
            return;

        _dummy = _entMan.SpawnEntity(species.DollPrototype, MapCoordinates.Nullspace);

        if (profile != null)
            _entMan.System<SharedVisualBodySystem>().ApplyProfileTo(_dummy, profile);

        _entMan.System<StationSpawningSystem>().EquipStartingGear(_dummy, _state.BaseGear);

        var chosen = _state.Items
            .Where(item => _selected.ContainsKey(item.Index))
            .OrderByDescending(item => item.EquipFirst);

        foreach (var item in chosen)
        {
            DressPreview(item);
        }

        _preview.View.SetEntity(_dummy);
    }

    private void DressPreview(DmArenaStoreOption item)
    {
        var inventory = _entMan.System<InventorySystem>();

        foreach (var slot in item.Slots)
        {
            if (!inventory.TryGetSlotEntity(_dummy, slot, out var old))
                continue;

            inventory.TryUnequip(_dummy, slot, silent: true, force: true);
            _entMan.DeleteEntity(old);
        }

        _entMan.System<StationSpawningSystem>().EquipStartingGear(_dummy, item.GearPrototype);

        if (item.InHand || item.Category != _heldCategory || !_entMan.TryGetComponent(_dummy, out HandsComponent? handsComp))
            return;

        var hands = _entMan.System<SharedHandsSystem>();
        var held = _entMan.SpawnEntity(item.SpritePrototype, MapCoordinates.Nullspace);

        if (!hands.TryGetEmptyHand((_dummy, handsComp), out var emptyHand)
            || !hands.TryPickup(_dummy, held, emptyHand, checkActionBlocker: false, handsComp: handsComp))
            _entMan.DeleteEntity(held);
    }

    private void DeleteDummy()
    {
        if (_dummy.IsValid())
            _entMan.DeleteEntity(_dummy);

        _dummy = EntityUid.Invalid;
    }

    protected override void ExitedTree()
    {
        base.ExitedTree();
        DeleteDummy();
    }
}
