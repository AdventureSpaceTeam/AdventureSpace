namespace Content.Server.CallErt;

[RegisterComponent]
public sealed partial class ApproveErtConsoleComponent : Component
{
    public float UIUpdateAccumulator = 0f;

    public EntityUid? SelectedStation = null;
}
