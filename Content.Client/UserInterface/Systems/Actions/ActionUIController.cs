﻿using System.Linq;
using System.Runtime.InteropServices;
using Content.Client.Actions;
using Content.Client.DragDrop;
using Content.Client.Gameplay;
using Content.Client.Hands;
using Content.Client.Outline;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.Actions.Controls;
using Content.Client.UserInterface.Systems.Actions.Widgets;
using Content.Client.UserInterface.Systems.Actions.Windows;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Input;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Client.Utility;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using static Content.Client.Actions.ActionsSystem;
using static Content.Client.UserInterface.Systems.Actions.Windows.ActionsWindow;
using static Robust.Client.UserInterface.Control;
using static Robust.Client.UserInterface.Controls.BaseButton;
using static Robust.Client.UserInterface.Controls.LineEdit;
using static Robust.Client.UserInterface.Controls.MultiselectOptionButton<
    Content.Client.UserInterface.Systems.Actions.Windows.ActionsWindow.Filters>;
using static Robust.Client.UserInterface.Controls.TextureRect;
using static Robust.Shared.Input.Binding.PointerInputCmdHandler;

namespace Content.Client.UserInterface.Systems.Actions;

public sealed class ActionUIController : UIController, IOnStateChanged<GameplayState>, IOnSystemChanged<ActionsSystem>
{
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly IOverlayManager _overlays = default!;

    [UISystemDependency] private readonly ActionsSystem _actionsSystem = default!;
    [UISystemDependency] private readonly InteractionOutlineSystem _interactionOutline = default!;
    [UISystemDependency] private readonly TargetOutlineSystem _targetOutline = default!;

    private const int DefaultPageIndex = 0;
    private ActionButtonContainer? _container;
    private readonly List<ActionPage> _pages = new();
    private int _currentPageIndex = DefaultPageIndex;
    private readonly DragDropHelper<ActionButton> _menuDragHelper;
    private readonly TextureRect _dragShadow;
    private ActionsWindow? _window;

    private ActionsBar? _actionsBar;
    private MenuButton? _actionButton;
    private ActionPage CurrentPage => _pages[_currentPageIndex];

    public bool IsDragging => _menuDragHelper.IsDragging;

    /// <summary>
    /// Action slot we are currently selecting a target for.
    /// </summary>
    public ActionButton? SelectingTargetFor { get; private set; }

    public ActionUIController()
    {
        _menuDragHelper = new DragDropHelper<ActionButton>(OnMenuBeginDrag, OnMenuContinueDrag, OnMenuEndDrag);
        _dragShadow = new TextureRect
        {
            MinSize = (64, 64),
            Stretch = StretchMode.Scale,
            Visible = false,
            SetSize = (64, 64),
            MouseFilter = MouseFilterMode.Ignore
        };

        var pageCount = ContentKeyFunctions.GetLoadoutBoundKeys().Length;
        var buttonCount = ContentKeyFunctions.GetHotbarBoundKeys().Length;
        for (var i = 0; i < pageCount; i++)
        {
            var page = new ActionPage(buttonCount);
            _pages.Add(page);
        }
    }

    public void OnStateEntered(GameplayState state)
    {
        DebugTools.Assert(_window == null);

        _window = UIManager.CreateWindow<ActionsWindow>();
        _actionButton = UIManager.GetActiveUIWidget<MenuBar.Widgets.GameTopMenuBar>().ActionButton;
        _actionsBar = UIManager.GetActiveUIWidget<ActionsBar>();
        LayoutContainer.SetAnchorPreset(_window, LayoutContainer.LayoutPreset.CenterTop);

        _window.OnOpen += OnWindowOpened;
        _window.OnClose += OnWindowClosed;
        _window.ClearButton.OnPressed += OnClearPressed;
        _window.SearchBar.OnTextChanged += OnSearchChanged;
        _window.FilterButton.OnItemSelected += OnFilterSelected;
        _actionButton.OnPressed += ActionButtonPressed;
        _actionsBar.PageButtons.LeftArrow.OnPressed += OnLeftArrowPressed;
        _actionsBar.PageButtons.RightArrow.OnPressed += OnRightArrowPressed;
        _actionsSystem.ActionReplaced += OnActionReplaced;
        _actionsSystem.ActionsUpdated += OnActionsUpdated;

        UpdateFilterLabel();
        SearchAndDisplay();

        _dragShadow.Orphan();
        UIManager.PopupRoot.AddChild(_dragShadow);

        var builder = CommandBinds.Builder;
        var hotbarKeys = ContentKeyFunctions.GetHotbarBoundKeys();
        for (var i = 0; i < hotbarKeys.Length; i++)
        {
            var boundId = i; // This is needed, because the lambda captures it.
            var boundKey = hotbarKeys[i];
            builder = builder.Bind(boundKey, new PointerInputCmdHandler((in PointerInputCmdArgs args) =>
            {
                if (args.State != BoundKeyState.Up)
                    return false;

                TriggerAction(boundId);
                return true;
            }, false));
        }

        var loadoutKeys = ContentKeyFunctions.GetLoadoutBoundKeys();
        for (var i = 0; i < loadoutKeys.Length; i++)
        {
            var boundId = i; // This is needed, because the lambda captures it.
            var boundKey = loadoutKeys[i];
            builder = builder.Bind(boundKey,
                InputCmdHandler.FromDelegate(_ => ChangePage(boundId)));
        }

        builder
            .Bind(ContentKeyFunctions.OpenActionsMenu,
                InputCmdHandler.FromDelegate(_ => ToggleWindow()))
            .Register<ActionUIController>();
    }

    private void OnWindowOpened()
    {
        if (_actionButton != null)
            _actionButton.Pressed = true;
    }

    private void OnWindowClosed()
    {
        if (_actionButton != null)
            _actionButton.Pressed = false;
    }

    public void OnStateExited(GameplayState state)
    {
        _actionsSystem.ActionReplaced -= OnActionReplaced;
        _actionsSystem.ActionsUpdated -= OnActionsUpdated;

        if (_window != null)
        {
            _window.OnOpen += OnWindowOpened;
            _window.OnClose += OnWindowClosed;
            _window.ClearButton.OnPressed += OnClearPressed;
            _window.SearchBar.OnTextChanged += OnSearchChanged;
            _window.FilterButton.OnItemSelected += OnFilterSelected;

            _window.Dispose();
            _window = null;
        }

        if (_actionsBar != null)
        {
            _actionsBar.PageButtons.LeftArrow.OnPressed += OnLeftArrowPressed;
            _actionsBar.PageButtons.RightArrow.OnPressed += OnRightArrowPressed;
        }

        if (_actionButton != null)
        {
            _actionButton.OnPressed -= ActionButtonPressed;
            _actionButton.Pressed = false;
        }

        CommandBinds.Unregister<ActionUIController>();
    }

    private void TriggerAction(int index)
    {
        if (CurrentPage[index] is not { } type)
            return;

        _actionsSystem.TriggerAction(type);
    }

    private void ChangePage(int index)
    {
        var lastPage = _pages.Count - 1;
        if (index < 0)
        {
            index = lastPage;
        }
        else if (index > lastPage)
        {
            index = 0;
        }

        _currentPageIndex = index;
        var page = _pages[_currentPageIndex];
        _container?.SetActionData(page);

        _actionsBar!.PageButtons.Label.Text = $"{_currentPageIndex + 1}";
    }

    private void OnLeftArrowPressed(ButtonEventArgs args)
    {
        ChangePage(_currentPageIndex - 1);
    }

    private void OnRightArrowPressed(ButtonEventArgs args)
    {
        ChangePage(_currentPageIndex + 1);
    }

    private void OnActionReplaced(ActionType existing, ActionType action)
    {
        if (_container == null)
            return;

        foreach (var button in _container.GetButtons())
        {
            if (button.Action == existing)
                button.UpdateData(action);
        }
    }

    private void OnActionsUpdated()
    {
        if (_container == null)
            return;

        foreach (var button in _container.GetButtons())
        {
            button.UpdateIcons();
        }
    }

    private void ActionButtonPressed(ButtonEventArgs args)
    {
        ToggleWindow();
    }

    private void ToggleWindow()
    {
        if (_window == null)
            return;

        if (_window.IsOpen)
        {
            _window.Close();
            return;
        }

        _window.Open();
    }

    private void UpdateFilterLabel()
    {
        if (_window == null)
            return;

        if (_window.FilterButton.SelectedKeys.Count == 0)
        {
            _window.FilterLabel.Visible = false;
        }
        else
        {
            _window.FilterLabel.Visible = true;
            _window.FilterLabel.Text = Loc.GetString("ui-actionmenu-filter-label",
                ("selectedLabels", string.Join(", ", _window.FilterButton.SelectedLabels)));
        }
    }

    private bool MatchesFilter(ActionType action, Filters filter)
    {
        return filter switch
        {
            Filters.Enabled => action.Enabled,
            Filters.Item => action.Provider != null && action.Provider != _actionsSystem.PlayerActions?.Owner,
            Filters.Innate => action.Provider == null || action.Provider == _actionsSystem.PlayerActions?.Owner,
            Filters.Instant => action is InstantAction,
            Filters.Targeted => action is TargetedAction,
            _ => throw new ArgumentOutOfRangeException(nameof(filter), filter, null)
        };
    }

    private void ClearList()
    {
        _window?.ResultsGrid.RemoveAllChildren();
    }

    private void PopulateActions(IEnumerable<ActionType> actions)
    {
        if (_window == null)
            return;

        ClearList();

        foreach (var action in actions)
        {
            var button = new ActionButton {Locked = true};

            button.UpdateData(action);
            button.ActionPressed += OnWindowActionPressed;
            button.ActionUnpressed += OnWindowActionUnPressed;
            button.ActionFocusExited += OnWindowActionFocusExisted;

            _window.ResultsGrid.AddChild(button);
        }
    }

    private void SearchAndDisplay()
    {
        if (_window == null)
            return;

        var search = _window.SearchBar.Text;
        var filters = _window.FilterButton.SelectedKeys;

        IEnumerable<ActionType>? actions = _actionsSystem.PlayerActions?.Actions;
        actions ??= Array.Empty<ActionType>();

        if (filters.Count == 0 && string.IsNullOrWhiteSpace(search))
        {
            PopulateActions(actions);
            return;
        }

        actions = actions.Where(action =>
        {
            if (filters.Count > 0 && filters.Any(filter => !MatchesFilter(action, filter)))
                return false;

            if (action.Keywords.Any(keyword => search.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                return true;

            if (action.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase))
                return true;

            if (action.Provider == null || action.Provider == _actionsSystem.PlayerActions?.Owner)
                return false;

            var name = _entities.GetComponent<MetaDataComponent>(action.Provider.Value).EntityName;
            return name.Contains(search, StringComparison.OrdinalIgnoreCase);
        });

        PopulateActions(actions);
    }

    private void SetAction(ActionButton button, ActionType? type)
    {
        int position;

        if (type == null)
        {
            button.ClearData();
            if (_container?.TryGetButtonIndex(button, out position) ?? false)
            {
                CurrentPage[position] = type;
            }

            return;
        }

        if (button.TryReplaceWith(type) &&
            _container != null &&
            _container.TryGetButtonIndex(button, out position))
        {
            CurrentPage[position] = type;
        }
    }

    private void DragAction()
    {
        if (UIManager.CurrentlyHovered is ActionButton button)
        {
            if (!_menuDragHelper.IsDragging || _menuDragHelper.Dragged?.Action is not { } type)
            {
                _menuDragHelper.EndDrag();
                return;
            }

            SetAction(button, type);
        }

        if (_menuDragHelper.Dragged is {Parent: ActionButtonContainer} old)
        {
            SetAction(old, null);
        }

        _menuDragHelper.EndDrag();
    }

    private void OnClearPressed(ButtonEventArgs args)
    {
        if (_window == null)
            return;

        _window.SearchBar.Clear();
        _window.FilterButton.DeselectAll();
        UpdateFilterLabel();
        SearchAndDisplay();
    }

    private void OnSearchChanged(LineEditEventArgs args)
    {
        SearchAndDisplay();
    }

    private void OnFilterSelected(ItemPressedEventArgs args)
    {
        UpdateFilterLabel();
        SearchAndDisplay();
    }

    private void OnWindowActionPressed(GUIBoundKeyEventArgs args, ActionButton action)
    {
        if (args.Function != EngineKeyFunctions.UIClick && args.Function != EngineKeyFunctions.Use)
            return;

        _menuDragHelper.MouseDown(action);
        args.Handle();
    }

    private void OnWindowActionUnPressed(GUIBoundKeyEventArgs args, ActionButton dragged)
    {
        if (args.Function != EngineKeyFunctions.UIClick && args.Function != EngineKeyFunctions.Use)
            return;

        DragAction();
        args.Handle();
    }

    private void OnWindowActionFocusExisted(ActionButton button)
    {
        _menuDragHelper.EndDrag();
    }

    private void OnActionPressed(GUIBoundKeyEventArgs args, ActionButton button)
    {
        if (args.Function == EngineKeyFunctions.UIClick)
        {
            _menuDragHelper.MouseDown(button);
            args.Handle();
        }
        else if (args.Function == EngineKeyFunctions.UIRightClick)
        {
            SetAction(button, null);
            args.Handle();
        }
    }

    private void OnActionUnpressed(GUIBoundKeyEventArgs args, ActionButton button)
    {
        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        if (UIManager.CurrentlyHovered == button)
        {
            if (button.Action is not InstantAction)
            {
                // for target actions, we go into "select target" mode, we don't
                // message the server until we actually pick our target.

                // if we're clicking the same thing we're already targeting for, then we simply cancel
                // targeting
                ToggleTargeting(button);
                return;
            }

            _actionsSystem.TriggerAction(button.Action);
            _menuDragHelper.EndDrag();
        }
        else
        {
            DragAction();
        }

        args.Handle();
    }

    private bool OnMenuBeginDrag()
    {
        if (_menuDragHelper.Dragged?.Action is { } action)
        {
            if (action.EntityIcon != null)
            {
                _dragShadow.Texture = _entities.GetComponent<SpriteComponent>(action.EntityIcon.Value).Icon?
                    .GetFrame(RSI.State.Direction.South, 0);
            }
            else if (action.Icon != null)
            {
                _dragShadow.Texture = action.Icon!.Frame0();
            }
            else
            {
                _dragShadow.Texture = null;
            }
        }

        LayoutContainer.SetPosition(_dragShadow, UIManager.MousePositionScaled.Position - (32, 32));
        return true;
    }

    private bool OnMenuContinueDrag(float frameTime)
    {
        LayoutContainer.SetPosition(_dragShadow, UIManager.MousePositionScaled.Position - (32, 32));
        _dragShadow.Visible = true;
        return true;
    }

    private void OnMenuEndDrag()
    {
        _dragShadow.Texture = null;
        _dragShadow.Visible = false;
    }

    public void RegisterActionContainer(ActionButtonContainer container)
    {
        if (_container != null)
        {
            Logger.Warning("Action container already defined for UI controller");
            return;
        }

        _container = container;
        _container.ActionPressed += OnActionPressed;
        _container.ActionUnpressed += OnActionUnpressed;
    }

    public void ClearActions()
    {
        _container?.ClearActionData();
    }

    private void AssignSlots(List<SlotAssignment> assignments)
    {
        foreach (ref var assignment in CollectionsMarshal.AsSpan(assignments))
        {
            _pages[assignment.Hotbar][assignment.Slot] = assignment.Action;
        }

        _container?.SetActionData(_pages[_currentPageIndex]);
    }

    public void RemoveActionContainer()
    {
        _container = null;
    }

    public void OnSystemLoaded(ActionsSystem system)
    {
        _actionsSystem.LinkActions += OnComponentLinked;
        _actionsSystem.UnlinkActions += OnComponentUnlinked;
        _actionsSystem.ClearAssignments += ClearActions;
        _actionsSystem.AssignSlot += AssignSlots;
    }

    public void OnSystemUnloaded(ActionsSystem system)
    {
        _actionsSystem.LinkActions -= OnComponentLinked;
        _actionsSystem.UnlinkActions -= OnComponentUnlinked;
        _actionsSystem.ClearAssignments -= ClearActions;
        _actionsSystem.AssignSlot -= AssignSlots;
    }

    public override void FrameUpdate(FrameEventArgs args)
    {
        _menuDragHelper.Update(args.DeltaSeconds);
    }

    private void OnComponentLinked(ActionsComponent component)
    {
        LoadDefaultActions(component);
        _container?.SetActionData(_pages[DefaultPageIndex]);
    }

    private void OnComponentUnlinked()
    {
        _container?.ClearActionData();
        //TODO: Clear button data
    }

    private void LoadDefaultActions(ActionsComponent component)
    {
        var actions = component.Actions.Where(actionType => actionType.AutoPopulate).ToList();

        var offset = 0;
        var totalPages = _pages.Count;
        var pagesLeft = totalPages;
        var currentPage = DefaultPageIndex;
        while (pagesLeft > 0)
        {
            var page = _pages[currentPage];
            var pageSize = page.Size;

            for (var slot = 0; slot < pageSize; slot++)
            {
                var actionIndex = slot + offset;
                if (actionIndex < actions.Count)
                {
                    page[slot] = actions[slot + offset];
                }
                else
                {
                    page[slot] = null;
                }
            }

            offset += pageSize;
            currentPage++;
            if (currentPage == totalPages)
            {
                currentPage = 0;
            }

            pagesLeft--;
        }
    }

    /// <summary>
    /// If currently targeting with this slot, stops targeting.
    /// If currently targeting with no slot or a different slot, switches to
    /// targeting with the specified slot.
    /// </summary>
    /// <param name="slot"></param>
    public void ToggleTargeting(ActionButton slot)
    {
        if (SelectingTargetFor == slot)
        {
            StopTargeting();
            return;
        }

        StartTargeting(slot);
    }

    /// <summary>
    /// Puts us in targeting mode, where we need to pick either a target point or entity
    /// </summary>
    private void StartTargeting(ActionButton actionSlot)
    {
        if (actionSlot.Action == null)
            return;

        // If we were targeting something else we should stop
        StopTargeting();

        SelectingTargetFor = actionSlot;

        if (actionSlot.Action is not TargetedAction action)
            return;

        // override "held-item" overlay
        if (action.TargetingIndicator && _overlays.TryGetOverlay<ShowHandItemOverlay>(out var handOverlay))
        {
            if (action.ItemIconStyle == ItemActionIconStyle.BigItem && action.Provider != null)
            {
                handOverlay.EntityOverride = action.Provider;
            }
            else if (action.Toggled && action.IconOn != null)
                handOverlay.IconOverride = action.IconOn.Frame0();
            else if (action.Icon != null)
                handOverlay.IconOverride = action.Icon.Frame0();
        }

        // TODO: allow world-targets to check valid positions. E.g., maybe:
        // - Draw a red/green ghost entity
        // - Add a yes/no checkmark where the HandItemOverlay usually is

        // Highlight valid entity targets
        if (action is not EntityTargetAction entityAction)
            return;

        Func<EntityUid, bool>? predicate = null;

        if (!entityAction.CanTargetSelf)
            predicate = e => e != entityAction.AttachedEntity;

        var range = entityAction.CheckCanAccess ? action.Range : -1;

        _interactionOutline.SetEnabled(false);
        _targetOutline.Enable(range, entityAction.CheckCanAccess, predicate, entityAction.Whitelist, null);
    }

    /// <summary>
    /// Switch out of targeting mode if currently selecting target for an action
    /// </summary>
    public void StopTargeting()
    {
        if (SelectingTargetFor == null)
            return;

        SelectingTargetFor = null;
        _targetOutline.Disable();
        _interactionOutline.SetEnabled(true);

        if (!_overlays.TryGetOverlay<ShowHandItemOverlay>(out var handOverlay) || handOverlay == null)
            return;

        handOverlay.IconOverride = null;
        handOverlay.EntityOverride = null;
    }


    //TODO: Serialize this shit
    private sealed class ActionPage
    {
        private readonly ActionType?[] _data;

        public ActionPage(int size)
        {
            _data = new ActionType?[size];
        }

        public ActionType? this[int index]
        {
            get => _data[index];
            set => _data[index] = value;
        }

        public static implicit operator ActionType?[](ActionPage p)
        {
            return p._data.ToArray();
        }

        public void Clear()
        {
            Array.Fill(_data, null);
        }

        public int Size => _data.Length;
    }
}
