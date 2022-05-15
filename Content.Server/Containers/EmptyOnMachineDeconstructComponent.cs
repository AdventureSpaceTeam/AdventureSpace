namespace Content.Server.Containers
{
    /// <summary>
    /// Empties a list of containers when the machine is deconstructed via MachineDeconstructedEvent.
    /// </summary>
    [RegisterComponent]
    public sealed class EmptyOnMachineDeconstructComponent : Component
    {
        [ViewVariables]
        [DataField("containers")]
        public HashSet<string> Containers { get; set; } = new();
    }
}
