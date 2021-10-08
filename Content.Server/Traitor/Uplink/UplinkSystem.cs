using Content.Server.Hands.Components;
using Content.Server.Inventory.Components;
using Content.Server.Items;
using Content.Server.PDA;
using Content.Server.Traitor.Uplink.Account;
using Content.Server.Traitor.Uplink.Components;
using Content.Server.UserInterface;
using Content.Shared.Traitor.Uplink;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Player;
using System;
using System.Linq;

namespace Content.Server.Traitor.Uplink
{
    public class UplinkSystem : EntitySystem
    {
        [Dependency]
        private readonly UplinkAccountsSystem _accounts = default!;
        [Dependency]
        private readonly UplinkListingSytem _listing = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<UplinkComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<UplinkComponent, ComponentRemove>(OnRemove);
            SubscribeLocalEvent<UplinkComponent, UplinkBuyListingMessage>(OnBuy);
            SubscribeLocalEvent<UplinkComponent, UplinkRequestUpdateInterfaceMessage>(OnRequestUpdateUI);

            SubscribeLocalEvent<UplinkAccountBalanceChanged>(OnBalanceChangedBroadcast);
        }

        public void SetAccount(UplinkComponent component, UplinkAccount account)
        {
            if (component.UplinkAccount != null)
            {
                Logger.Error("Can't init one uplink with different account!");
                return;
            }

            component.UplinkAccount = account;
        }

        private void OnInit(EntityUid uid, UplinkComponent component, ComponentInit args)
        {
            RaiseLocalEvent(uid, new UplinkInitEvent(component));
        }

        private void OnRemove(EntityUid uid, UplinkComponent component, ComponentRemove args)
        {
            RaiseLocalEvent(uid, new UplinkRemovedEvent());
        }

        private void OnBalanceChangedBroadcast(UplinkAccountBalanceChanged ev)
        {
            foreach (var uplink in EntityManager.EntityQuery<UplinkComponent>())
            {
                if (uplink.UplinkAccount == ev.Account)
                {
                    UpdateUserInterface(uplink);
                }
            }    
        }

        private void OnRequestUpdateUI(EntityUid uid, UplinkComponent uplink, UplinkRequestUpdateInterfaceMessage args)
        {
            UpdateUserInterface(uplink);
        }

        private void OnBuy(EntityUid uid, UplinkComponent uplink, UplinkBuyListingMessage message)
        {
            var player = message.Session.AttachedEntity;
            if (player == null) return;
            if (uplink.UplinkAccount == null) return;

            if (!_accounts.TryPurchaseItem(uplink.UplinkAccount, message.ItemId,
                player.Transform.Coordinates, out var entity))
            {
                SoundSystem.Play(Filter.SinglePlayer(message.Session), uplink.InsufficientFundsSound.GetSound(),
                    uplink.Owner, AudioParams.Default);
                RaiseNetworkEvent(new UplinkInsufficientFundsMessage(), message.Session.ConnectedClient);
                return;
            }

            if (player.TryGetComponent(out HandsComponent? hands) &&
                entity.TryGetComponent(out ItemComponent? item))
            {
                hands.PutInHandOrDrop(item);
            }

            SoundSystem.Play(Filter.SinglePlayer(message.Session), uplink.BuySuccessSound.GetSound(),
                uplink.Owner, AudioParams.Default.WithVolume(-2f));

            RaiseNetworkEvent(new UplinkBuySuccessMessage(), message.Session.ConnectedClient);
        }

        public void ToggleUplinkUI(UplinkComponent component, IPlayerSession session)
        {
            var ui = component.Owner.GetUIOrNull(UplinkUiKey.Key);
            ui?.Toggle(session);

            UpdateUserInterface(component);
        }

        private void UpdateUserInterface(UplinkComponent component)
        {
            var ui = component.Owner.GetUIOrNull(UplinkUiKey.Key);
            if (ui == null)
                return;

            var listings = _listing.GetListings().Values.ToArray();
            var acc = component.UplinkAccount;

            UplinkAccountData accData;
            if (acc != null)
                accData = new UplinkAccountData(acc.AccountHolder, acc.Balance);
            else
                accData = new UplinkAccountData(null, 0);

            ui.SetState(new UplinkUpdateState(accData, listings));
        }

        public bool AddUplink(IEntity user, UplinkAccount account, IEntity? uplinkEntity = null)
        {
            // Try to find target item
            if (uplinkEntity == null)
            {
                uplinkEntity = FindUplinkTarget(user);
                if (uplinkEntity == null)
                    return false;
            }

            var uplink = uplinkEntity.EnsureComponent<UplinkComponent>();
            SetAccount(uplink, account);

            return true;
        }

        private IEntity? FindUplinkTarget(IEntity user)
        {
            // Try to find PDA in inventory
            if (user.TryGetComponent(out InventoryComponent? inventory))
            {
                var foundPDA = inventory.LookupItems<PDAComponent>().FirstOrDefault();
                if (foundPDA != null)
                    return foundPDA.Owner;
            }

            // Also check hands
            if (user.TryGetComponent(out IHandsComponent? hands))
            {
                var heldItems = hands.GetAllHeldItems();
                foreach (var item in heldItems)
                {
                    if (item.Owner.HasComponent<PDAComponent>())
                        return item.Owner;
                }
            }

            return null;
        }
    }
}
