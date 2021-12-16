using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Hands.Components;
using Content.Server.Power.Components;
using Content.Server.UserInterface;
using Content.Shared.ActionBlocker;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Dispenser;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Sound;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Chemistry.Components
{
    /// <summary>
    /// Contains all the server-side logic for reagent dispensers. See also <see cref="SharedReagentDispenserComponent"/>.
    /// This includes initializing the component based on prototype data, and sending and receiving messages from the client.
    /// Messages sent to the client are used to update update the user interface for a component instance.
    /// Messages sent from the client are used to handle ui button presses.
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(SharedReagentDispenserComponent))]
    public class ReagentDispenserComponent : SharedReagentDispenserComponent, IActivate
    {
        private static ReagentInventoryComparer _comparer = new();
        public static string SolutionName = "reagent";

        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IEntityManager _entities = default!;

        [ViewVariables] [DataField("pack")] private string _packPrototypeId = "";

        [DataField("clickSound")]
        private SoundSpecifier _clickSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");

        [ViewVariables] private FixedPoint2 _dispenseAmount = FixedPoint2.New(10);

        [UsedImplicitly]
        [ViewVariables]
        private Solution? Solution
        {
            get
            {
                EntitySystem.Get<SolutionContainerSystem>().TryGetSolution(Owner, SolutionName, out var solution);
                return solution;
            }
        }

        [ViewVariables]
        private bool Powered => !_entities.TryGetComponent(Owner, out ApcPowerReceiverComponent? receiver) || receiver.Powered;

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(ReagentDispenserUiKey.Key);

        /// <summary>
        /// Called once per instance of this component. Gets references to any other components needed
        /// by this component and initializes it's UI and other data.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += OnUiReceiveMessage;
            }

            InitializeFromPrototype();
        }

        [Obsolete("Component Messages are deprecated, use Entity Events instead.")]
        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
#pragma warning disable 618
            base.HandleMessage(message, component);
#pragma warning restore 618
            switch (message)
            {
                case PowerChangedMessage powerChanged:
                    OnPowerChanged(powerChanged);
                    break;
            }
        }

        /// <summary>
        /// Checks to see if the <c>pack</c> defined in this components yaml prototype
        /// exists. If so, it fills the reagent inventory list.
        /// </summary>
        private void InitializeFromPrototype()
        {
            if (string.IsNullOrEmpty(_packPrototypeId)) return;

            if (!_prototypeManager.TryIndex(_packPrototypeId, out ReagentDispenserInventoryPrototype? packPrototype))
            {
                return;
            }

            foreach (var entry in packPrototype.Inventory)
            {
                Inventory.Add(new ReagentDispenserInventoryEntry(entry));
            }

            Inventory.Sort(_comparer);
        }

        private void OnPowerChanged(PowerChangedMessage e)
        {
            UpdateUserInterface();
        }

        /// <summary>
        /// Handles ui messages from the client. For things such as button presses
        /// which interact with the world and require server action.
        /// </summary>
        /// <param name="obj">A user interface message from the client.</param>
        private void OnUiReceiveMessage(ServerBoundUserInterfaceMessage obj)
        {
            if (obj.Session.AttachedEntity == null)
            {
                return;
            }

            var msg = (UiButtonPressedMessage) obj.Message;
            var needsPower = msg.Button switch
            {
                UiButton.Eject => false,
                _ => true,
            };

            if (!PlayerCanUseDispenser(obj.Session.AttachedEntity, needsPower))
                return;

            switch (msg.Button)
            {
                case UiButton.Eject:
                    EntitySystem.Get<ItemSlotsSystem>().TryEjectToHands(Owner, BeakerSlot, obj.Session.AttachedEntity);
                    break;
                case UiButton.Clear:
                    TryClear();
                    break;
                case UiButton.SetDispenseAmount1:
                    _dispenseAmount = FixedPoint2.New(1);
                    break;
                case UiButton.SetDispenseAmount5:
                    _dispenseAmount = FixedPoint2.New(5);
                    break;
                case UiButton.SetDispenseAmount10:
                    _dispenseAmount = FixedPoint2.New(10);
                    break;
                case UiButton.SetDispenseAmount15:
                    _dispenseAmount = FixedPoint2.New(15);
                    break;
                case UiButton.SetDispenseAmount20:
                    _dispenseAmount = FixedPoint2.New(20);
                    break;
                case UiButton.SetDispenseAmount25:
                    _dispenseAmount = FixedPoint2.New(25);
                    break;
                case UiButton.SetDispenseAmount30:
                    _dispenseAmount = FixedPoint2.New(30);
                    break;
                case UiButton.SetDispenseAmount50:
                    _dispenseAmount = FixedPoint2.New(50);
                    break;
                case UiButton.SetDispenseAmount100:
                    _dispenseAmount = FixedPoint2.New(100);
                    break;
                case UiButton.Dispense:
                    if (BeakerSlot.HasItem)
                    {
                        TryDispense(msg.DispenseIndex);
                    }
                    Logger.Info($"User {obj.Session.UserId.UserId} ({obj.Session.Name}) dispensed {_dispenseAmount}u of {Inventory[msg.DispenseIndex].ID}");

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            ClickSound();
        }

        /// <summary>
        /// Checks whether the player entity is able to use the chem dispenser.
        /// </summary>
        /// <param name="playerEntity">The player entity.</param>
        /// <returns>Returns true if the entity can use the dispenser, and false if it cannot.</returns>
        private bool PlayerCanUseDispenser(EntityUid? playerEntity, bool needsPower = true)
        {
            //Need player entity to check if they are still able to use the dispenser
            if (playerEntity == null)
                return false;

            var actionBlocker = EntitySystem.Get<ActionBlockerSystem>();

            //Check if player can interact in their current state
            if (!actionBlocker.CanInteract(playerEntity.Value) || !actionBlocker.CanUse(playerEntity.Value))
                return false;
            //Check if device is powered
            if (needsPower && !Powered)
                return false;

            return true;
        }

        /// <summary>
        /// Gets component data to be used to update the user interface client-side.
        /// </summary>
        /// <returns>Returns a <see cref="SharedReagentDispenserComponent.ReagentDispenserBoundUserInterfaceState"/></returns>
        private ReagentDispenserBoundUserInterfaceState GetUserInterfaceState()
        {
            if (BeakerSlot.Item is not {Valid: true} beaker ||
                !_entities.TryGetComponent(beaker, out FitsInDispenserComponent? fits) ||
                !EntitySystem.Get<SolutionContainerSystem>().TryGetSolution(beaker, fits.Solution, out var solution))
            {
                return new ReagentDispenserBoundUserInterfaceState(Powered, false, FixedPoint2.New(0),
                    FixedPoint2.New(0),
                    string.Empty, Inventory, _entities.GetComponent<MetaDataComponent>(Owner).EntityName, null, _dispenseAmount);
            }

            return new ReagentDispenserBoundUserInterfaceState(Powered, true, solution.CurrentVolume,
                solution.MaxVolume,
                _entities.GetComponent<MetaDataComponent>(beaker).EntityName, Inventory, _entities.GetComponent<MetaDataComponent>(Owner).EntityName, solution.Contents.ToList(), _dispenseAmount);
        }

        public void UpdateUserInterface()
        {
            if (!Initialized) return;

            var state = GetUserInterfaceState();
            UserInterface?.SetState(state);
        }

        /// <summary>
        /// If this component contains an entity with a <see cref="SolutionHolder"/>, remove all of it's reagents / solutions.
        /// </summary>
        private void TryClear()
        {
            if (BeakerSlot.Item is not {Valid: true} beaker ||
                !_entities.TryGetComponent(beaker, out FitsInDispenserComponent? fits) ||
                !EntitySystem.Get<SolutionContainerSystem>().TryGetSolution(beaker, fits.Solution, out var solution))
                return;

            EntitySystem.Get<SolutionContainerSystem>().RemoveAllSolution(beaker, solution);

            UpdateUserInterface();
        }

        /// <summary>
        /// If this component contains an entity with a <see cref="SolutionHolder"/>, attempt to dispense the specified reagent to it.
        /// </summary>
        /// <param name="dispenseIndex">The index of the reagent in <c>Inventory</c>.</param>
        private void TryDispense(int dispenseIndex)
        {
            if (BeakerSlot.Item is not {Valid: true} beaker ||
                !_entities.TryGetComponent(beaker, out FitsInDispenserComponent? fits) ||
                !EntitySystem.Get<SolutionContainerSystem>().TryGetSolution(beaker, fits.Solution, out var solution)) return;

            EntitySystem.Get<SolutionContainerSystem>()
                .TryAddReagent(beaker, solution, Inventory[dispenseIndex].ID, _dispenseAmount, out _);

            UpdateUserInterface();
        }

        /// <summary>
        /// Called when you click the owner entity with an empty hand. Opens the UI client-side if possible.
        /// </summary>
        /// <param name="args">Data relevant to the event such as the actor which triggered it.</param>
        void IActivate.Activate(ActivateEventArgs args)
        {
            if (!_entities.TryGetComponent(args.User, out ActorComponent? actor))
            {
                return;
            }

            if (!_entities.TryGetComponent(args.User, out HandsComponent? hands))
            {
                Owner.PopupMessage(args.User, Loc.GetString("reagent-dispenser-component-activate-no-hands"));
                return;
            }

            var activeHandEntity = hands.GetActiveHand?.Owner;
            if (activeHandEntity == null)
            {
                UserInterface?.Open(actor.PlayerSession);
            }
        }

        private void ClickSound()
        {
            SoundSystem.Play(Filter.Pvs(Owner), _clickSound.GetSound(), Owner, AudioParams.Default.WithVolume(-2f));
        }

        private class ReagentInventoryComparer : Comparer<ReagentDispenserInventoryEntry>
        {
            public override int Compare(ReagentDispenserInventoryEntry x, ReagentDispenserInventoryEntry y)
            {
                return string.Compare(x.ID, y.ID, StringComparison.InvariantCultureIgnoreCase);
            }
        }
    }
}
