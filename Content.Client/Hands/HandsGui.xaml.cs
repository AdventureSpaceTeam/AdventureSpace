using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Content.Client.HUD;
using Content.Client.Items.Managers;
using Content.Client.Items.UI;
using Content.Client.Resources;
using Content.Shared.CCVar;
using Content.Shared.Hands.Components;
using Robust.Client.AutoGenerated;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;

namespace Content.Client.Hands
{
    [GenerateTypedNameReferences]
    public sealed partial class HandsGui : Control
    {
        [Dependency] private readonly IResourceCache _resourceCache = default!;
        [Dependency] private readonly IItemSlotManager _itemSlotManager = default!;
        [Dependency] private readonly IGameHud _gameHud = default!;
        [Dependency] private readonly INetConfigurationManager _configManager = default!;

        private readonly HandsSystem _handsSystem;
        private readonly HandsComponent _handsComponent;

        private Texture StorageTexture => _gameHud.GetHudTexture("back.png");
        private Texture BlockedTexture => _resourceCache.GetTexture("/Textures/Interface/Inventory/blocked.png");

        private ItemStatusPanel StatusPanel { get; }

        [ViewVariables] private GuiHand[] _hands = Array.Empty<GuiHand>();

        private string? ActiveHand { get; set; }

        public HandsGui(HandsComponent hands, HandsSystem handsSystem)
        {
            _handsComponent = hands;
            _handsSystem = handsSystem;

            RobustXamlLoader.Load(this);
            IoCManager.InjectDependencies(this);

            StatusPanel = ItemStatusPanel.FromSide(HandLocation.Middle);
            StatusContainer.AddChild(StatusPanel);
            StatusPanel.SetPositionFirst();
        }

        protected override void EnteredTree()
        {
            base.EnteredTree();

            _handsSystem.GuiStateUpdated += HandsSystemOnGuiStateUpdated;
            _configManager.OnValueChanged(CCVars.HudTheme, UpdateHudTheme);

            HandsSystemOnGuiStateUpdated();
        }

        protected override void ExitedTree()
        {
            base.ExitedTree();

            _handsSystem.GuiStateUpdated -= HandsSystemOnGuiStateUpdated;
            _configManager.UnsubValueChanged(CCVars.HudTheme, UpdateHudTheme);
        }

        private void HandsSystemOnGuiStateUpdated()
        {
            var state = _handsSystem.GetGuiState();

            ActiveHand = state.ActiveHand;
            _hands = state.GuiHands;
            Array.Sort(_hands, HandOrderComparer.Instance);
            UpdateGui();
        }

        private void UpdateGui()
        {
            HandsContainer.DisposeAllChildren();

            foreach (var hand in _hands)
            {
                var newButton = MakeHandButton(hand.HandLocation);
                HandsContainer.AddChild(newButton);
                hand.HandButton = newButton;

                var handName = hand.Name;
                newButton.OnPressed += args => OnHandPressed(args, handName);
                newButton.OnStoragePressed += _ => OnStoragePressed(handName);

                _itemSlotManager.SetItemSlot(newButton, hand.HeldItem);

                // Show blocked overlay if hand is blocked.
                newButton.Blocked.Visible =
                    hand.HeldItem != null && hand.HeldItem.HasComponent<HandVirtualItemComponent>();
            }

            if (TryGetActiveHand(out var activeHand))
            {
                activeHand.HandButton.SetActiveHand(true);
                StatusPanel.Update(activeHand.HeldItem);
            }
        }

        private void OnHandPressed(GUIBoundKeyEventArgs args, string handName)
        {
            if (args.Function == EngineKeyFunctions.UIClick)
            {
                _handsSystem.UIHandClick(_handsComponent, handName);
            }
            else if (TryGetHand(handName, out var hand))
            {
                _itemSlotManager.OnButtonPressed(args, hand.HeldItem);
            }
        }

        private void OnStoragePressed(string handName)
        {
            _handsSystem.UIHandActivate(handName);
        }

        private bool TryGetActiveHand([NotNullWhen(true)] out GuiHand? activeHand)
        {
            TryGetHand(ActiveHand, out activeHand);
            return activeHand != null;
        }

        private bool TryGetHand(string? handName, [NotNullWhen(true)] out GuiHand? foundHand)
        {
            foundHand = null;

            if (handName == null)
                return false;

            foreach (var hand in _hands)
            {
                if (hand.Name == handName)
                    foundHand = hand;
            }

            return foundHand != null;
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);

            foreach (var hand in _hands)
            {
                _itemSlotManager.UpdateCooldown(hand.HandButton, hand.HeldItem);
            }
        }

        private HandButton MakeHandButton(HandLocation buttonLocation)
        {
            var buttonTextureName = buttonLocation switch
            {
                HandLocation.Right => "hand_r.png",
                _ => "hand_l.png"
            };
            var buttonTexture = _gameHud.GetHudTexture(buttonTextureName);

            return new HandButton(buttonTexture, StorageTexture, buttonTextureName, BlockedTexture, buttonLocation);
        }

        private void UpdateHudTheme(int idx)
        {
            UpdateGui();
        }

        private sealed class HandOrderComparer : IComparer<GuiHand>
        {
            public static readonly HandOrderComparer Instance = new();

            public int Compare(GuiHand? x, GuiHand? y)
            {
                if (ReferenceEquals(x, y)) return 0;
                if (ReferenceEquals(null, y)) return 1;
                if (ReferenceEquals(null, x)) return -1;

                var orderX = Map(x.HandLocation);
                var orderY = Map(y.HandLocation);

                return orderX.CompareTo(orderY);

                static int Map(HandLocation loc)
                {
                    return loc switch
                    {
                        HandLocation.Left => 3,
                        HandLocation.Middle => 2,
                        HandLocation.Right => 1,
                        _ => throw new ArgumentOutOfRangeException(nameof(loc), loc, null)
                    };
                }
            }
        }
    }

    /// <summary>
    ///     Info on a set of hands to be displayed.
    /// </summary>
    public class HandsGuiState
    {
        /// <summary>
        ///     The set of hands to be displayed.
        /// </summary>
        [ViewVariables]
        public GuiHand[] GuiHands { get; }

        /// <summary>
        ///     The name of the currently active hand.
        /// </summary>
        [ViewVariables]
        public string? ActiveHand { get; }

        public HandsGuiState(GuiHand[] guiHands, string? activeHand = null)
        {
            GuiHands = guiHands;
            ActiveHand = activeHand;
        }
    }

    /// <summary>
    ///     Info on an individual hand to be displayed.
    /// </summary>
    public class GuiHand
    {
        /// <summary>
        ///     The name of this hand.
        /// </summary>
        [ViewVariables]
        public string Name { get; }

        /// <summary>
        ///     Where this hand is located.
        /// </summary>
        [ViewVariables]
        public HandLocation HandLocation { get; }

        /// <summary>
        ///     The item being held in this hand.
        /// </summary>
        [ViewVariables]
        public IEntity? HeldItem { get; }

        /// <summary>
        ///     The button in the gui associated with this hand. Assumed to be set by gui shortly after being received from the client HandsComponent.
        /// </summary>
        [ViewVariables]
        public HandButton HandButton { get; set; } = default!;

        public GuiHand(string name, HandLocation handLocation, IEntity? heldItem)
        {
            Name = name;
            HandLocation = handLocation;
            HeldItem = heldItem;
        }
    }
}
