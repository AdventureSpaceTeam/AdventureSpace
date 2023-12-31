namespace Content.Server.CallErt;

[RegisterComponent]
public sealed partial class CallErtConsoleComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public float CallErtCooldownRemaining;

    public float UIUpdateAccumulator = 0f;

    /// <summary>
    /// Time in seconds between announcement delays on a per-console basis
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("delayCallErt")]
    public int DelayBetweenCallErt = 30;
}
