using System.Diagnostics.CodeAnalysis;
using Content.Shared.GameObjects.Components.Movement;
using Content.Shared.Physics;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
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
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Players;
using Robust.Shared.Prototypes;

#nullable enable

namespace Content.Shared.GameObjects.EntitySystems
{
    public abstract class SharedMoverSystem : EntitySystem
    {
        // [Dependency] private readonly IPauseManager _pauseManager;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IPhysicsManager _physicsManager = default!;
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;

        public override void Initialize()
        {
            base.Initialize();

            EntityQuery = new TypeEntityQuery(typeof(IMoverComponent));

            var moveUpCmdHandler = new MoverDirInputCmdHandler(Direction.North);
            var moveLeftCmdHandler = new MoverDirInputCmdHandler(Direction.West);
            var moveRightCmdHandler = new MoverDirInputCmdHandler(Direction.East);
            var moveDownCmdHandler = new MoverDirInputCmdHandler(Direction.South);

            CommandBinds.Builder
                .Bind(EngineKeyFunctions.MoveUp, moveUpCmdHandler)
                .Bind(EngineKeyFunctions.MoveLeft, moveLeftCmdHandler)
                .Bind(EngineKeyFunctions.MoveRight, moveRightCmdHandler)
                .Bind(EngineKeyFunctions.MoveDown, moveDownCmdHandler)
                .Bind(EngineKeyFunctions.Run, new SprintInputCmdHandler())
                .Register<SharedMoverSystem>();

            _configurationManager.RegisterCVar("game.diagonalmovement", true, CVar.ARCHIVE);
        }

        /// <inheritdoc />
        public override void Shutdown()
        {
            CommandBinds.Unregister<SharedMoverSystem>();
            base.Shutdown();
        }


        protected void UpdateKinematics(ITransformComponent transform, IMoverComponent mover, SharedPhysicsComponent physics,
            CollidableComponent? collider = null)
        {
            if (physics.Controller == null)
            {
                // Set up controller
                SetController(physics);
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

            // TODO: movement check.
            var (walkDir, sprintDir) = mover.VelocityDir;
            var combined = walkDir + sprintDir;
            if (combined.LengthSquared < 0.001 || !ActionBlockerSystem.CanMove(mover.Owner) && !weightless)
            {
                (physics.Controller as MoverController)?.StopMoving();
            }
            else
            {
                //Console.WriteLine($"{IoCManager.Resolve<IGameTiming>().TickStamp}: {combined}");

                if (weightless)
                {
                    (physics.Controller as MoverController)?.Push(combined, mover.CurrentPushSpeed);
                    transform.LocalRotation = walkDir.GetDir().ToAngle();
                    return;
                }

                var total = walkDir * mover.CurrentWalkSpeed + sprintDir * mover.CurrentSprintSpeed;
                //Console.WriteLine($"{walkDir} ({mover.CurrentWalkSpeed}) + {sprintDir} ({mover.CurrentSprintSpeed}): {total}");

                (physics.Controller as MoverController)?.Move(total, 1);
                transform.LocalRotation = total.GetDir().ToAngle();

                HandleFootsteps(mover);
            }
        }

        protected virtual void HandleFootsteps(IMoverComponent mover)
        {

        }

        protected abstract void SetController(SharedPhysicsComponent physics);

        private bool IsAroundCollider(ITransformComponent transform, IMoverComponent mover,
            CollidableComponent collider)
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

                // TODO: Item check.
                var touching = ((collider.CollisionMask & otherCollider.CollisionLayer) != 0x0
                                || (otherCollider.CollisionMask & collider.CollisionLayer) != 0x0) // Ensure collision
                               && true; // !entity.HasComponent<ItemComponent>(); // This can't be an item

                if (touching)
                {
                    return true;
                }
            }

            return false;
        }


        private static void HandleDirChange(ICommonSession? session, Direction dir, ushort subTick, bool state)
        {
            if (!TryGetAttachedComponent<IMoverComponent>(session, out var moverComp))
                return;

            var owner = session?.AttachedEntity;

            if (owner != null)
            {
                foreach (var comp in owner.GetAllComponents<IRelayMoveInput>())
                {
                    comp.MoveInputPressed(session);
                }
            }

            moverComp.SetVelocityDirection(dir, subTick, state);
        }

        private static void HandleRunChange(ICommonSession? session, ushort subTick, bool running)
        {
            if (!TryGetAttachedComponent<IMoverComponent>(session, out var moverComp))
            {
                return;
            }

            moverComp.SetSprinting(subTick, running);
        }

        private static bool TryGetAttachedComponent<T>(ICommonSession? session, [MaybeNullWhen(false)] out T component)
            where T : IComponent
        {
            component = default;

            var ent = session?.AttachedEntity;

            if (ent == null || !ent.IsValid())
                return false;

            if (!ent.TryGetComponent(out T comp))
                return false;

            component = comp;
            return true;
        }

        private sealed class MoverDirInputCmdHandler : InputCmdHandler
        {
            private readonly Direction _dir;

            public MoverDirInputCmdHandler(Direction dir)
            {
                _dir = dir;
            }

            public override bool HandleCmdMessage(ICommonSession? session, InputCmdMessage message)
            {
                if (!(message is FullInputCmdMessage full))
                {
                    return false;
                }

                HandleDirChange(session, _dir, message.SubTick, full.State == BoundKeyState.Down);
                return false;
            }
        }

        private sealed class SprintInputCmdHandler : InputCmdHandler
        {
            public override bool HandleCmdMessage(ICommonSession? session, InputCmdMessage message)
            {
                if (!(message is FullInputCmdMessage full))
                {
                    return false;
                }

                HandleRunChange(session, full.SubTick, full.State == BoundKeyState.Down);
                return false;
            }
        }
    }
}
