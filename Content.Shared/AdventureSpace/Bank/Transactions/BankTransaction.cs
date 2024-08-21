using Robust.Shared.Serialization;

namespace Content.Shared.AdventureSpace.Bank.Transactions;

[Serializable, NetSerializable]
public sealed class BankTransaction
{
    public string Location;
    public BankTransactionType Type;
    public BankTransactionStatus Status;
    public BankBalanceChangeType BalanceChangeType;
    public int Amount;

    public BankTransaction(string location, BankTransactionType type, BankTransactionStatus status, BankBalanceChangeType balanceChangeType, int amount)
    {
        Location = location;
        Type = type;
        Status = status;
        BalanceChangeType = balanceChangeType;
        Amount = amount;
    }
}

[Serializable, NetSerializable]
public enum BankTransactionType
{
    Salary,
    Withdraw,
    Deposit,
    Buy,
    Unknown
}

[Serializable, NetSerializable]
public enum BankTransactionStatus
{
    Success,
    Failure,
    InProgress,
    Unknown
}

[Serializable, NetSerializable]
public enum BankSalarySource
{
    CentralCommand,
    Unknown
}

[Serializable, NetSerializable]
public enum BankBalanceChangeType
{
    Income,
    Expense
}
