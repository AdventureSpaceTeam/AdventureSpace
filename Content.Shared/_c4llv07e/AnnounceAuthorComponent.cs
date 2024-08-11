using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Shared._c4llv07e.AnnounceAuthor;

[RegisterComponent, NetworkedComponent]
public sealed partial class AnnounceAuthorComponent : Component
{
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public string Name = "Неизвестный";
}
