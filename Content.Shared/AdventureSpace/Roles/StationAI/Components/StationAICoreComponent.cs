using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.AdventureSpace.Roles.StationAI.Components;

[NetworkedComponent]
[RegisterComponent]
public sealed partial class StationAICoreComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid GhostUid = EntityUid.Invalid;

    //Electric
    [ViewVariables(VVAccess.ReadWrite)]
    public bool Enabled = true;

    [ViewVariables(VVAccess.ReadWrite)]
    public float Wattage = 15f;
    //EndElectric

    [DataField]
    public SoundSpecifier AIHacked = new SoundPathSpecifier("/Audio/DarkStation/ii_error.ogg");

    [DataField]
    public bool Hacked;
}
