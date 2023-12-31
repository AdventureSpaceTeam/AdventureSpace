using Content.Shared.CallErt;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.CallErt;

[RegisterComponent]
public sealed partial class StationCallErtComponent : Component
{
    [ViewVariables]
    public ErtGroupsPrototype? ErtGroups;

    [ViewVariables]
    public List<CallErtGroupEnt> CalledErtGroups = new();

    [DataField("ertGroupsPrototype", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<ErtGroupsPrototype>))]
    public string ErtGroupsPrototype = default!;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("automaticApprove")]
    public bool AutomaticApprove = true;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("reviewTime")]
    public float ReviewTime = 60f;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("timeoutNewCall")]
    public float TimeoutNewCall = 60f;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("timeoutNewApprovedCall")]
    public float TimeoutNewApprovedCall = 900f;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("checkInterval")]
    public float CheckInterval = 1f;

    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextCheck = TimeSpan.FromSeconds(0);
}
