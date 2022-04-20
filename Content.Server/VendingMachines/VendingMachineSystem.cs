using Robust.Shared.Audio;
using Robust.Shared.Player;
using System.Linq;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.VendingMachines;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Shared.Acts;
using Content.Shared.Emag.Systems;
using static Content.Shared.VendingMachines.SharedVendingMachineComponent;
using Content.Shared.Throwing;

namespace Content.Server.VendingMachines.systems
{
    public sealed class VendingMachineSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly AccessReaderSystem _accessReader = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!; 
        [Dependency] private readonly ThrowingSystem _throwingSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<VendingMachineComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<VendingMachineComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<VendingMachineComponent, InventorySyncRequestMessage>(OnInventoryRequestMessage);
            SubscribeLocalEvent<VendingMachineComponent, VendingMachineEjectMessage>(OnInventoryEjectMessage);
            SubscribeLocalEvent<VendingMachineComponent, BreakageEventArgs>(OnBreak);
            SubscribeLocalEvent<VendingMachineComponent, GotEmaggedEvent>(OnEmagged);
        }

        private void OnComponentInit(EntityUid uid, VendingMachineComponent component, ComponentInit args)
        {
            base.Initialize();

            if (TryComp<ApcPowerReceiverComponent>(component.Owner, out var receiver))
            {
                TryUpdateVisualState(uid, null, component);
            }

            InitializeFromPrototype(uid, component);
        }

        private void OnInventoryRequestMessage(EntityUid uid, VendingMachineComponent component, InventorySyncRequestMessage args)
        {
            if (!IsPowered(uid, component))
                return;

            component.UserInterface?.SendMessage(new VendingMachineInventoryMessage(component.Inventory));
        }

        private void OnInventoryEjectMessage(EntityUid uid, VendingMachineComponent component, VendingMachineEjectMessage args)
        {
            if (!IsPowered(uid, component))
                return;

            if (args.Session.AttachedEntity is not { Valid: true } entity || Deleted(entity))
                return;

            AuthorizedVend(uid, entity, args.ID, component);
        }

        private void OnPowerChanged(EntityUid uid, VendingMachineComponent component, PowerChangedEvent args)
        {
            TryUpdateVisualState(uid, null, component);
        }

        private void OnBreak(EntityUid uid, VendingMachineComponent vendComponent, BreakageEventArgs eventArgs)
        {
            vendComponent.Broken = true;
            TryUpdateVisualState(uid, VendingMachineVisualState.Broken, vendComponent);
        }

        private void OnEmagged(EntityUid uid, VendingMachineComponent component, GotEmaggedEvent args)
        {
            if (component.Emagged || component.EmagPackPrototypeId == string.Empty)
                return;

            AddVendEntries(component, component.EmagPackPrototypeId);
            component.Emagged = true;
            args.Handled = true;
        }

        public bool IsPowered(EntityUid uid, VendingMachineComponent? vendComponent = null)
        {
            if (!Resolve(uid, ref vendComponent))
                return false;

            if (!TryComp<ApcPowerReceiverComponent>(vendComponent.Owner, out var receiver))
            {
                return false;
            }
            return receiver.Powered;
        }

        public void InitializeFromPrototype(EntityUid uid, VendingMachineComponent? vendComponent = null)
        {
            if (!Resolve(uid, ref vendComponent))
                return;

            if (string.IsNullOrEmpty(vendComponent.PackPrototypeId)) { return; }

            if (!_prototypeManager.TryIndex(vendComponent.PackPrototypeId, out VendingMachineInventoryPrototype? packPrototype))
            {
                return;
            }

            MetaData(uid).EntityName = packPrototype.Name;
            vendComponent.AnimationDuration = TimeSpan.FromSeconds(packPrototype.AnimationDuration);
            vendComponent.SpriteName = packPrototype.SpriteName;
            if (!string.IsNullOrEmpty(vendComponent.SpriteName))
            {
                if (TryComp<SpriteComponent>(vendComponent.Owner, out var spriteComp)) {
                    const string vendingMachineRSIPath = "Structures/Machines/VendingMachines/{0}.rsi";
                    spriteComp.BaseRSIPath = string.Format(vendingMachineRSIPath, vendComponent.SpriteName);
                }
            }
            var inventory = new List<VendingMachineInventoryEntry>();
            foreach (var (id, amount) in packPrototype.StartingInventory)
            {
                if (!_prototypeManager.TryIndex(id, out EntityPrototype? prototype))
                {
                    continue;
                }
                inventory.Add(new VendingMachineInventoryEntry(id, amount));
            }
            vendComponent.Inventory = inventory;
        }

        /// <summary>
        /// Add more entries for any reason AFTER initialization (emag, machine upgrades, etc)
        /// </summary>
        public void AddVendEntries(VendingMachineComponent component, string pack)
        {
            if (!_prototypeManager.TryIndex(pack, out VendingMachineInventoryPrototype? packPrototype))
            {
                Logger.Error($"Pack has no valid inventory prototype: {pack}");
                return;
            }

            foreach (var (id, amount) in packPrototype.StartingInventory)
            {
                if (!_prototypeManager.TryIndex(id, out EntityPrototype? prototype))
                {
                    continue;
                }
                component.Inventory.Add(new VendingMachineInventoryEntry(id, amount));
            }
        }

        public void Deny(EntityUid uid, VendingMachineComponent? vendComponent = null)
        {
            if (!Resolve(uid, ref vendComponent))
                return;

            SoundSystem.Play(Filter.Pvs(vendComponent.Owner), vendComponent.SoundDeny.GetSound(), vendComponent.Owner, AudioParams.Default.WithVolume(-2f));
            // Play the Deny animation
            TryUpdateVisualState(uid, VendingMachineVisualState.Deny, vendComponent);
            //TODO: This duration should be a distinct value specific to the deny animation
            vendComponent.Owner.SpawnTimer(vendComponent.AnimationDuration, () =>
            {
                TryUpdateVisualState(uid, VendingMachineVisualState.Normal, vendComponent);
            });
        }

        public bool IsAuthorized(EntityUid uid, EntityUid? sender, VendingMachineComponent? vendComponent = null)
        {
            if (!Resolve(uid, ref vendComponent))
                return false;

            if (TryComp<AccessReaderComponent?>(vendComponent.Owner, out var accessReader))
            {
                if (sender == null || !_accessReader.IsAllowed(accessReader, sender.Value))
                {
                    _popupSystem.PopupEntity(Loc.GetString("vending-machine-component-try-eject-access-denied"), uid, Filter.Pvs(uid));
                    Deny(uid, vendComponent);
                    return false;
                }
            }
            return true;
        }

        public void TryEjectVendorItem(EntityUid uid, string itemId, bool throwItem, VendingMachineComponent? vendComponent = null)
        {
            if (!Resolve(uid, ref vendComponent))
                return;

            if (vendComponent.Ejecting || vendComponent.Broken || !IsPowered(uid, vendComponent))
            {
                return;
            }

            var entry = vendComponent.Inventory.Find(x => x.ID == itemId);
            if (entry == null)
            {
                _popupSystem.PopupEntity(Loc.GetString("vending-machine-component-try-eject-invalid-item"), uid, Filter.Pvs(uid));
                Deny(uid, vendComponent);
                return;
            }

            if (entry.Amount <= 0)
            {
                _popupSystem.PopupEntity(Loc.GetString("vending-machine-component-try-eject-out-of-stock"), uid, Filter.Pvs(uid));
                Deny(uid, vendComponent);
                return;
            }

            if (entry.ID == null)
                return;

            if (!TryComp<TransformComponent>(vendComponent.Owner, out var transformComp))
                return;

            // Start Ejecting, and prevent users from ordering while anim playing
            vendComponent.Ejecting = true;
            entry.Amount--;
            vendComponent.UserInterface?.SendMessage(new VendingMachineInventoryMessage(vendComponent.Inventory));
            TryUpdateVisualState(uid, VendingMachineVisualState.Eject, vendComponent);
            vendComponent.Owner.SpawnTimer(vendComponent.AnimationDuration, () =>
            {
                vendComponent.Ejecting = false;
                TryUpdateVisualState(uid, VendingMachineVisualState.Normal, vendComponent);
                var ent = EntityManager.SpawnEntity(entry.ID, transformComp.Coordinates);
                if (throwItem)
                {
                    float range = vendComponent.NonLimitedEjectRange;
                    Vector2 direction = new Vector2(_random.NextFloat(-range, range), _random.NextFloat(-range, range));
                    _throwingSystem.TryThrow(ent, direction, vendComponent.NonLimitedEjectForce);
                }
            });
            SoundSystem.Play(Filter.Pvs(vendComponent.Owner), vendComponent.SoundVend.GetSound(), vendComponent.Owner, AudioParams.Default.WithVolume(-2f));
        }

        public void AuthorizedVend(EntityUid uid, EntityUid sender, string itemId, VendingMachineComponent component)
        {
            if (IsAuthorized(uid, sender, component))
            {
                TryEjectVendorItem(uid, itemId, component.CanShoot, component);
            }
            return;
        }

        public void TryUpdateVisualState(EntityUid uid, VendingMachineVisualState? state = VendingMachineVisualState.Normal, VendingMachineComponent? vendComponent = null)
        {
            if (!Resolve(uid, ref vendComponent))
                return;

            var finalState = state == null ? VendingMachineVisualState.Normal : state;
            if (vendComponent.Broken)
            {
                finalState = VendingMachineVisualState.Broken;
            }
            else if (vendComponent.Ejecting)
            {
                finalState = VendingMachineVisualState.Eject;
            }
            else if (!IsPowered(uid, vendComponent))
            {
                finalState = VendingMachineVisualState.Off;
            }

            if (TryComp<AppearanceComponent>(vendComponent.Owner, out var appearance))
            {
                appearance.SetData(VendingMachineVisuals.VisualState, finalState);
            }
        }

        public void EjectRandom(EntityUid uid, bool throwItem, VendingMachineComponent? vendComponent = null)
        {
            if (!Resolve(uid, ref vendComponent))
                return;

            var availableItems = vendComponent.Inventory.Where(x => x.Amount > 0).ToList();
            if (availableItems.Count <= 0)
            {
                return;
            }

            TryEjectVendorItem(uid, _random.Pick(availableItems).ID, throwItem, vendComponent);
        }
    }
}
