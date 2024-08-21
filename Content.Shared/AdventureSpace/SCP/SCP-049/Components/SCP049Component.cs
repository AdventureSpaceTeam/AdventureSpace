using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.AdventureSpace.SCP.SCP_049.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class SCP049Component : Component
{
    [DataField]
    public EntProtoId HealActionPrototype = "SCP049HealAction";

    [DataField]
    public EntityUid? HealActionUid;

    [DataField]
    public EntProtoId ThrallActionPrototype = "SCP049ThrallAction";

    [DataField]
    public EntityUid? ThrallActionUid;

    [DataField]
    public EntProtoId UnThrallActionPrototype = "SCP049UnThrallAction";

    [DataField]
    public EntityUid? UnThrallActionUid;

    [DataField]
    public EntProtoId FlashActionPrototype = "SCP049FlashAction";

    [DataField]
    public EntityUid? FlashActionUid;

    [DataField]
    public string ThrallAccentProtoId = "mute";

    /** TimeSpans **/
    [DataField]
    public TimeSpan HealDelay = TimeSpan.FromSeconds(20);

    [DataField]
    public TimeSpan MakeThrallDelay = TimeSpan.FromSeconds(10);

    [DataField]
    public TimeSpan UnThrallDelay = TimeSpan.FromSeconds(10);

    [DataField]
    public TimeSpan StunTime = TimeSpan.FromSeconds(15);
}
