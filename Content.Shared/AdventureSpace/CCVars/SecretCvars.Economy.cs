using Robust.Shared;
using Robust.Shared.Configuration;

namespace Content.Shared.AdventureSpace.CCVars;

[CVarDefs]
public sealed partial class SecretCCVars : CVars
{
    public static readonly CVarDef<bool> EconomyEnabled =
        CVarDef.Create("economy.enabled", false, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> EconomyAntagBaseSalary =
        CVarDef.Create("economy.antag_base_salary", 300, CVar.SERVER);

    public static readonly CVarDef<int> EconomyAntagMaxSalary =
        CVarDef.Create("economy.antag_max_salary", 2500, CVar.SERVER);
}
