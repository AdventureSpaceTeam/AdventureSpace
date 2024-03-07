namespace Content.Server.CallErt;

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
