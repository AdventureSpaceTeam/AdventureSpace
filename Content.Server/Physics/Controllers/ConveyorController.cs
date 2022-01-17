using System;
using System.Collections.Generic;
using Content.Server.Conveyor;
using Content.Server.Recycling.Components;
using Content.Shared.Movement.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Controllers;

namespace Content.Server.Physics.Controllers
{
    internal sealed class ConveyorController : VirtualController
    {
        [Dependency] private readonly ConveyorSystem _conveyor = default!;

        public override void Initialize()
        {
            UpdatesAfter.Add(typeof(MoverController));

            base.Initialize();
        }

        public override void UpdateBeforeSolve(bool prediction, float frameTime)
        {
            base.UpdateBeforeSolve(prediction, frameTime);
            foreach (var comp in EntityManager.EntityQuery<ConveyorComponent>())
            {
                Convey(_conveyor, comp, frameTime);
            }
        }

        private void Convey(ConveyorSystem system, ConveyorComponent comp, float frameTime)
        {
            // Use an event for conveyors to know what needs to run
            if (!system.CanRun(comp))
            {
                return;
            }

            var direction = system.GetAngle(comp).ToVec();
            var entMan = IoCManager.Resolve<IEntityManager>();
                         var ownerPos = entMan.GetComponent<TransformComponent>(comp.Owner).WorldPosition;

            foreach (var (entity, physics) in EntitySystem.Get<ConveyorSystem>().GetEntitiesToMove(comp))
            {
                var itemRelativeToConveyor = entMan.GetComponent<TransformComponent>(entity).WorldPosition - ownerPos;
                physics.LinearVelocity += Convey(direction, comp.Speed, frameTime, itemRelativeToConveyor);
            }
        }

        private Vector2 Convey(Vector2 direction, float speed, float frameTime, Vector2 itemRelativeToConveyor)
        {
            if(speed == 0 || direction.Length == 0) return Vector2.Zero;
            direction = direction.Normalized;

            var dirNormal = new Vector2(direction.Y, direction.X);
            var dot = Vector2.Dot(itemRelativeToConveyor, dirNormal);

            var velocity = direction * speed * 5;
            velocity += dirNormal * speed * -dot;

            return velocity * frameTime;
        }
    }
}
