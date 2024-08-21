namespace Content.Shared.VendingMachines;

[RegisterComponent]
public sealed partial class VendingPriceComponent : Component
{
    [DataField(required: true)]
    public int Price;
}
