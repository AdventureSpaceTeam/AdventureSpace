using System;
using System.Linq;
using Content.Server.Construction.Components;
using Content.Server.Disposal.Unit.Components;
using Content.Shared.Acts;
using Content.Shared.Disposal.Components;
using Content.Shared.Popups;
using Content.Shared.Sound;
using Content.Shared.Verbs;
using Robust.Server.Console;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Disposal.Tube.Components
{
    public abstract class DisposalTubeComponent : Component, IDisposalTubeComponent, IBreakAct
    {
        public static readonly TimeSpan ClangDelay = TimeSpan.FromSeconds(0.5);
        public TimeSpan LastClang;

        private bool _connected;
        private bool _broken;
        [DataField("clangSound")] public SoundSpecifier ClangSound = new SoundPathSpecifier("/Audio/Effects/clang.ogg");

        /// <summary>
        ///     Container of entities that are currently inside this tube
        /// </summary>
        [ViewVariables]
        public Container Contents { get; private set; } = default!;

        [ViewVariables]
        private bool Anchored =>
            !Owner.TryGetComponent(out PhysicsComponent? physics) ||
            physics.BodyType == BodyType.Static;

        /// <summary>
        ///     The directions that this tube can connect to others from
        /// </summary>
        /// <returns>a new array of the directions</returns>
        protected abstract Direction[] ConnectableDirections();

        public abstract Direction NextDirection(DisposalHolderComponent holder);

        public virtual Vector2 ExitVector(DisposalHolderComponent holder)
        {
            return NextDirection(holder).ToVec();
        }

        public bool Remove(DisposalHolderComponent holder)
        {
            var removed = Contents.Remove(holder.Owner);
            holder.ExitDisposals();
            return removed;
        }

        public bool TransferTo(DisposalHolderComponent holder, IDisposalTubeComponent to)
        {
            var position = holder.Owner.Transform.LocalPosition;
            if (!to.Contents.Insert(holder.Owner))
            {
                return false;
            }

            holder.Owner.Transform.LocalPosition = position;

            Contents.Remove(holder.Owner);
            holder.EnterTube(to);

            return true;
        }

        // TODO: Make disposal pipes extend the grid
        private void Connect()
        {
            if (_connected || _broken)
            {
                return;
            }

            _connected = true;
        }

        public bool CanConnect(Direction direction, IDisposalTubeComponent with)
        {
            if (!_connected)
            {
                return false;
            }

            if (_broken)
            {
                return false;
            }

            if (!ConnectableDirections().Contains(direction))
            {
                return false;
            }

            return true;
        }

        private void Disconnect()
        {
            if (!_connected)
            {
                return;
            }

            _connected = false;

            foreach (var entity in Contents.ContainedEntities.ToArray())
            {
                if (!entity.TryGetComponent(out DisposalHolderComponent? holder))
                {
                    continue;
                }

                holder.ExitDisposals();
            }
        }

        public void PopupDirections(IEntity entity)
        {
            var directions = string.Join(", ", ConnectableDirections());

            Owner.PopupMessage(entity, Loc.GetString("disposal-tube-component-popup-directions-text", ("directions", directions)));
        }

        private void UpdateVisualState()
        {
            if (!Owner.TryGetComponent(out AppearanceComponent? appearance))
            {
                return;
            }

            var state = _broken
                ? DisposalTubeVisualState.Broken
                : Anchored
                    ? DisposalTubeVisualState.Anchored
                    : DisposalTubeVisualState.Free;

            appearance.SetData(DisposalTubeVisuals.VisualState, state);
        }

        public void AnchoredChanged()
        {
            if (!Owner.TryGetComponent(out PhysicsComponent? physics))
            {
                return;
            }

            if (physics.BodyType == BodyType.Static)
            {
                OnAnchor();
            }
            else
            {
                OnUnAnchor();
            }
        }

        private void OnAnchor()
        {
            Connect();
            UpdateVisualState();
        }

        private void OnUnAnchor()
        {
            Disconnect();
            UpdateVisualState();
        }

        protected override void Initialize()
        {
            base.Initialize();

            Contents = ContainerHelpers.EnsureContainer<Container>(Owner, Name);
            Owner.EnsureComponent<AnchorableComponent>();
        }

        protected override void Startup()
        {
            base.Startup();

            Owner.EnsureComponent<PhysicsComponent>(out var physicsComponent);
            if (physicsComponent.BodyType != BodyType.Static)
            {
                return;
            }

            Connect();
            UpdateVisualState();
        }

        protected override void OnRemove()
        {
            base.OnRemove();

            Disconnect();
        }

        void IBreakAct.OnBreak(BreakageEventArgs eventArgs)
        {
            _broken = true; // TODO: Repair
            Disconnect();
            UpdateVisualState();
        }
    }
}
