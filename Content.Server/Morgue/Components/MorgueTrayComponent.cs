﻿using Content.Shared.Interaction;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.ViewVariables;

namespace Content.Server.Morgue.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class MorgueTrayComponent : Component, IActivate
    {
        public override string Name => "MorgueTray";

        [ViewVariables]
        public EntityUid Morgue { get; set; }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            if (Morgue != null && !((!IoCManager.Resolve<IEntityManager>().EntityExists(Morgue) ? EntityLifeStage.Deleted : IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(Morgue).EntityLifeStage) >= EntityLifeStage.Deleted) && IoCManager.Resolve<IEntityManager>().TryGetComponent<MorgueEntityStorageComponent?>(Morgue, out var comp))
            {
                comp.Activate(new ActivateEventArgs(eventArgs.User, Morgue));
            }
        }
    }
}
