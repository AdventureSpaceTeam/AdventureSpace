﻿#nullable enable
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.Mobs;
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.Components.Body.Behavior;
using Content.Shared.GameObjects.Components.Body.Part;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.ComponentDependencies;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Body.Behavior
{
    [RegisterComponent]
    public class BrainBehaviorComponent : MechanismBehaviorComponent
    {
        public override string Name => "Brain";

        protected override void OnAddedToBody()
        {
            base.OnAddedToBody();

            HandleMind(Body!.Owner, Owner);
        }

        protected override void OnAddedToPart()
        {
            base.OnAddedToPart();

            HandleMind(Part!.Owner, Owner);
        }

        protected override void OnAddedToPartInBody()
        {
            base.OnAddedToPartInBody();

            HandleMind(Body!.Owner, Owner);
        }

        protected override void OnRemovedFromBody(IBody old)
        {
            base.OnRemovedFromBody(old);

            HandleMind(Part!.Owner, old.Owner);
        }

        protected override void OnRemovedFromPart(IBodyPart old)
        {
            base.OnRemovedFromPart(old);

            HandleMind(Owner, old.Owner);
        }

        protected override void OnRemovedFromPartInBody(IBody? oldBody, IBodyPart? oldPart)
        {
            base.OnRemovedFromPartInBody(oldBody, oldPart);

            HandleMind(oldBody!.Owner, Owner);
        }

        private void HandleMind(IEntity newEntity, IEntity oldEntity)
        {
            var newMind = newEntity.EnsureComponent<MindComponent>();
            var oldMind = oldEntity.EnsureComponent<MindComponent>();

            oldMind.Mind?.TransferTo(newEntity);
        }

        public override void Update(float frameTime)
        {
        }
    }
}
