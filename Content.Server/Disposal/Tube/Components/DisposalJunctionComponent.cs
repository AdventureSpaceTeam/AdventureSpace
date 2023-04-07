namespace Content.Server.Disposal.Tube.Components;

[RegisterComponent]
[Access(typeof(DisposalTubeSystem))]
[Virtual]
public class DisposalJunctionComponent : Component
{
    /// <summary>
    ///     The angles to connect to.
    /// </summary>
    [DataField("degrees")] public List<Angle> Degrees = new();
}
