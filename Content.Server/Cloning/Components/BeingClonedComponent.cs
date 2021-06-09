using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.Cloning.Components
{
    [RegisterComponent]
    public class BeingClonedComponent : Component
    {
        public override string Name => "BeingCloned";

        [ViewVariables]
        public Mind.Mind? Mind = default;

        [ViewVariables]
        public EntityUid Parent;
    }
}
