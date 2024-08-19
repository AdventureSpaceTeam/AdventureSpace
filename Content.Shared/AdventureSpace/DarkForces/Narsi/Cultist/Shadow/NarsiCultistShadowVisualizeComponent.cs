using Robust.Shared.GameStates;

namespace Content.Shared.AdventureSpace.DarkForces.Narsi.Cultist.Shadow;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NarsiCultistShadowVisualizeComponent : Component
{

    [DataField("entityToCopy")]
    [AutoNetworkedField]
    public NetEntity EntityToCopy;
}
