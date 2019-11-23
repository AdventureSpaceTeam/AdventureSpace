﻿using System.Collections.Generic;
using System.Reflection;
using Content.Shared.GameObjects;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using static Content.Shared.GameObjects.EntitySystemMessages.VerbSystemMessages;

namespace Content.Server.GameObjects.EntitySystems
{
    public class VerbSystem : EntitySystem
    {
        #pragma warning disable 649
        [Dependency] private readonly IEntityManager _entityManager;
        [Dependency] private readonly IPlayerManager _playerManager;
        #pragma warning restore 649

        public override void Initialize()
        {
            base.Initialize();

            IoCManager.InjectDependencies(this);
        }

        public override void RegisterMessageTypes()
        {
            base.RegisterMessageTypes();

            RegisterMessageType<RequestVerbsMessage>();
            RegisterMessageType<UseVerbMessage>();
        }

        public override void HandleNetMessage(INetChannel channel, EntitySystemMessage message)
        {
            base.HandleNetMessage(channel, message);

            switch (message)
            {
                case RequestVerbsMessage req:
                {
                    if (!_entityManager.TryGetEntity(req.EntityUid, out var entity))
                    {
                        return;
                    }

                    var session = _playerManager.GetSessionByChannel(channel);
                    var userEntity = session.AttachedEntity;

                    var data = new List<VerbsResponseMessage.VerbData>();
                    //Get verbs, component dependent.
                    foreach (var (component, verb) in VerbUtility.GetVerbs(entity))
                    {
                        if (verb.RequireInteractionRange && !VerbUtility.InVerbUseRange(userEntity, entity))
                            continue;
                        if(VerbUtility.IsVerbInvisible(verb, userEntity, component, out var vis))
                            continue;

                        // TODO: These keys being giant strings is inefficient as hell.
                        data.Add(new VerbsResponseMessage.VerbData(verb.GetText(userEntity, component),
                            $"{component.GetType()}:{verb.GetType()}",
                            vis == VerbVisibility.Visible));
                    }
                    //Get global verbs. Visible for all entities regardless of their components.
                    foreach (var globalVerb in VerbUtility.GetGlobalVerbs(Assembly.GetExecutingAssembly()))
                    {
                        if (globalVerb.RequireInteractionRange && !VerbUtility.InVerbUseRange(userEntity, entity))
                            continue;
                        if(VerbUtility.IsVerbInvisible(globalVerb, userEntity, entity, out var vis))
                            continue;

                        data.Add(new VerbsResponseMessage.VerbData(globalVerb.GetText(userEntity, entity),
                            globalVerb.GetType().ToString(), vis == VerbVisibility.Visible));
                    }

                    var response = new VerbsResponseMessage(data, req.EntityUid);
                    RaiseNetworkEvent(response, channel);
                    break;
                }


                case UseVerbMessage use:
                {
                    if (!_entityManager.TryGetEntity(use.EntityUid, out var entity))
                    {
                        return;
                    }

                    var session = _playerManager.GetSessionByChannel(channel);
                    var userEntity = session.AttachedEntity;

                    foreach (var (component, verb) in VerbUtility.GetVerbs(entity))
                    {
                        if ($"{component.GetType()}:{verb.GetType()}" != use.VerbKey)
                        {
                            continue;
                        }

                        if (verb.RequireInteractionRange)
                        {
                            var distanceSquared = (userEntity.Transform.WorldPosition - entity.Transform.WorldPosition)
                                .LengthSquared;
                            if (distanceSquared > VerbUtility.InteractionRangeSquared)
                            {
                                break;
                            }
                        }

                        verb.Activate(userEntity, component);
                        break;
                    }

                    foreach (var globalVerb in VerbUtility.GetGlobalVerbs(Assembly.GetExecutingAssembly()))
                    {
                        if (globalVerb.GetType().ToString() != use.VerbKey)
                        {
                            continue;
                        }

                        if (globalVerb.RequireInteractionRange)
                        {
                            var distanceSquared = (userEntity.Transform.WorldPosition - entity.Transform.WorldPosition)
                                .LengthSquared;
                            if (distanceSquared > VerbUtility.InteractionRangeSquared)
                            {
                                break;
                            }
                        }

                        globalVerb.Activate(userEntity, entity);
                        break;
                    }

                    break;
                }
            }
        }
    }
}
