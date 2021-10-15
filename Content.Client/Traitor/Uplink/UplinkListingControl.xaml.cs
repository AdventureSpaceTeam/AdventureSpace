using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Maths;

namespace Content.Client.Traitor.Uplink
{
    [GenerateTypedNameReferences]
    public partial class UplinkListingControl : Control
    {
        public Button UplinkItemBuyButton => UplinkItemBuyButtonProtected;

        public UplinkListingControl(string itemName, string itemDescription, int itemPrice, bool canBuy)
        {
            RobustXamlLoader.Load(this);

            UplinkItemName.Text = itemName;
            UplinkItemDescription.SetMessage(itemDescription);

            UplinkItemBuyButtonProtected.Text = $"{itemPrice} TC";
            UplinkItemBuyButtonProtected.Disabled = !canBuy;
        }
    }
}
