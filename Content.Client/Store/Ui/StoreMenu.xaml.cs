using System.Linq;
using Content.Client.Actions;
using Content.Client.GameTicking.Managers;
using Content.Client.Message;
using Content.Shared.FixedPoint;
using Content.Shared.Store;
using Robust.Client.AutoGenerated;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.Store.Ui;

[GenerateTypedNameReferences]
public sealed partial class StoreMenu : DefaultWindow
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IEntitySystemManager _entitySystem = default!;
    private readonly ClientGameTicker _gameTicker;

    private StoreWithdrawWindow? _withdrawWindow;

    public event EventHandler<string>? SearchTextUpdated;
    public event Action<BaseButton.ButtonEventArgs, ListingData>? OnListingButtonPressed;
    public event Action<BaseButton.ButtonEventArgs, string>? OnCategoryButtonPressed;
    public event Action<BaseButton.ButtonEventArgs, string, int>? OnWithdrawAttempt;
    public event Action<BaseButton.ButtonEventArgs>? OnRefreshButtonPressed;
    public event Action<BaseButton.ButtonEventArgs>? OnRefundAttempt;

    public Dictionary<string, FixedPoint2> Balance = new();
    public string CurrentCategory = string.Empty;

    public StoreMenu(string name)
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);

        _gameTicker = _entitySystem.GetEntitySystem<ClientGameTicker>();

        WithdrawButton.OnButtonDown += OnWithdrawButtonDown;
        RefreshButton.OnButtonDown += OnRefreshButtonDown;
        RefundButton.OnButtonDown += OnRefundButtonDown;
        SearchBar.OnTextChanged += _ => SearchTextUpdated?.Invoke(this, SearchBar.Text);

        if (Window != null)
            Window.Title = name;
    }

    public void UpdateBalance(Dictionary<string, FixedPoint2> balance)
    {
        Balance = balance;

        var currency = balance.ToDictionary(type =>
            (type.Key, type.Value), type => _prototypeManager.Index<CurrencyPrototype>(type.Key));

        var balanceStr = string.Empty;
        foreach (var ((_, amount), proto) in currency)
        {
            balanceStr += Loc.GetString("store-ui-balance-display", ("amount", amount),
                ("currency", Loc.GetString(proto.DisplayName, ("amount", 1))));
        }

        BalanceInfo.SetMarkup(balanceStr.TrimEnd());

        var disabled = true;
        foreach (var type in currency)
        {
            if (type.Value.CanWithdraw && type.Value.Cash != null && type.Key.Item2 > 0)
                disabled = false;
        }

        WithdrawButton.Disabled = disabled;
    }

    public void UpdateListing(List<ListingData> listings)
    {
        var sorted = listings.OrderBy(l => l.Priority).ThenBy(l => l.Cost.Values.Sum());

        // should probably chunk these out instead. to-do if this clogs the internet tubes.
        // maybe read clients prototypes instead?
        ClearListings();
        foreach (var item in sorted)
        {
            AddListingGui(item);
        }
    }

    public void SetFooterVisibility(bool visible)
    {
        TraitorFooter.Visible = visible;
    }


    private void OnRefreshButtonDown(BaseButton.ButtonEventArgs args)
    {
        OnRefreshButtonPressed?.Invoke(args);
    }

    private void OnWithdrawButtonDown(BaseButton.ButtonEventArgs args)
    {
        // check if window is already open
        if (_withdrawWindow != null && _withdrawWindow.IsOpen)
        {
            _withdrawWindow.MoveToFront();
            return;
        }

        // open a new one
        _withdrawWindow = new StoreWithdrawWindow();
        _withdrawWindow.OpenCentered();

        _withdrawWindow.CreateCurrencyButtons(Balance);
        _withdrawWindow.OnWithdrawAttempt += OnWithdrawAttempt;
    }

    private void OnRefundButtonDown(BaseButton.ButtonEventArgs args)
    {
        OnRefundAttempt?.Invoke(args);
    }

    private void AddListingGui(ListingData listing)
    {
        if (!listing.Categories.Contains(CurrentCategory))
            return;

        var listingName = ListingLocalisationHelpers.GetLocalisedNameOrEntityName(listing, _prototypeManager);
        var listingDesc = ListingLocalisationHelpers.GetLocalisedDescriptionOrEntityDescription(listing, _prototypeManager);
        var listingPrice = listing.Cost;
        var canBuy = CanBuyListing(Balance, listingPrice);

        var spriteSys = _entityManager.EntitySysManager.GetEntitySystem<SpriteSystem>();

        Texture? texture = null;
        if (listing.Icon != null)
            texture = spriteSys.Frame0(listing.Icon);

        if (listing.ProductEntity != null)
        {
            if (texture == null)
                texture = spriteSys.GetPrototypeIcon(listing.ProductEntity).Default;
        }
        else if (listing.ProductAction != null)
        {
            var actionId = _entityManager.Spawn(listing.ProductAction);
            if (_entityManager.System<ActionsSystem>().TryGetActionData(actionId, out var action) &&
                action.Icon != null)
            {
                texture = spriteSys.Frame0(action.Icon);
            }
        }
        var listingInStock = ListingInStock(listing);
        if (listingInStock != GetListingPriceString(listing))
        {
            listingName += " (Out of stock)";
            canBuy = false;
        }

        var newListing = new StoreListingControl(listingName, listingDesc, listingInStock, canBuy, texture);
        newListing.StoreItemBuyButton.OnButtonDown += args
            => OnListingButtonPressed?.Invoke(args, listing);

        StoreListingsContainer.AddChild(newListing);
    }

    /// <summary>
    /// Return time until available or the cost.
    /// </summary>
    /// <param name="listing"></param>
    /// <returns></returns>
    public string ListingInStock(ListingData listing)
    {
        var stationTime = _gameTiming.CurTime.Subtract(_gameTicker.RoundStartTimeSpan);

        TimeSpan restockTimeSpan = TimeSpan.FromMinutes(listing.RestockTime);
        if (restockTimeSpan > stationTime)
        {
            var timeLeftToBuy = stationTime - restockTimeSpan;
            return timeLeftToBuy.Duration().ToString(@"mm\:ss");
        }

        return GetListingPriceString(listing);
    }
    public bool CanBuyListing(Dictionary<string, FixedPoint2> currency, Dictionary<string, FixedPoint2> price)
    {
        foreach (var type in price)
        {
            if (!currency.ContainsKey(type.Key))
                return false;

            if (currency[type.Key] < type.Value)
                return false;
        }
        return true;
    }

    public string GetListingPriceString(ListingData listing)
    {
        var text = string.Empty;
        if (listing.Cost.Count < 1)
            text = Loc.GetString("store-currency-free");
        else
        {
            foreach (var (type, amount) in listing.Cost)
            {
                var currency = _prototypeManager.Index<CurrencyPrototype>(type);
                text += Loc.GetString("store-ui-price-display", ("amount", amount),
                    ("currency", Loc.GetString(currency.DisplayName, ("amount", amount))));
            }
        }

        return text.TrimEnd();
    }

    private void ClearListings()
    {
        StoreListingsContainer.Children.Clear();
    }

    public void PopulateStoreCategoryButtons(HashSet<ListingData> listings)
    {
        var allCategories = new List<StoreCategoryPrototype>();
        foreach (var listing in listings)
        {
            foreach (var cat in listing.Categories)
            {
                var proto = _prototypeManager.Index<StoreCategoryPrototype>(cat);
                if (!allCategories.Contains(proto))
                    allCategories.Add(proto);
            }
        }

        allCategories = allCategories.OrderBy(c => c.Priority).ToList();

        // This will reset the Current Category selection if nothing matches the search.
        if (allCategories.All(category => category.ID != CurrentCategory))
            CurrentCategory = string.Empty;

        if (CurrentCategory == string.Empty && allCategories.Count > 0)
            CurrentCategory = allCategories.First().ID;

        CategoryListContainer.Children.Clear();
        if (allCategories.Count < 1)
            return;

        foreach (var proto in allCategories)
        {
            var catButton = new StoreCategoryButton
            {
                Text = Loc.GetString(proto.Name),
                Id = proto.ID
            };

            catButton.OnPressed += args => OnCategoryButtonPressed?.Invoke(args, catButton.Id);
            CategoryListContainer.AddChild(catButton);
        }
    }

    public override void Close()
    {
        base.Close();
        _withdrawWindow?.Close();
    }

    public void UpdateRefund(bool allowRefund)
    {
        RefundButton.Disabled = !allowRefund;
    }

    private sealed class StoreCategoryButton : Button
    {
        public string? Id;
    }
}
