using Content.Client.VendingMachines.UI;
using Content.Shared.VendingMachines;
using Robust.Client.UserInterface.Controls;
using System.Linq;
using Content.Client.UserInterface.Controls;
using Content.Shared.AdventureSpace.CCVars;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;

namespace Content.Client.VendingMachines
{
    public sealed class VendingMachineBoundUserInterface : BoundUserInterface
    {
        [Dependency] private readonly IConfigurationManager _cfg = default!;

        [ViewVariables]
        private FancyWindow? _menu;

        [ViewVariables]
        private List<VendingMachineInventoryEntry> _cachedInventory = new();

        [ViewVariables]
        private List<int> _cachedFilteredIndex = new();

        public VendingMachineBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            var vendingMachineSys = EntMan.System<VendingMachineSystem>();

            _cachedInventory = vendingMachineSys.GetAllInventory(Owner);

            if (!_cfg.GetCVar(SecretCCVars.EconomyEnabled))
            {
                _menu = this.CreateWindow<VendingMachineMenu>();
                _menu.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;

                SetupOldVendingMenu((VendingMachineMenu) _menu);
            }
            else
            {
                _menu = this.CreateWindow<EconomyVendingMachineMenu>();
                _menu.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;

                SetupNewVendingMenu((EconomyVendingMachineMenu) _menu);
            }

            _menu.OnClose += Close;
            _menu.OpenCenteredLeft();
        }

        private void SetupOldVendingMenu(VendingMachineMenu menu)
        {
            menu.OnItemSelected += OnItemSelected;
            menu.OnSearchChanged += OnSearchChanged;
            menu.Populate(_cachedInventory, out _cachedFilteredIndex);
        }

        private void SetupNewVendingMenu(EconomyVendingMachineMenu menu)
        {
            menu.OnItemSelected += OnItemSelected;
            menu.OnSearchChanged += OnSearchChanged;
            menu.OnBuyButtonPressed += OnBuyButtonPressed;
            menu.OnSelectedItemRequestUpdate += OnSelectedItemRequestUpdate;
            menu.Populate(_cachedInventory, out _cachedFilteredIndex);
        }

        private void OnSelectedItemRequestUpdate(int index)
        {
            var selected = GetSelectedItem(index);
            if (selected != null && _menu is EconomyVendingMachineMenu economyMenu)
            {
                economyMenu.SetSelectedProductState(selected, index);
            }
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (state is not VendingMachineInterfaceState newState)
                return;

            _cachedInventory = newState.Inventory;

            switch (_menu)
            {
                case EconomyVendingMachineMenu economyMenu:
                    economyMenu.Populate(_cachedInventory, out _cachedFilteredIndex, economyMenu.SearchBar.Text);
                    economyMenu.UpdateSelectedProduct();
                    break;
                case VendingMachineMenu menu:
                    menu.Populate(_cachedInventory, out _cachedFilteredIndex, menu.SearchBar.Text);
                    break;
            }
        }

        private void OnItemSelected(ItemList.ItemListSelectedEventArgs args)
        {
            var selectedItem = GetSelectedItem(args.ItemIndex);
            if (selectedItem == null)
                return;

            switch (_menu)
            {
                case EconomyVendingMachineMenu economyMenu:
                    economyMenu.SetSelectedProductState(selectedItem, args.ItemIndex);
                    break;
                case VendingMachineMenu:
                    SendMessage(new VendingMachineEjectMessage(selectedItem.Type, selectedItem.ID));
                    break;
            }
        }

        private void OnBuyButtonPressed(int index)
        {
            var selectedItem = GetSelectedItem(index);
            if (selectedItem == null)
                return;

            SendMessage(new VendingMachineEjectMessage(selectedItem.Type, selectedItem.ID));
        }

        private VendingMachineInventoryEntry? GetSelectedItem(int index)
        {
            return _cachedInventory.Count == 0
                ? null
                : _cachedInventory.ElementAtOrDefault(_cachedFilteredIndex.ElementAtOrDefault(index));
        }


        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;

            switch (_menu)
            {
                case null:
                    return;
                case EconomyVendingMachineMenu economyMenu:
                    economyMenu.OnItemSelected -= OnItemSelected;
                    break;
                case VendingMachineMenu menu:
                    menu.OnItemSelected -= OnItemSelected;
                    break;
            }

            _menu.OnClose -= Close;
            _menu.Dispose();
        }

        private void OnSearchChanged(string? filter)
        {
            switch (_menu)
            {
                case null:
                    return;
                case EconomyVendingMachineMenu economyMenu:
                    economyMenu.Populate(_cachedInventory, out _cachedFilteredIndex, filter);
                    break;
                case VendingMachineMenu menu:
                    menu.Populate(_cachedInventory, out _cachedFilteredIndex, filter);
                    break;
            }
        }
    }
}
