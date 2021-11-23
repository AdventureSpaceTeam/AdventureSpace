using System.Collections.Generic;
using Content.Server.Power.Components;
using Content.Server.Recycling.Components;
using Content.Shared.Body.Components;
using Content.Shared.Recycling;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Utility;

namespace Content.Server.Recycling
{
    internal sealed class RecyclerSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<RecyclerComponent, StartCollideEvent>(HandleCollide);
        }

        private void HandleCollide(EntityUid uid, RecyclerComponent component, StartCollideEvent args)
        {
            Recycle(component, args.OtherFixture.Body.Owner);
        }

        private void Recycle(RecyclerComponent component, IEntity entity)
        {
            // TODO: Prevent collision with recycled items

            // Can only recycle things that are recyclable... And also check the safety of the thing to recycle.
            if (!entity.TryGetComponent(out RecyclableComponent? recyclable) || !recyclable.Safe && component.Safe) return;

            // Mobs are a special case!
            if (CanGib(component, entity))
            {
                entity.GetComponent<SharedBodyComponent>().Gib(true);
                Bloodstain(component);
                return;
            }

            recyclable.Recycle(component.Efficiency);
        }

        private bool CanGib(RecyclerComponent component, IEntity entity)
        {
            // We suppose this entity has a Recyclable component.
            return entity.HasComponent<SharedBodyComponent>() && !component.Safe &&
                   component.Owner.TryGetComponent(out ApcPowerReceiverComponent? receiver) && receiver.Powered;
        }

        public void Bloodstain(RecyclerComponent component)
        {
            if (component.Owner.TryGetComponent(out AppearanceComponent? appearance))
            {
                appearance.SetData(RecyclerVisuals.Bloody, true);
            }
        }
    }
}
