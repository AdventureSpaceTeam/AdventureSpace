﻿using System;
using Content.Server.Commands;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.Players;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Observer.GhostRoles
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

        [ViewVariables(VVAccess.ReadWrite)] [DataField("makeSentient")]
        private bool _makeSentient = true;

        [ViewVariables(VVAccess.ReadWrite)] [DataField("availableTakeovers")]
        private int _availableTakeovers = 1;

        [ViewVariables]
        private int _currentTakeovers = 0;

        [CanBeNull]
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("prototype")]
        public string Prototype { get; private set; }

        public override bool Take(IPlayerSession session)
        {
            if (Taken)
                return false;

            if(string.IsNullOrEmpty(Prototype))
                throw new NullReferenceException("Prototype string cannot be null or empty!");

            var mob = Owner.EntityManager.SpawnEntity(Prototype, Owner.Transform.Coordinates);

            if(_makeSentient)
                MakeSentientCommand.MakeSentient(mob);

            mob.EnsureComponent<MindComponent>();

            session.ContentData().Mind.TransferTo(mob);

            if (++_currentTakeovers < _availableTakeovers) return true;

            Taken = true;

            if (_deleteOnSpawn)
                Owner.Delete();


            return true;

        }
    }
}
