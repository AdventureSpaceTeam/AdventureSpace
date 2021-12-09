using System;
using Content.Server.Mind.Commands;
using Content.Server.Mind.Components;
using Content.Server.Players;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Utility;

namespace Content.Server.Ghost.Roles.Components
{
    /// <summary>
    ///     Allows a ghost to take over the Owner entity.
    /// </summary>
    [RegisterComponent, ComponentReference(typeof(GhostRoleComponent))]
    public class GhostTakeoverAvailableComponent : GhostRoleComponent
    {
        public override string Name => "GhostTakeoverAvailable";

        public override bool Take(IPlayerSession session)
        {
            if (Taken)
                return false;

            Taken = true;

            var mind = Owner.EnsureComponent<MindComponent>();

            if (mind.HasMind)
                return false;

            if (MakeSentient)
                MakeSentientCommand.MakeSentient(Owner, IoCManager.Resolve<IEntityManager>());

            var ghostRoleSystem = EntitySystem.Get<GhostRoleSystem>();
            ghostRoleSystem.GhostRoleInternalCreateMindAndTransfer(session, Owner, Owner, this);

            ghostRoleSystem.UnregisterGhostRole(this);

            return true;
        }
    }
}
