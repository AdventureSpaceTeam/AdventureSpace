namespace Content.Server.CallErt;

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
