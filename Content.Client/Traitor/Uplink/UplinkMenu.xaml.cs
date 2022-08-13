using Content.Client.Message;
using Content.Shared.PDA;
using Content.Shared.Traitor.Uplink;
using Robust.Client.AutoGenerated;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Client.Utility;
using Robust.Shared.Prototypes;

namespace Content.Client.Traitor.Uplink
{
    [GenerateTypedNameReferences]
    public sealed partial class UplinkMenu : DefaultWindow
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IResourceCache _resourceCache = default!;

        private UplinkWithdrawWindow? _withdrawWindow;

        public event Action<BaseButton.ButtonEventArgs, UplinkListingData>? OnListingButtonPressed;
        public event Action<BaseButton.ButtonEventArgs, UplinkCategory>? OnCategoryButtonPressed;
        public event Action<int>? OnWithdrawAttempt;

        private UplinkCategory _currentFilter;
        private UplinkAccountData? _loggedInUplinkAccount;

        public UplinkMenu()
        {
            RobustXamlLoader.Load(this);
            IoCManager.InjectDependencies(this);

            PopulateUplinkCategoryButtons();
            WithdrawButton.OnButtonDown += OnWithdrawButtonDown;
        }

        public UplinkCategory CurrentFilterCategory
        {
            get => _currentFilter;
            set
            {
                if (value.GetType() != typeof(UplinkCategory))
                {
                    return;
                }

                _currentFilter = value;
            }
        }

        public void UpdateAccount(UplinkAccountData account)
        {
            _loggedInUplinkAccount = account;

            // update balance label
            var balance = account.DataBalance;
            var weightedColor = balance switch
            {
                <= 0 => "gray",
                <= 5 => "green",
                <= 20 => "yellow",
                <= 50 => "purple",
                _ => "gray"
            };
            var balanceStr = Loc.GetString("uplink-bound-user-interface-tc-balance-popup",
                                                      ("weightedColor", weightedColor),
                                                      ("balance", balance));
            BalanceInfo.SetMarkup(balanceStr);

            // you can't withdraw if you don't have TC
            WithdrawButton.Disabled = balance <= 0;
        }

        public void UpdateListing(UplinkListingData[] listings)
        {
            // should probably chunk these out instead. to-do if this clogs the internet tubes.
            // maybe read clients prototypes instead?
            ClearListings();
            foreach (var item in listings)
            {
                AddListingGui(item);
            }
        }

        private void OnWithdrawButtonDown(BaseButton.ButtonEventArgs args)
        {
            if (_loggedInUplinkAccount == null)
                return;

            // check if window is already open
            if (_withdrawWindow != null && _withdrawWindow.IsOpen)
            {
                _withdrawWindow.MoveToFront();
                return;
            }

            // open a new one
            _withdrawWindow = new UplinkWithdrawWindow(_loggedInUplinkAccount.DataBalance);
            _withdrawWindow.OpenCentered();

            _withdrawWindow.OnWithdrawAttempt += OnWithdrawAttempt;
        }

        private void AddListingGui(UplinkListingData listing)
        {
            if (!_prototypeManager.TryIndex(listing.ItemId, out EntityPrototype? prototype) || listing.Category != CurrentFilterCategory)
            {
                return;
            }

            var listingName = listing.ListingName == string.Empty ? prototype.Name : listing.ListingName;
            var listingDesc = listing.Description == string.Empty ? prototype.Description : listing.Description;
            var listingPrice = listing.Price;
            var canBuy = _loggedInUplinkAccount?.DataBalance >= listing.Price;

            var texture = listing.Icon?.Frame0();
            if (texture == null)
                texture = SpriteComponent.GetPrototypeIcon(prototype, _resourceCache).Default;

            var newListing = new UplinkListingControl(listingName, listingDesc, listingPrice, canBuy, texture);
            newListing.UplinkItemBuyButton.OnButtonDown += args
                => OnListingButtonPressed?.Invoke(args, listing);

            UplinkListingsContainer.AddChild(newListing);
        }

        private void ClearListings()
        {
            UplinkListingsContainer.Children.Clear();
        }

        private void PopulateUplinkCategoryButtons()
        {
            foreach (UplinkCategory cat in Enum.GetValues(typeof(UplinkCategory)))
            {
                var catButton = new PDAUplinkCategoryButton
                {
                    Text = Loc.GetString(cat.ToString()),
                    ButtonCategory = cat
                };
                //It'd be neat if it could play a cool tech ping sound when you switch categories,
                //but right now there doesn't seem to be an easy way to do client-side audio without still having to round trip to the server and
                //send to a specific client INetChannel.
                catButton.OnPressed += args => OnCategoryButtonPressed?.Invoke(args, catButton.ButtonCategory);

                CategoryListContainer.AddChild(catButton);
            }
        }

        public override void Close()
        {
            base.Close();
            _withdrawWindow?.Close();
        }

        private sealed class PDAUplinkCategoryButton : Button
        {
            public UplinkCategory ButtonCategory;
        }
    }
}
