﻿using System;
using Content.Server.Mind.Commands;
using Content.Server.Mind.Components;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Ghost.Roles.Components
{
    /// <summary>
    ///     Allows a ghost to take this role, spawning a new entity.
    /// </summary>
    [RegisterComponent, ComponentReference(typeof(GhostRoleComponent))]
    public class GhostRoleMobSpawnerComponent : GhostRoleComponent
    {
        public override string Name => "GhostRoleMobSpawner";

        [ViewVariables(VVAccess.ReadWrite)] [DataField("deleteOnSpawn")]
        private bool _deleteOnSpawn = true;

        [ViewVariables(VVAccess.ReadWrite)] [DataField("availableTakeovers")]
        private int _availableTakeovers = 1;

        [ViewVariables]
        private int _currentTakeovers = 0;

        [CanBeNull]
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("prototype")]
        public string? Prototype { get; private set; }

        public override bool Take(IPlayerSession session)
        {
            if (Taken)
                return false;

            if (string.IsNullOrEmpty(Prototype))
                throw new NullReferenceException("Prototype string cannot be null or empty!");

            var mob = IoCManager.Resolve<IEntityManager>().SpawnEntity(Prototype, IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(Owner).Coordinates);

            if (MakeSentient)
                MakeSentientCommand.MakeSentient(mob, IoCManager.Resolve<IEntityManager>());

            mob.EnsureComponent<MindComponent>();

            var ghostRoleSystem = EntitySystem.Get<GhostRoleSystem>();
            ghostRoleSystem.GhostRoleInternalCreateMindAndTransfer(session, Owner, mob, this);

            if (++_currentTakeovers < _availableTakeovers)
                return true;

            Taken = true;

            if (_deleteOnSpawn)
                IoCManager.Resolve<IEntityManager>().DeleteEntity(Owner);

            return true;
        }
    }
}
