#nullable enable
using Content.Server.Power.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.Power.EntitySystems
{
    [UsedImplicitly]
    internal sealed class PowerApcSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            UpdatesAfter.Add(typeof(PowerNetSystem));
        }

        public override void Update(float frameTime)
        {
            foreach (var apc in ComponentManager.EntityQuery<ApcComponent>())
            {
                apc.Update();
            }
        }
    }
}
