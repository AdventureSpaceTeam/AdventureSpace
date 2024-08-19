namespace Content.Server.Cargo.Components;

public sealed class CargoSaleEvent
{
    public EntityUid CashStack;

    public CargoSaleEvent(EntityUid cashStack)
    {
        CashStack = cashStack;
    }
}
