using Robust.Shared.GameStates;

namespace Content.Shared.AdventureSpace.DarkForces.Narsi.Roles;

[RegisterComponent, NetworkedComponent]
public sealed partial class NarsiCultistComponent : Component
{
    [DataField]
    public Dictionary<string, EntityUid?> Abilities = new();
}
