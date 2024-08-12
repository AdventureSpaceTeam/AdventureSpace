using Content.Shared.CallErt;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.CallErt;

[Prototype("ertGroups")]
public sealed class ErtGroupsPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = default!;

    [DataField("groups")] public Dictionary<string, ErtGroupDetail> ErtGroupList = new();
}

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

[RegisterComponent]
public sealed partial class CallErtConsoleComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public float CallErtCooldownRemaining;

    public float UIUpdateAccumulator = 0f;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("delayCallErt")]
    public int DelayBetweenCallErt = 30;

    [ViewVariables]
    public string? SelectedErtGroup = null;
}

[RegisterComponent]
public sealed partial class ApproveErtConsoleComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public float SendErtCooldownRemaining;

    public float UIUpdateAccumulator = 0f;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("delaySendErt")]
    public int DelayBetweenSendErt = 30;

    [ViewVariables]
    public EntityUid? SelectedStation = null;

    [ViewVariables]
    public string? SelectedErtGroup = null;
}
