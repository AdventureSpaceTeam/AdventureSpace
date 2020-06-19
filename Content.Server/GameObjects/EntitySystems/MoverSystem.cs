﻿using Content.Server.GameObjects.Components;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.Components.Movement;
using Content.Server.GameObjects.Components.Sound;
using Content.Server.Interfaces.GameObjects.Components.Movement;
using Content.Shared.Audio;
using Content.Shared.GameObjects.Components.Inventory;
using Content.Shared.GameObjects.Components.Movement;
using Content.Shared.Input;
using Content.Shared.Maps;
using Content.Shared.Physics;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.Player;
using Robust.Server.Interfaces.Timing;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Players;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    internal class MoverSystem : EntitySystem
    {
#pragma warning disable 649
        [Dependency] private readonly IPauseManager _pauseManager;
        [Dependency] private readonly IPrototypeManager _prototypeManager;
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager;
        [Dependency] private readonly IMapManager _mapManager;
        [Dependency] private readonly IRobustRandom _robustRandom;
        [Dependency] private readonly IConfigurationManager _configurationManager;
        [Dependency] private readonly IEntityManager _entityManager;
        [Dependency] private readonly IPhysicsManager _physicsManager;
#pragma warning restore 649

        private AudioSystem _audioSystem;

        private const float StepSoundMoveDistanceRunning = 2;
        private const float StepSoundMoveDistanceWalking = 1.5f;

        /// <inheritdoc />
        public override void Initialize()
        {
            EntityQuery = new TypeEntityQuery(typeof(IMoverComponent));

            var moveUpCmdHandler = InputCmdHandler.FromDelegate(
                session => HandleDirChange(session, Direction.North, true),
                session => HandleDirChange(session, Direction.North, false));
            var moveLeftCmdHandler = InputCmdHandler.FromDelegate(
                session => HandleDirChange(session, Direction.West, true),
                session => HandleDirChange(session, Direction.West, false));
            var moveRightCmdHandler = InputCmdHandler.FromDelegate(
                session => HandleDirChange(session, Direction.East, true),
                session => HandleDirChange(session, Direction.East, false));
            var moveDownCmdHandler = InputCmdHandler.FromDelegate(
                session => HandleDirChange(session, Direction.South, true),
                session => HandleDirChange(session, Direction.South, false));
            var walkCmdHandler = InputCmdHandler.FromDelegate(
                session => HandleRunChange(session, true),
                session => HandleRunChange(session, false));

            var input = EntitySystemManager.GetEntitySystem<InputSystem>();

            CommandBinds.Builder
                .Bind(EngineKeyFunctions.MoveUp, moveUpCmdHandler)
                .Bind(EngineKeyFunctions.MoveLeft, moveLeftCmdHandler)
                .Bind(EngineKeyFunctions.MoveRight, moveRightCmdHandler)
                .Bind(EngineKeyFunctions.MoveDown, moveDownCmdHandler)
                .Bind(EngineKeyFunctions.Walk, walkCmdHandler)
                .Register<MoverSystem>();

            SubscribeLocalEvent<PlayerAttachSystemMessage>(PlayerAttached);
            SubscribeLocalEvent<PlayerDetachedSystemMessage>(PlayerDetached);

            _audioSystem = EntitySystemManager.GetEntitySystem<AudioSystem>();

            _configurationManager.RegisterCVar("game.diagonalmovement", true, CVar.ARCHIVE);
        }

        private static void PlayerAttached(PlayerAttachSystemMessage ev)
        {
            if (!ev.Entity.HasComponent<IMoverComponent>())
            {
                ev.Entity.AddComponent<PlayerInputMoverComponent>();
            }
        }

        private static void PlayerDetached(PlayerDetachedSystemMessage ev)
        {
            if (ev.Entity.HasComponent<PlayerInputMoverComponent>())
            {
                ev.Entity.RemoveComponent<PlayerInputMoverComponent>();
            }
        }

        /// <inheritdoc />
        public override void Shutdown()
        {
            CommandBinds.Unregister<MoverSystem>();
            base.Shutdown();
        }

        /// <inheritdoc />
        public override void Update(float frameTime)
        {
            foreach (var entity in RelevantEntities)
            {
                if (_pauseManager.IsEntityPaused(entity))
                {
                    continue;
                }
                var mover = entity.GetComponent<IMoverComponent>();
                var physics = entity.GetComponent<PhysicsComponent>();
                if (entity.TryGetComponent<CollidableComponent>(out var collider))
                {
                    UpdateKinematics(entity.Transform, mover, physics, collider);
                }
                else
                {
                    UpdateKinematics(entity.Transform, mover, physics);
                }
            }
        }

        private void UpdateKinematics(ITransformComponent transform, IMoverComponent mover, PhysicsComponent physics, CollidableComponent collider = null)
        {
            if (physics.Controller == null)
            {
                // Set up controller
                physics.SetController<MoverController>();
            }

            var weightless = !transform.Owner.HasComponent<MovementIgnoreGravityComponent>() &&
                             _physicsManager.IsWeightless(transform.GridPosition);

            if (weightless && collider != null)
            {
                // No gravity: is our entity touching anything?
                var touching = IsAroundCollider(transform, mover, collider);

                if (!touching)
                {
                    return;
                }
            }

            if (mover.VelocityDir.LengthSquared < 0.001 || !ActionBlockerSystem.CanMove(mover.Owner) && !weightless)
            {
                (physics.Controller as MoverController)?.StopMoving();
            }
            else
            {
                if (weightless)
                {
                    (physics.Controller as MoverController)?.Push(mover.VelocityDir, mover.CurrentPushSpeed);
                    transform.LocalRotation = mover.VelocityDir.GetDir().ToAngle();
                    return;
                }
                (physics.Controller as MoverController)?.Move(mover.VelocityDir,
                    mover.Sprinting ? mover.CurrentSprintSpeed : mover.CurrentWalkSpeed);
                transform.LocalRotation = mover.VelocityDir.GetDir().ToAngle();

                // Handle footsteps.
                if (_mapManager.GridExists(mover.LastPosition.GridID))
                {
                    // Can happen when teleporting between grids.
                    var distance = transform.GridPosition.Distance(_mapManager, mover.LastPosition);
                    mover.StepSoundDistance += distance;
                }

                mover.LastPosition = transform.GridPosition;
                float distanceNeeded;
                if (mover.Sprinting)
                {
                    distanceNeeded = StepSoundMoveDistanceRunning;
                }
                else
                {
                    distanceNeeded = StepSoundMoveDistanceWalking;
                }
                if (mover.StepSoundDistance > distanceNeeded)
                {
                    mover.StepSoundDistance = 0;

                    if (!mover.Owner.HasComponent<FootstepSoundComponent>())
                    {
                        return;
                    }

                    if (mover.Owner.TryGetComponent<InventoryComponent>(out var inventory)
                        && inventory.TryGetSlotItem<ItemComponent>(EquipmentSlotDefines.Slots.SHOES, out var item)
                        && item.Owner.TryGetComponent<FootstepModifierComponent>(out var modifier))
                    {
                        modifier.PlayFootstep();
                    }
                    else
                    {
                        PlayFootstepSound(transform.GridPosition);
                    }
                }
            }
        }

        private bool IsAroundCollider(ITransformComponent transform, IMoverComponent mover, CollidableComponent collider)
        {
            foreach (var entity in _entityManager.GetEntitiesInRange(transform.Owner, mover.GrabRange, true))
            {
                if (entity == transform.Owner)
                {
                    continue; // Don't try to push off of yourself!
                }

                if (!entity.TryGetComponent<CollidableComponent>(out var otherCollider))
                {
                    continue;
                }

                var touching = ((collider.CollisionMask & otherCollider.CollisionLayer) != 0x0
                                || (otherCollider.CollisionMask & collider.CollisionLayer) != 0x0) // Ensure collision
                               && !entity.HasComponent<ItemComponent>(); // This can't be an item

                if (touching)
                {
                    return true;
                }
            }

            return false;
        }

        private static void HandleDirChange(ICommonSession session, Direction dir, bool state)
        {
            var playerSes = session as IPlayerSession;
            if (!TryGetAttachedComponent(playerSes, out IMoverComponent moverComp))
                return;

            var owner = playerSes?.AttachedEntity;

            if (owner != null)
            {
                foreach (var comp in owner.GetAllComponents<IRelayMoveInput>())
                {
                    comp.MoveInputPressed(playerSes);
                }
            }

            moverComp.SetVelocityDirection(dir, state);
        }

        private static void HandleRunChange(ICommonSession session, bool walking)
        {
            if (!TryGetAttachedComponent(session as IPlayerSession, out PlayerInputMoverComponent moverComp))
                return;

            moverComp.Sprinting = !walking;
        }

        private static bool TryGetAttachedComponent<T>(IPlayerSession session, out T component)
            where T : IComponent
        {
            component = default;

            var ent = session.AttachedEntity;

            if (ent == null || !ent.IsValid())
                return false;

            if (!ent.TryGetComponent(out T comp))
                return false;

            component = comp;
            return true;
        }

        private void PlayFootstepSound(GridCoordinates coordinates)
        {
            // Step one: figure out sound collection prototype.
            var grid = _mapManager.GetGrid(coordinates.GridID);
            var tile = grid.GetTileRef(coordinates);

            // If the coordinates have a catwalk, it's always catwalk.
            string soundCollectionName;
            var catwalk = false;
            foreach (var maybeCatwalk in grid.GetSnapGridCell(tile.GridIndices, SnapGridOffset.Center))
            {
                if (maybeCatwalk.Owner.HasComponent<CatwalkComponent>())
                {
                    catwalk = true;
                    break;
                }
            }

            if (catwalk)
            {
                // Catwalk overrides tile sound.s
                soundCollectionName = "footstep_catwalk";
            }
            else
            {
                // Walking on a tile.
                var def = (ContentTileDefinition)_tileDefinitionManager[tile.Tile.TypeId];
                if (def.FootstepSounds == null)
                {
                    // Nothing to play, oh well.
                    return;
                }
                soundCollectionName = def.FootstepSounds;
            }

            // Ok well we know the position of the
            try
            {
                var soundCollection = _prototypeManager.Index<SoundCollectionPrototype>(soundCollectionName);
                var file = _robustRandom.Pick(soundCollection.PickFiles);
                _audioSystem.PlayAtCoords(file, coordinates);
            }
            catch (UnknownPrototypeException)
            {
                // Shouldn't crash over a sound
                Logger.ErrorS("sound", $"Unable to find sound collection for {soundCollectionName}");
            }
        }
    }
}
