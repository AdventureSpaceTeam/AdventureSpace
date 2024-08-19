using Content.Shared.Actions;

namespace Content.Shared.AdventureSpace.Patron.Pets;

public sealed partial class PetOrderActionEvent : InstantActionEvent
{
    [DataField("type")]
    public PetOrderType Type;
}

public sealed partial class PetMakeGhostRoleEvent : InstantActionEvent
{
}

public sealed partial class PetRemoveGhostRoleEvent : InstantActionEvent
{
}
