using System;
using Content.Server.GameObjects.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Maths;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    internal sealed class StressTestMovementSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var stressTest in ComponentManager.EntityQuery<StressTestMovementComponent>(true))
            {
                var transform = stressTest.Owner.Transform;

                stressTest.Progress += frameTime;

                if (stressTest.Progress > 1)
                {
                    stressTest.Progress -= 1;
                }

                var x = MathF.Sin(stressTest.Progress * MathHelper.TwoPi);
                var y = MathF.Cos(stressTest.Progress * MathHelper.TwoPi);

                transform.WorldPosition = stressTest.Origin + (new Vector2(x, y) * 5);
            }
        }
    }
}
