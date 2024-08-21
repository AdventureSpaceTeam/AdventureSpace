namespace Content.Shared.AdventureSpace.Bank.Components;

[RegisterComponent]
public sealed partial class BankAccountComponent : Component
{
    [DataField]
    public int Balance;
}
