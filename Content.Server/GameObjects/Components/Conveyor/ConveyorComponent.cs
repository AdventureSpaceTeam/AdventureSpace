#nullable enable
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Interactable;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.MachineLinking;
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Content.Shared.GameObjects.Components.Conveyor;
using Content.Shared.GameObjects.Components.Interactable;
using Content.Shared.GameObjects.Components.MachineLinking;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Physics;
using Content.Shared.Utility;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Components.Map;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Conveyor
{
    [RegisterComponent]
    public class ConveyorComponent : Component, ISignalReceiver<TwoWayLeverSignal>, ISignalReceiver<bool>
    {
        public override string Name => "Conveyor";

        [ViewVariables] private bool Powered => !Owner.TryGetComponent(out PowerReceiverComponent? receiver) || receiver.Powered;

        /// <summary>
        ///     The angle to move entities by in relation to the owner's rotation.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        private Angle _angle;

        /// <summary>
        ///     The amount of units to move the entity by per second.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        private float _speed;

        private ConveyorState _state;
        /// <summary>
        ///     The current state of this conveyor
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        private ConveyorState State
        {
            get => _state;
            set
            {
                _state = value;
                UpdateAppearance();
            }
        }

        public override void OnAdd()
        {
            base.OnAdd();
            if (Owner.TryGetComponent(out PowerReceiverComponent? receiver))
            {
                receiver.OnPowerStateChanged += OnPowerChanged;
            }
        }

        public override void OnRemove()
        {
            base.OnRemove();
            if (Owner.TryGetComponent(out PowerReceiverComponent? receiver))
            {
                receiver.OnPowerStateChanged -= OnPowerChanged;
            }
        }

        private void OnPowerChanged(object? sender, PowerStateEventArgs e)
        {
            UpdateAppearance();
        }

        private void UpdateAppearance()
        {
            if (Owner.TryGetComponent<AppearanceComponent>(out var appearance))
            {
                if (Powered)
                {
                    appearance.SetData(ConveyorVisuals.State, _state);
                }
                else
                {
                    appearance.SetData(ConveyorVisuals.State, ConveyorState.Off);
                }
            }
        }

        /// <summary>
        ///     Calculates the angle in which entities on top of this conveyor
        ///     belt are pushed in
        /// </summary>
        /// <returns>
        ///     The angle when taking into account if the conveyor is reversed
        /// </returns>
        private Angle GetAngle()
        {
            var adjustment = _state == ConveyorState.Reversed ? MathHelper.Pi : 0;
            var radians = MathHelper.DegreesToRadians(_angle);

            return new Angle(Owner.Transform.LocalRotation.Theta + radians + adjustment);
        }

        private bool CanRun()
        {
            if (State == ConveyorState.Off)
            {
                return false;
            }

            if (Owner.TryGetComponent(out PowerReceiverComponent? receiver) &&
                !receiver.Powered)
            {
                return false;
            }

            if (Owner.HasComponent<ItemComponent>())
            {
                return false;
            }

            return true;
        }

        private bool CanMove(IEntity entity)
        {
            if (entity == Owner)
            {
                return false;
            }

            if (!entity.TryGetComponent(out IPhysicsComponent? physics) ||
                physics.Anchored)
            {
                return false;
            }

            if (entity.HasComponent<ConveyorComponent>())
            {
                return false;
            }

            if (entity.HasComponent<IMapGridComponent>())
            {
                return false;
            }

            if (entity.IsInContainer())
            {
                return false;
            }

            return true;
        }

        public void Update(float frameTime)
        {
            if (!CanRun())
            {
                return;
            }

            var intersecting = Owner.EntityManager.GetEntitiesIntersecting(Owner, true);
            var direction = GetAngle().ToVec();

            foreach (var entity in intersecting)
            {
                if (!CanMove(entity))
                {
                    continue;
                }

                if (entity.TryGetComponent(out IPhysicsComponent? physics))
                {
                    var controller = physics.EnsureController<ConveyedController>();
                    controller.Move(direction, _speed, entity.Transform.WorldPosition - Owner.Transform.WorldPosition);
                }
            }
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _angle, "angle", 0);
            serializer.DataField(ref _speed, "speed", 2);
        }

        public void TriggerSignal(TwoWayLeverSignal signal)
        {
            State = signal switch
            {
                TwoWayLeverSignal.Left => ConveyorState.Reversed,
                TwoWayLeverSignal.Middle => ConveyorState.Off,
                TwoWayLeverSignal.Right => ConveyorState.Forward,
                _ => ConveyorState.Off
            };
        }

        public void TriggerSignal(bool signal)
        {
            State = signal ? ConveyorState.Forward : ConveyorState.Off;
        }
    }
}
