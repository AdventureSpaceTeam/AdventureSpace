using Content.Shared.Eui;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.AdventureSpace.DarkForces.Vampire;

[Serializable, NetSerializable]
public sealed class VampireAbilitiesState : EuiMessageBase
{
    public NetEntity NetEntity;
    public List<string> OpenedAbilities;
    public int TotalBlood;
    public int CurrentBlood;

    public VampireAbilitiesState(NetEntity netEntity, List<string> openedAbilities, int totalBlood, int currentBlood)
    {
        NetEntity = netEntity;
        OpenedAbilities = openedAbilities;
        TotalBlood = totalBlood;
        CurrentBlood = currentBlood;
    }
}

[Serializable, NetSerializable]
public sealed class VampireAbilitySelected : EuiMessageBase
{
    public NetEntity NetEntity;
    public EntProtoId? ReplaceId;
    public string Action;
    public int BloodRequired;

    public VampireAbilitySelected(NetEntity netEntity, EntProtoId? replaceId, string action, int bloodRequired)
    {
        NetEntity = netEntity;
        ReplaceId = replaceId;
        Action = action;
        BloodRequired = bloodRequired;
    }
}
