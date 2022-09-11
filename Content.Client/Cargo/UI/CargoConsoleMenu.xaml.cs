using System.Linq;
using Content.Client.UserInterface;
using Content.Shared.Cargo;
using Content.Shared.Cargo.Prototypes;
using Robust.Client.AutoGenerated;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Prototypes;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client.Cargo.UI
{
    [GenerateTypedNameReferences]
    public sealed partial class CargoConsoleMenu : FancyWindow
    {
        private IPrototypeManager _protoManager;
        private SpriteSystem _spriteSystem;

        public event Action<ButtonEventArgs>? OnItemSelected;
        public event Action<ButtonEventArgs>? OnOrderApproved;
        public event Action<ButtonEventArgs>? OnOrderCanceled;

        private readonly List<string> _categoryStrings = new();
        private string? _category;

        public CargoConsoleMenu(IPrototypeManager protoManager, SpriteSystem spriteSystem)
        {
            RobustXamlLoader.Load(this);
            _protoManager = protoManager;
            _spriteSystem = spriteSystem;

            Title = Loc.GetString("cargo-console-menu-title");

            SearchBar.OnTextChanged += OnSearchBarTextChanged;
            Categories.OnItemSelected += OnCategoryItemSelected;
        }

        private void OnCategoryItemSelected(OptionButton.ItemSelectedEventArgs args)
        {
            SetCategoryText(args.Id);
            PopulateProducts();
        }

        private void OnSearchBarTextChanged(LineEdit.LineEditEventArgs args)
        {
            PopulateProducts();
        }

        private void SetCategoryText(int id)
        {
            _category = id == 0 ? null : _categoryStrings[id];
            Categories.SelectId(id);
        }

        public IEnumerable<CargoProductPrototype> ProductPrototypes => _protoManager.EnumeratePrototypes<CargoProductPrototype>();

        /// <summary>
        ///     Populates the list of products that will actually be shown, using the current filters.
        /// </summary>
        public void PopulateProducts()
        {
            Products.RemoveAllChildren();
            var products = ProductPrototypes.ToList();
            products.Sort((x, y) =>
                string.Compare(x.Name, y.Name, StringComparison.Ordinal));

            var search = SearchBar.Text.Trim().ToLowerInvariant();
            foreach (var prototype in products)
            {
                // if no search or category
                // else if search
                // else if category and not search
                if (search.Length == 0 && _category == null ||
                    search.Length != 0 && prototype.Name.ToLowerInvariant().Contains(search) ||
                    search.Length == 0 && _category != null && prototype.Category.Equals(_category))
                {
                    var button = new CargoProductRow
                    {
                        Product = prototype,
                        ProductName = { Text = prototype.Name },
                        PointCost = { Text = Loc.GetString("cargo-console-menu-points-amount", ("amount", prototype.PointCost.ToString())) },
                        Icon = { Texture = _spriteSystem.Frame0(prototype.Icon) },
                    };
                    button.MainButton.OnPressed += args =>
                    {
                        OnItemSelected?.Invoke(args);
                    };
                    Products.AddChild(button);
                }
            }
        }

        /// <summary>
        ///     Populates the list of products that will actually be shown, using the current filters.
        /// </summary>
        public void PopulateCategories()
        {
            _categoryStrings.Clear();
            Categories.Clear();

            foreach (var prototype in ProductPrototypes)
            {
                if (!_categoryStrings.Contains(prototype.Category))
                {
                    _categoryStrings.Add(Loc.GetString(prototype.Category));
                }
            }

            _categoryStrings.Sort();

            // Add "All" category at the top of the list
            _categoryStrings.Insert(0, Loc.GetString("cargo-console-menu-populate-categories-all-text"));

            foreach (var str in _categoryStrings)
            {
                Categories.AddItem(str);
            }
        }

        /// <summary>
        ///     Populates the list of orders and requests.
        /// </summary>
        public void PopulateOrders(IEnumerable<CargoOrderData> orders)
        {
            Orders.DisposeAllChildren();
            Requests.DisposeAllChildren();

            foreach (var order in orders)
            {
                var product = _protoManager.Index<CargoProductPrototype>(order.ProductId);
                var productName = product.Name;

                var row = new CargoOrderRow
                {
                    Order = order,
                    Icon = { Texture = _spriteSystem.Frame0(product.Icon) },
                    ProductName =
                    {
                        Text = Loc.GetString(
                            "cargo-console-menu-populate-orders-cargo-order-row-product-name-text",
                            ("productName", productName),
                            ("orderAmount", order.Amount),
                            ("orderRequester", order.Requester))
                    },
                    Description = {Text = Loc.GetString("cargo-console-menu-order-reason-description",
                                                        ("reason", order.Reason))}
                };
                row.Cancel.OnPressed += (args) => { OnOrderCanceled?.Invoke(args); };
                if (order.Approved)
                {
                    row.Approve.Visible = false;
                    row.Cancel.Visible = false;
                    Orders.AddChild(row);
                }
                else
                {
                    // TODO: Disable based on access.
                    row.Approve.OnPressed += (args) => { OnOrderApproved?.Invoke(args); };
                    Requests.AddChild(row);
                }
            }
        }

        public void UpdateCargoCapacity(int count, int capacity)
        {
            // TODO: Rename + Loc.
            ShuttleCapacityLabel.Text = $"{count}/{capacity}";
        }

        public void UpdateBankData(string name, int points)
        {
            AccountNameLabel.Text = name;
            PointsLabel.Text = Loc.GetString("cargo-console-menu-points-amount", ("amount", points.ToString()));
        }
    }
}
