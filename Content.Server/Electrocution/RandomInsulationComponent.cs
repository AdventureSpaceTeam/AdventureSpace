using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Electrocution
{
    [RegisterComponent]
    public class RandomInsulationComponent : Component
    {
        [DataField("list")]
        public readonly float[] List = { 0f };
    }
}
