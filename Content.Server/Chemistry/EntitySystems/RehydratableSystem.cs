﻿using Content.Server.Chemistry.Components;
using Content.Server.Popups;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.Chemistry.EntitySystems
{
    [UsedImplicitly]
    public class RehydratableSystem : EntitySystem
    {
        [Dependency] private readonly SolutionContainerSystem _solutionsSystem = default!;
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<RehydratableComponent, SolutionChangedEvent>(OnSolutionChange);
        }

        private void OnSolutionChange(EntityUid uid, RehydratableComponent component, SolutionChangedEvent args)
        {
            if (_solutionsSystem.GetReagentQuantity(uid, component.CatalystPrototype) > FixedPoint2.Zero)
            {
                Expand(component, component.Owner);
            }
        }

        // Try not to make this public if you can help it.
        private void Expand(RehydratableComponent component, EntityUid owner)
        {
            if (component.Expanding)
            {
                return;
            }

            component.Expanding = true;
            owner.PopupMessageEveryone(Loc.GetString("rehydratable-component-expands-message", ("owner", owner)));
            if (!string.IsNullOrEmpty(component.TargetPrototype))
            {
                var ent = IoCManager.Resolve<IEntityManager>().SpawnEntity(component.TargetPrototype,
                    IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(owner).Coordinates);
                IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(ent).AttachToGridOrMap();
            }

            IoCManager.Resolve<IEntityManager>().QueueDeleteEntity((EntityUid) owner);
        }
    }
}
