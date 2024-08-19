using Robust.Shared.GameStates;

namespace Content.Shared.AdventureSpace.Patron;

[RegisterComponent]
[NetworkedComponent]
[AutoGenerateComponentState(true)]
public sealed partial class PatronEarsVisualizerComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    public string RsiPath = default!;
}
