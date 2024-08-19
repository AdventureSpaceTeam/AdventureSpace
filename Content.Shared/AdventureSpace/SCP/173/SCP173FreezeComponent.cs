using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.AdventureSpace.SCP._173;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedSCP173System))]
public partial class SCP173FreezeComponent : Component
{
    [DataField("enabled")]
    public bool Enabled = true;

    [DataField("lookedAt")]
    public bool LookedAt;
}

[Serializable, NetSerializable]
public sealed class SCP173FreezeComponentState : ComponentState
{
    public bool Enabled { get; init; }
    public bool LookedAt { get; init; }

    public SCP173FreezeComponentState(bool enabled, bool lookedAt)
    {
        Enabled = enabled;
        LookedAt = lookedAt;
    }
}
