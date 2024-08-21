using Robust.Shared.Serialization;

namespace Content.Shared.AdventureSpace.Roles.Salary;

[ImplicitDataDefinitionForInheritors]
[Serializable]
[NetSerializable]
public sealed partial class CrewSalaryEntry
{
    [DataField]
    public int Salary;

    [DataField]
    public int PrePaid;

    [DataField]
    public int PrePaidCount;

    [DataField]
    public List<CrewSalaryPenalty> SalaryPenalties = new();

    [DataField]
    public List<CrewSalaryBonus> SalaryBonuses = new();

    public CrewSalaryEntry(int salary)
    {
        Salary = salary;
    }
}

[ImplicitDataDefinitionForInheritors]
[Serializable]
[NetSerializable]
public sealed partial class CrewSalaryPenalty
{
    public int Penalty;
    public string Reason;

    public CrewSalaryPenalty(int penalty, string reason)
    {
        Penalty = penalty;
        Reason = reason;
    }
}

[ImplicitDataDefinitionForInheritors]
[Serializable]
[NetSerializable]
public sealed partial class CrewSalaryBonus
{
    public int Bonus;
    public string Reason;

    public CrewSalaryBonus(int bonus, string reason)
    {
        Bonus = bonus;
        Reason = reason;
    }
}
