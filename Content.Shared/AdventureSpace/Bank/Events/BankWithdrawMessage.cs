using Robust.Shared.Serialization;

namespace Content.Shared.AdventureSpace.Bank.Events;

[Serializable, NetSerializable]

public sealed class BankWithdrawMessage : BoundUserInterfaceMessage
{
    //amount to withdraw. validation is happening server side but we still need client input from a text field.
    public int Amount;

    public BankWithdrawMessage(int amount)
    {
        Amount = amount;
    }
}
