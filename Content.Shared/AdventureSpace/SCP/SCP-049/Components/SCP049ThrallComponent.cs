using Robust.Shared.GameStates;

namespace Content.Shared.AdventureSpace.SCP.SCP_049.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class SCP049ThrallComponent : Component
{
    [DataField]
    public string? OldAccent;
}
