﻿#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Construction;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.Stack;
using Content.Server.GameObjects.EntitySystems.DoAfter;
using Content.Shared.Construction;
using Content.Shared.GameObjects.Components;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces;
using Content.Shared.Utility;
using JetBrains.Annotations;
using Robust.Server.GameObjects.Components.Container;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Players;
using Robust.Shared.Prototypes;
using Robust.Shared.Timers;


namespace Content.Server.GameObjects.EntitySystems
{
    /// <summary>
    /// The server-side implementation of the construction system, which is used for constructing entities in game.
    /// </summary>
    [UsedImplicitly]
    internal class ConstructionSystem : SharedConstructionSystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;

        private readonly Dictionary<ICommonSession, HashSet<int>> _beingBuilt = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeNetworkEvent<TryStartStructureConstructionMessage>(HandleStartStructureConstruction);
            SubscribeNetworkEvent<TryStartItemConstructionMessage>(HandleStartItemConstruction);
        }

        private IEnumerable<IEntity> EnumerateNearby(IEntity user)
        {
            if (user.TryGetComponent(out HandsComponent? hands))
            {
                foreach (var itemComponent in hands?.GetAllHeldItems()!)
                {
                    if (itemComponent.Owner.TryGetComponent(out ServerStorageComponent? storage))
                    {
                        foreach (var storedEntity in storage.StoredEntities!)
                        {
                            yield return storedEntity;
                        }
                    }

                    yield return itemComponent.Owner;
                }
            }

            if (user!.TryGetComponent(out InventoryComponent? inventory))
            {
                foreach (var held in inventory.GetAllHeldItems())
                {
                    if (held.TryGetComponent(out ServerStorageComponent? storage))
                    {
                        foreach (var storedEntity in storage.StoredEntities!)
                        {
                            yield return storedEntity;
                        }
                    }

                    yield return held;
                }
            }

            foreach (var near in EntityManager.GetEntitiesInRange(user!, 2f, true))
            {
                yield return near;
            }
        }

        private async Task<IEntity?> Construct(IEntity user, string materialContainer, ConstructionGraphPrototype graph, ConstructionGraphEdge edge, ConstructionGraphNode targetNode)
        {
            // We need a place to hold our construction items!
            var container = ContainerManagerComponent.Ensure<Container>(materialContainer, user, out var existed);

            if (existed)
            {
                user.PopupMessageCursor(Loc.GetString("You can't start another construction now!"));
                return null;
            }

            var containers = new Dictionary<string, Container>();

            var doAfterTime = 0f;

            // HOLY SHIT THIS IS SOME HACKY CODE.
            // But I'd rather do this shit than risk having collisions with other containers.
            Container GetContainer(string name)
            {
                if (containers!.ContainsKey(name))
                    return containers[name];

                while (true)
                {
                    var random = _robustRandom.Next();
                    var c = ContainerManagerComponent.Ensure<Container>(random.ToString(), user!, out var existed);

                    if (existed) continue;

                    containers[name] = c;
                    return c;
                }
            }

            void FailCleanup()
            {
                foreach (var entity in container!.ContainedEntities.ToArray())
                {
                    container.Remove(entity);
                }

                foreach (var cont in containers!.Values)
                {
                    foreach (var entity in cont.ContainedEntities.ToArray())
                    {
                        cont.Remove(entity);
                    }
                }

                // If we don't do this, items are invisible for some fucking reason. Nice.
                Timer.Spawn(1, ShutdownContainers);
            }

            void ShutdownContainers()
            {
                container!.Shutdown();
                foreach (var c in containers!.Values.ToArray())
                {
                    c.Shutdown();
                }
            }

            var failed = false;

            var steps = new List<ConstructionGraphStep>();

            foreach (var step in edge.Steps)
            {
                doAfterTime += step.DoAfter;

                var handled = false;

                switch (step)
                {
                    case MaterialConstructionGraphStep materialStep:
                        foreach (var entity in EnumerateNearby(user))
                        {
                            if (!materialStep.EntityValid(entity, out var sharedStack))
                                continue;

                            var stack = (StackComponent) sharedStack;

                            if (!stack.Split(materialStep.Amount, user.ToCoordinates(), out var newStack))
                                continue;

                            if (string.IsNullOrEmpty(materialStep.Store))
                            {
                                if (!container.Insert(newStack))
                                    continue;
                            }
                            else if (!GetContainer(materialStep.Store).Insert(newStack))
                                    continue;

                            handled = true;
                            break;
                        }

                        break;

                    case ComponentConstructionGraphStep componentStep:
                        foreach (var entity in EnumerateNearby(user))
                        {
                            if (!componentStep.EntityValid(entity))
                                continue;

                            if (string.IsNullOrEmpty(componentStep.Store))
                            {
                                if (!container.Insert(entity))
                                    continue;
                            }
                            else if (!GetContainer(componentStep.Store).Insert(entity))
                                continue;

                            handled = true;
                            break;
                        }

                        break;

                    case PrototypeConstructionGraphStep prototypeStep:
                        foreach (var entity in EnumerateNearby(user))
                        {
                            if (!prototypeStep.EntityValid(entity))
                                continue;

                            if (string.IsNullOrEmpty(prototypeStep.Store))
                            {
                                if (!container.Insert(entity))
                                    continue;
                            }
                            else if (!GetContainer(prototypeStep.Store).Insert(entity))
                            {
                                continue;
                            }

                            handled = true;
                            break;
                        }

                        break;
                }

                if (handled == false)
                {
                    failed = true;
                    break;
                }

                steps.Add(step);
            }

            if (failed)
            {
                user.PopupMessageCursor(Loc.GetString("You don't have the materials to build that!"));
                FailCleanup();
                return null;
            }

            var doAfterSystem = Get<DoAfterSystem>();

            var doAfterArgs = new DoAfterEventArgs(user, doAfterTime)
            {
                BreakOnDamage = true,
                BreakOnStun = true,
                BreakOnTargetMove = false,
                BreakOnUserMove = true,
                NeedHand = true,
            };

            if (await doAfterSystem.DoAfter(doAfterArgs) == DoAfterStatus.Cancelled)
            {
                FailCleanup();
                return null;
            }

            var newEntity = EntityManager.SpawnEntity(graph.Nodes[edge.Target].Entity, user.Transform.Coordinates);

            // Yes, this should throw if it's missing the component.
            var construction = newEntity.GetComponent<ConstructionComponent>();

            // We attempt to set the pathfinding target.
            construction.Target = targetNode;

            // We preserve the containers...
            foreach (var (name, cont) in containers)
            {
                var newCont = ContainerManagerComponent.Ensure<Container>(name, newEntity);

                foreach (var entity in cont.ContainedEntities.ToArray())
                {
                    cont.ForceRemove(entity);
                    newCont.Insert(entity);
                }
            }

            // We now get rid of all them.
            ShutdownContainers();

            // We have step completed steps!
            foreach (var step in steps)
            {
                foreach (var completed in step.Completed)
                {
                    await completed.PerformAction(newEntity, user);
                }
            }

            // And we also have edge completed effects!
            foreach (var completed in edge.Completed)
            {
                await completed.PerformAction(newEntity, user);
            }

            return newEntity;
        }

        private async void HandleStartItemConstruction(TryStartItemConstructionMessage ev, EntitySessionEventArgs args)
        {
            if (!_prototypeManager.TryIndex(ev.PrototypeName, out ConstructionPrototype constructionPrototype))
            {
                Logger.Error($"Tried to start construction of invalid recipe '{ev.PrototypeName}'!");
                return;
            }

            if (!_prototypeManager.TryIndex(constructionPrototype.Graph, out ConstructionGraphPrototype constructionGraph))
            {
                Logger.Error($"Invalid construction graph '{constructionPrototype.Graph}' in recipe '{ev.PrototypeName}'!");
                return;
            }

            var startNode = constructionGraph.Nodes[constructionPrototype.StartNode];
            var targetNode = constructionGraph.Nodes[constructionPrototype.TargetNode];
            var pathFind = constructionGraph.Path(startNode.Name, targetNode.Name);

            var user = args.SenderSession.AttachedEntity;

            if (user == null || !ActionBlockerSystem.CanInteract(user)) return;

            if (!user.TryGetComponent(out HandsComponent? hands)) return;

            foreach (var condition in constructionPrototype.Conditions)
            {
                if (!condition.Condition(user, user.ToCoordinates(), Direction.South))
                    return;
            }

            if(pathFind == null)
                throw new InvalidDataException($"Can't find path from starting node to target node in construction! Recipe: {ev.PrototypeName}");

            var edge = startNode.GetEdge(pathFind[0].Name);

            if(edge == null)
                throw new InvalidDataException($"Can't find edge from starting node to the next node in pathfinding! Recipe: {ev.PrototypeName}");

            // No support for conditions here!

            foreach (var step in edge.Steps)
            {
                switch (step)
                {
                    case ToolConstructionGraphStep _:
                    case NestedConstructionGraphStep _:
                        throw new InvalidDataException("Invalid first step for construction recipe!");
                }
            }

            var item = await Construct(user, "item_construction", constructionGraph, edge, targetNode);

            if(item != null && item.TryGetComponent(out ItemComponent? itemComp))
                hands.PutInHandOrDrop(itemComp);
        }

        private async void HandleStartStructureConstruction(TryStartStructureConstructionMessage ev, EntitySessionEventArgs args)
        {
            if (!_prototypeManager.TryIndex(ev.PrototypeName, out ConstructionPrototype constructionPrototype))
            {
                Logger.Error($"Tried to start construction of invalid recipe '{ev.PrototypeName}'!");
                RaiseNetworkEvent(new AckStructureConstructionMessage(ev.Ack));
                return;
            }

            if (!_prototypeManager.TryIndex(constructionPrototype.Graph, out ConstructionGraphPrototype constructionGraph))
            {
                Logger.Error($"Invalid construction graph '{constructionPrototype.Graph}' in recipe '{ev.PrototypeName}'!");
                RaiseNetworkEvent(new AckStructureConstructionMessage(ev.Ack));
                return;
            }

            var startNode = constructionGraph.Nodes[constructionPrototype.StartNode];
            var targetNode = constructionGraph.Nodes[constructionPrototype.TargetNode];
            var pathFind = constructionGraph.Path(startNode.Name, targetNode.Name);

            var user = args.SenderSession.AttachedEntity;

            if (_beingBuilt.TryGetValue(args.SenderSession, out var set))
            {
                if (!set.Add(ev.Ack))
                {
                    user.PopupMessageCursor(Loc.GetString("You are already building that!"));
                    return;
                }
            }
            else
            {
                var newSet = new HashSet<int> {ev.Ack};
                _beingBuilt[args.SenderSession] = newSet;
            }

            foreach (var condition in constructionPrototype.Conditions)
            {
                if (!condition.Condition(user, ev.Location, ev.Angle.GetCardinalDir()))
                {
                    Cleanup();
                    return;
                }
            }

            void Cleanup()
            {
                _beingBuilt[args.SenderSession].Remove(ev.Ack);
            }

            if (user == null
                || !ActionBlockerSystem.CanInteract(user)
                || !user.TryGetComponent(out HandsComponent? hands) || hands.GetActiveHand == null
                || !user.InRangeUnobstructed(ev.Location, ignoreInsideBlocker:constructionPrototype.CanBuildInImpassable))
            {
                Cleanup();
                return;
            }

            if(pathFind == null)
                throw new InvalidDataException($"Can't find path from starting node to target node in construction! Recipe: {ev.PrototypeName}");

            var edge = startNode.GetEdge(pathFind[0].Name);

            if(edge == null)
                throw new InvalidDataException($"Can't find edge from starting node to the next node in pathfinding! Recipe: {ev.PrototypeName}");

            var valid = false;
            var holding = hands.GetActiveHand?.Owner;

            if (holding == null)
            {
                Cleanup();
                return;
            }

            // No support for conditions here!

            foreach (var step in edge.Steps)
            {
                switch (step)
                {
                    case EntityInsertConstructionGraphStep entityInsert:
                        if (entityInsert.EntityValid(holding))
                            valid = true;
                        break;
                    case ToolConstructionGraphStep _:
                    case NestedConstructionGraphStep _:
                        throw new InvalidDataException("Invalid first step for item recipe!");
                }

                if (valid)
                    break;
            }

            if (!valid)
            {
                Cleanup();
                return;
            }

            var structure = await Construct(user, (ev.Ack + constructionPrototype.GetHashCode()).ToString(), constructionGraph, edge, targetNode);

            if (structure == null)
            {
                Cleanup();
                return;
            }

            structure.Transform.Coordinates = ev.Location;
            structure.Transform.LocalRotation = constructionPrototype.CanRotate ? ev.Angle : Angle.South;

            RaiseNetworkEvent(new AckStructureConstructionMessage(ev.Ack));

            Cleanup();
        }
    }
}
