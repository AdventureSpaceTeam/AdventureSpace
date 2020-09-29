﻿using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.Mobs.Roles;
using Content.Server.Players;
using Robust.Server.Interfaces.GameObjects;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.ViewVariables;

namespace Content.Server.Mobs
{
    /// <summary>
    ///     A mind represents the IC "mind" of a player. Stores roles currently.
    /// </summary>
    /// <remarks>
    ///     Think of it like this: if a player is supposed to have their memories,
    ///     their mind follows along.
    ///
    ///     Things such as respawning do not follow, because you're a new character.
    ///     Getting borged, cloned, turned into a catbeast, etc... will keep it following you.
    /// </remarks>
    public sealed class Mind
    {
        private readonly ISet<Role> _roles = new HashSet<Role>();

        /// <summary>
        ///     Creates the new mind attached to a specific player session.
        /// </summary>
        /// <param name="userId">The session ID of the owning player.</param>
        public Mind(NetUserId userId)
        {
            UserId = userId;
        }

        // TODO: This session should be able to be changed, probably.
        /// <summary>
        ///     The session ID of the player owning this mind.
        /// </summary>
        [ViewVariables]
        public NetUserId? UserId { get; private set; }

        [ViewVariables]
        public bool IsVisitingEntity => VisitingEntity != null;

        [ViewVariables]
        public IEntity VisitingEntity { get; private set; }

        [ViewVariables] public IEntity CurrentEntity => VisitingEntity ?? OwnedEntity;

        [ViewVariables(VVAccess.ReadWrite)]
        public string CharacterName { get; set; }

        /// <summary>
        ///     The component currently owned by this mind.
        ///     Can be null.
        /// </summary>
        [ViewVariables]
        public MindComponent OwnedMob { get; private set; }

        /// <summary>
        ///     The entity currently owned by this mind.
        ///     Can be null.
        /// </summary>
        [ViewVariables]
        public IEntity OwnedEntity => OwnedMob?.Owner;

        /// <summary>
        ///     An enumerable over all the roles this mind has.
        /// </summary>
        [ViewVariables]
        public IEnumerable<Role> AllRoles => _roles;

        /// <summary>
        ///     The session of the player owning this mind.
        ///     Can be null, in which case the player is currently not logged in.
        /// </summary>
        [ViewVariables]
        public IPlayerSession Session
        {
            get
            {
                if (!UserId.HasValue)
                {
                    return null;
                }
                var playerMgr = IoCManager.Resolve<IPlayerManager>();
                playerMgr.TryGetSessionById(UserId.Value, out var ret);
                return ret;
            }
        }

        /// <summary>
        ///     Gives this mind a new role.
        /// </summary>
        /// <param name="role">The type of the role to give.</param>
        /// <returns>The instance of the role.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown if we already have a role with this type.
        /// </exception>
        public Role AddRole(Role role)
        {
            if (_roles.Contains(role))
            {
                throw new ArgumentException($"We already have this role: {role}");
            }

            _roles.Add(role);
            role.Greet();

            var message = new RoleAddedMessage(role);
            OwnedEntity?.SendMessage(OwnedMob, message);

            return role;
        }

        /// <summary>
        ///     Removes a role from this mind.
        /// </summary>
        /// <param name="role">The type of the role to remove.</param>
        /// <exception cref="ArgumentException">
        ///     Thrown if we do not have this role.
        /// </exception>
        public void RemoveRole(Role role)
        {
            if (!_roles.Contains(role))
            {
                throw new ArgumentException($"We do not have this role: {role}");
            }

            _roles.Remove(role);

            var message = new RoleRemovedMessage(role);
            OwnedEntity?.SendMessage(OwnedMob, message);
        }

        public bool HasRole<T>() where T : Role
        {
            var t = typeof(T);

            return _roles.Any(role => role.GetType() == t);
        }

        /// <summary>
        ///     Transfer this mind's control over to a new entity.
        /// </summary>
        /// <param name="entity">
        ///     The entity to control.
        ///     Can be null, in which case it will simply detach the mind from any entity.
        /// </param>
        /// <exception cref="ArgumentException">
        ///     Thrown if <paramref name="entity"/> is already owned by another mind.
        /// </exception>
        public void TransferTo(IEntity entity)
        {
            MindComponent component = null;
            bool alreadyAttached = false;
            if (entity != null)
            {
                if (!entity.TryGetComponent(out component))
                {
                    component = entity.AddComponent<MindComponent>();
                }
                else if (component.HasMind)
                {
                    // TODO: Kick them out, maybe?
                    throw new ArgumentException("That entity already has a mind.", nameof(entity));
                }

                if (entity.TryGetComponent(out IActorComponent actor))
                {
                    // Happens when transferring to your currently visited entity.
                    if (actor.playerSession != Session)
                    {
                        throw new ArgumentException("Visit target already has a session.", nameof(entity));
                    }

                    alreadyAttached = true;
                }
            }

            OwnedMob?.InternalEjectMind();
            OwnedMob = component;
            OwnedMob?.InternalAssignMind(this);

            // Player is CURRENTLY connected.
            if (Session != null && OwnedMob != null && !alreadyAttached)
            {
                Session.AttachToEntity(entity);
            }

            VisitingEntity = null;
        }

        public void ChangeOwningPlayer(NetUserId? newOwner)
        {
            var playerMgr = IoCManager.Resolve<IPlayerManager>();
            PlayerData newOwnerData = null;
            if (newOwner.HasValue)
            {
                if (!playerMgr.TryGetPlayerData(newOwner.Value, out var uncast))
                {
                    // This restriction is because I'm too lazy to initialize the player data
                    // for a client that hasn't logged in yet.
                    // Go ahead and remove it if you need.
                    throw new ArgumentException("new owner must have previously logged into the server.");
                }

                newOwnerData = uncast.ContentData();
            }

            // Make sure to remove control from our old owner if they're logged in.
            var oldSession = Session;
            oldSession?.AttachToEntity(null);

            if (UserId.HasValue)
            {
                playerMgr.GetPlayerData(UserId.Value).ContentData().Mind = null;
            }

            UserId = newOwner;
            if (!newOwner.HasValue)
            {
                return;
            }

            // Yank new owner out of their old mind too.
            // Can I mention how much I love the word yank?
            newOwnerData.Mind?.ChangeOwningPlayer(null);
            newOwnerData.Mind = this;
        }

        public void Visit(IEntity entity)
        {
            Session?.AttachToEntity(entity);
            VisitingEntity = entity;

            var comp = entity.AddComponent<VisitingMindComponent>();
            comp.Mind = this;
        }

        public void UnVisit()
        {
            if (!IsVisitingEntity)
            {
                return;
            }

            Session?.AttachToEntity(OwnedEntity);
            var oldVisitingEnt = VisitingEntity;
            // Null this before removing the component to avoid any infinite loops.
            VisitingEntity = null;

            if (oldVisitingEnt.HasComponent<VisitingMindComponent>())
            {
                oldVisitingEnt.RemoveComponent<VisitingMindComponent>();
            }
        }
    }
}
