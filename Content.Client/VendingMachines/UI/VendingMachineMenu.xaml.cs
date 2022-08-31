using Content.Shared.VendingMachines;
using Robust.Client.AutoGenerated;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Prototypes;

namespace Content.Client.VendingMachines.UI
{
    [GenerateTypedNameReferences]
    public sealed partial class VendingMachineMenu : DefaultWindow
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public event Action<ItemList.ItemListSelectedEventArgs>? OnItemSelected;

        public VendingMachineMenu()
        {
            MinSize = SetSize = (250, 150);
            RobustXamlLoader.Load(this);
            IoCManager.InjectDependencies(this);

            VendingContents.OnItemSelected += args =>
            {
                OnItemSelected?.Invoke(args);
            };
        }

        /// <summary>
        /// Populates the list of available items on the vending machine interface
        /// and sets icons based on their prototypes
        /// </summary>
        public void Populate(List<VendingMachineInventoryEntry> inventory)
        {
            if (inventory.Count == 0)
            {
                VendingContents.Clear();
                var outOfStockText = Loc.GetString("vending-machine-component-try-eject-out-of-stock");
                VendingContents.AddItem(outOfStockText);
                SetSizeAfterUpdate(outOfStockText.Length);
                return;
            }

            while (inventory.Count != VendingContents.Count)
            {
                if (inventory.Count > VendingContents.Count)
                    VendingContents.AddItem(string.Empty);
                else
                    VendingContents.RemoveAt(VendingContents.Count - 1);
            }

            var longestEntry = string.Empty;
            var spriteSystem = EntitySystem.Get<SpriteSystem>();
            for (var i = 0; i < inventory.Count; i++)
            {
                var entry = inventory[i];
                var vendingItem = VendingContents[i];
                vendingItem.Text = string.Empty;
                vendingItem.Icon = null;

                var itemName = entry.ID;
                Texture? icon = null;
                if (_prototypeManager.TryIndex<EntityPrototype>(entry.ID, out var prototype))
                {
                    itemName = prototype.Name;
                    icon = spriteSystem.GetPrototypeIcon(prototype).Default;
                }

                if (itemName.Length > longestEntry.Length)
                    longestEntry = itemName;

                vendingItem.Text = $"{itemName} [{entry.Amount}]";
                vendingItem.Icon = icon;
            }

            SetSizeAfterUpdate(longestEntry.Length);
        }

        private void SetSizeAfterUpdate(int longestEntryLength)
        {
            SetSize = (Math.Clamp((longestEntryLength + 2) * 12, 250, 300),
                Math.Clamp(VendingContents.Count * 50, 150, 350));
        }
    }
}
