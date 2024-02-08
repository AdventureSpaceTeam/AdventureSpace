using Robust.Shared.Serialization;
using Robust.Shared.GameStates;
namespace Content.Shared.Cards;

[RegisterComponent, NetworkedComponent]
public sealed partial class FlipCardComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("flipped")]
    public bool Flipped = false;
}

[Serializable, NetSerializable]
public sealed class FlipCardComponentState : ComponentState
{
    public bool Flipped { get; init; }
}

[Serializable, NetSerializable]
public enum CardsVisual
{
    Visual,
    Normal,
    Flipped
}
