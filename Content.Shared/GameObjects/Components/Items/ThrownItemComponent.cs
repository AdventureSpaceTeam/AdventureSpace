#nullable enable
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Physics;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision;
using Robust.Shared.Physics.Dynamics;

namespace Content.Shared.GameObjects.Components.Items
{
    [RegisterComponent]
    public class ThrownItemComponent : Component, IStartCollide, ICollideSpecial, IThrown, ILand
    {
        public override string Name => "ThrownItem";

        public IEntity? Thrower { get; set; }

        private Fixture? _fixture;

        void IStartCollide.CollideWith(IPhysBody ourBody, IPhysBody otherBody, in Manifold manifold)
        {
            if (otherBody.Entity == Thrower) return;
            EntitySystem.Get<ThrownItemSystem>().ThrowCollideInteraction(Thrower, ourBody, otherBody);
        }

        bool ICollideSpecial.PreventCollide(IPhysBody collidedwith)
        {
            return collidedwith.Entity == Thrower;
        }

        void IThrown.Thrown(ThrownEventArgs eventArgs)
        {
            if (!Owner.TryGetComponent(out PhysicsComponent? physicsComponent) ||
                physicsComponent.Fixtures.Count != 1) return;

            var shape = physicsComponent.Fixtures[0].Shape;
            _fixture = new Fixture(physicsComponent, shape) {CollisionLayer = (int) CollisionGroup.ThrownItem, Hard = false};
            physicsComponent.AddFixture(_fixture);
        }

        void ILand.Land(LandEventArgs eventArgs)
        {
            if (!Owner.TryGetComponent(out PhysicsComponent? physicsComponent) || _fixture == null) return;

            physicsComponent.RemoveFixture(_fixture);
        }
    }
}
