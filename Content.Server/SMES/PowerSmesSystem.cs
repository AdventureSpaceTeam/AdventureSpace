#nullable enable
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.SMES
{
    [UsedImplicitly]
    internal class PowerSmesSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            foreach (var comp in ComponentManager.EntityQuery<SmesComponent>(true))
            {
                comp.OnUpdate();
            }
        }
    }
}
