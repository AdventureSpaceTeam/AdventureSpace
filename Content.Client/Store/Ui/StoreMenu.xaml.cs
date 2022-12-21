using Content.Client.Message;
using Content.Shared.Store;
using Robust.Client.AutoGenerated;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Prototypes;
using Robust.Client.Graphics;
using Content.Shared.Actions.ActionTypes;
using System.Linq;
using Content.Shared.FixedPoint;

namespace Content.Client.Store.Ui;

[GenerateTypedNameReferences]
public sealed partial class StoreMenu : DefaultWindow
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private StoreWithdrawWindow? _withdrawWindow;

    public event Action<BaseButton.ButtonEventArgs, ListingData>? OnListingButtonPressed;
    public event Action<BaseButton.ButtonEventArgs, string>? OnCategoryButtonPressed;
    public event Action<BaseButton.ButtonEventArgs, string, int>? OnWithdrawAttempt;

    public Dictionary<string, FixedPoint2> Balance = new();
    public string CurrentCategory = string.Empty;

    public StoreMenu(string name)
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);

        WithdrawButton.OnButtonDown += OnWithdrawButtonDown;
        if (Window != null)
            Window.Title = name;
    }

    public void UpdateBalance(Dictionary<string, FixedPoint2> balance)
    {
        Balance = balance;

        var currency = balance.ToDictionary(type =>
            (type.Key, type.Value), type => _prototypeManager.Index<CurrencyPrototype>(type.Key));

        var balanceStr = string.Empty;
        foreach (var ((type, amount),proto) in currency)
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

    private void AddListingGui(ListingData listing)
    {
        if (!listing.Categories.Contains(CurrentCategory))
            return;

        var listingName = Loc.GetString(listing.Name);
        var listingDesc = Loc.GetString(listing.Description);
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

            var proto = _prototypeManager.Index<EntityPrototype>(listing.ProductEntity);
            if (listingName == string.Empty)
                listingName = proto.Name;
            if (listingDesc == string.Empty)
                listingDesc = proto.Description;
        }
        else if (listing.ProductAction != null)
        {
            var action = _prototypeManager.Index<InstantActionPrototype>(listing.ProductAction);
            if (action.Icon != null)
                texture = spriteSys.Frame0(action.Icon);
        }

        var newListing = new StoreListingControl(listingName, listingDesc, GetListingPriceString(listing), canBuy, texture);
        newListing.StoreItemBuyButton.OnButtonDown += args
            => OnListingButtonPressed?.Invoke(args, listing);

        StoreListingsContainer.AddChild(newListing);
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

        if (CurrentCategory == string.Empty && allCategories.Count > 0)
            CurrentCategory = allCategories.First().ID;

        if (allCategories.Count <= 1)
            return;

        CategoryListContainer.Children.Clear();

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

    private sealed class StoreCategoryButton : Button
    {
        public string? Id;
    }
}
