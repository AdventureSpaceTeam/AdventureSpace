﻿#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.Components.Mobs.State;
using Content.Server.GameObjects.Components.Pulling;
using Content.Server.GameObjects.Components.Strap;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Alert;
using Content.Shared.GameObjects.Components.Buckle;
using Content.Shared.GameObjects.Components.Strap;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.GameObjects.Verbs;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Utility;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.ComponentDependencies;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Buckle
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedBuckleComponent))]
    public class BuckleComponent : SharedBuckleComponent, IInteractHand
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        [ComponentDependency] public readonly AppearanceComponent? AppearanceComponent = null;
        [ComponentDependency] private readonly ServerAlertsComponent? _serverAlertsComponent = null;
        [ComponentDependency] private readonly StunnableComponent? _stunnableComponent = null;
        [ComponentDependency] private readonly MobStateComponent? _mobStateComponent = null;

        private int _size;

        /// <summary>
        ///     The amount of time that must pass for this entity to
        ///     be able to unbuckle after recently buckling.
        /// </summary>
        [ViewVariables]
        private TimeSpan _unbuckleDelay;

        /// <summary>
        ///     The time that this entity buckled at.
        /// </summary>
        [ViewVariables]
        private TimeSpan _buckleTime;

        /// <summary>
        ///     The position offset that is being applied to this entity if buckled.
        /// </summary>
        public Vector2 BuckleOffset { get; private set; }

        private StrapComponent? _buckledTo;


        /// <summary>
        ///     The strap that this component is buckled to.
        /// </summary>
        [ViewVariables]
        public StrapComponent? BuckledTo
        {
            get => _buckledTo;
            private set
            {
                _buckledTo = value;
                _buckleTime = _gameTiming.CurTime;
                Dirty();
            }
        }

        [ViewVariables]
        public override bool Buckled => BuckledTo != null;

        /// <summary>
        ///     The amount of space that this entity occupies in a
        ///     <see cref="StrapComponent"/>.
        /// </summary>
        [ViewVariables]
        public int Size => _size;

        /// <summary>
        ///     Shows or hides the buckled status effect depending on if the
        ///     entity is buckled or not.
        /// </summary>
        private void UpdateBuckleStatus()
        {
            if (_serverAlertsComponent == null)
            {
                return;
            }

            if (Buckled)
            {
                _serverAlertsComponent.ShowAlert(BuckledTo?.BuckledAlertType ?? AlertType.Buckled);
            }
            else
            {
                _serverAlertsComponent.ClearAlertCategory(AlertCategory.Buckled);
            }
        }


        /// <summary>
        ///     Reattaches this entity to the strap, modifying its position and rotation.
        /// </summary>
        /// <param name="strap">The strap to reattach to.</param>
        public void ReAttach(StrapComponent strap)
        {
            var ownTransform = Owner.Transform;
            var strapTransform = strap.Owner.Transform;

            ownTransform.AttachParent(strapTransform);

            switch (strap.Position)
            {
                case StrapPosition.None:
                    ownTransform.WorldRotation = strapTransform.WorldRotation;
                    break;
                case StrapPosition.Stand:
                    EntitySystem.Get<StandingStateSystem>().Standing(Owner);
                    ownTransform.WorldRotation = strapTransform.WorldRotation;
                    break;
                case StrapPosition.Down:
                    EntitySystem.Get<StandingStateSystem>().Down(Owner, force: true);
                    ownTransform.WorldRotation = Angle.South;
                    break;
            }

            // Assign BuckleOffset first, before causing a MoveEvent to fire
            if (strapTransform.WorldRotation.GetCardinalDir() == Direction.North)
            {
                BuckleOffset = (0, 0.15f);
                ownTransform.WorldPosition = strapTransform.WorldPosition + BuckleOffset;
            }
            else
            {
                BuckleOffset = Vector2.Zero;
                ownTransform.WorldPosition = strapTransform.WorldPosition;
            }
        }

        private bool CanBuckle(IEntity? user, IEntity to, [NotNullWhen(true)] out StrapComponent? strap)
        {
            strap = null;

            if (user == null || user == to)
            {
                return false;
            }

            if (!ActionBlockerSystem.CanInteract(user))
            {
                user.PopupMessage(Loc.GetString("You can't do that!"));
                return false;
            }

            if (!to.TryGetComponent(out strap))
            {
                var message = Loc.GetString(Owner == user
                    ? "You can't buckle yourself there!"
                    : "You can't buckle {0:them} there!", Owner);
                Owner.PopupMessage(user, message);

                return false;
            }

            var component = strap;
            bool Ignored(IEntity entity) => entity == Owner || entity == user || entity == component.Owner;

            if (!Owner.InRangeUnobstructed(strap, Range, predicate: Ignored, popup: true))
            {
                return false;
            }

            // If in a container
            if (Owner.TryGetContainer(out var ownerContainer))
            {
                // And not in the same container as the strap
                if (!strap.Owner.TryGetContainer(out var strapContainer) ||
                    ownerContainer != strapContainer)
                {
                    return false;
                }
            }

            if (!user.HasComponent<HandsComponent>())
            {
                user.PopupMessage(Loc.GetString("You don't have hands!"));
                return false;
            }

            if (Buckled)
            {
                var message = Loc.GetString(Owner == user
                    ? "You are already buckled in!"
                    : "{0:They} are already buckled in!", Owner);
                Owner.PopupMessage(user, message);

                return false;
            }

            var parent = to.Transform.Parent;
            while (parent != null)
            {
                if (parent == user.Transform)
                {
                    var message = Loc.GetString(Owner == user
                        ? "You can't buckle yourself there!"
                        : "You can't buckle {0:them} there!", Owner);
                    Owner.PopupMessage(user, message);

                    return false;
                }

                parent = parent.Parent;
            }

            if (!strap.HasSpace(this))
            {
                var message = Loc.GetString(Owner == user
                    ? "You can't fit there!"
                    : "{0:They} can't fit there!", Owner);
                Owner.PopupMessage(user, message);

                return false;
            }

            return true;
        }

        public override bool TryBuckle(IEntity user, IEntity to)
        {
            if (!CanBuckle(user, to, out var strap))
            {
                return false;
            }

            EntitySystem.Get<AudioSystem>().PlayFromEntity(strap.BuckleSound, Owner);

            if (!strap.TryAdd(this))
            {
                var message = Loc.GetString(Owner == user
                    ? "You can't buckle yourself there!"
                    : "You can't buckle {0:them} there!", Owner);
                Owner.PopupMessage(user, message);
                return false;
            }

            AppearanceComponent?.SetData(BuckleVisuals.Buckled, true);

            BuckledTo = strap;
            LastEntityBuckledTo = BuckledTo.Owner.Uid;
            DontCollide = true;

            ReAttach(strap);
            UpdateBuckleStatus();

            SendMessage(new BuckleMessage(Owner, to));

            if (Owner.TryGetComponent(out PullableComponent? pullableComponent))
            {
                if (pullableComponent.Puller != null)
                {
                    pullableComponent.TryStopPull();
                }
            }

            return true;
        }

        /// <summary>
        ///     Tries to unbuckle the Owner of this component from its current strap.
        /// </summary>
        /// <param name="user">The entity doing the unbuckling.</param>
        /// <param name="force">
        ///     Whether to force the unbuckling or not. Does not guarantee true to
        ///     be returned, but guarantees the owner to be unbuckled afterwards.
        /// </param>
        /// <returns>
        ///     true if the owner was unbuckled, otherwise false even if the owner
        ///     was previously already unbuckled.
        /// </returns>
        public bool TryUnbuckle(IEntity user, bool force = false)
        {
            if (BuckledTo == null)
            {
                return false;
            }

            var oldBuckledTo = BuckledTo;

            if (!force)
            {
                if (_gameTiming.CurTime < _buckleTime + _unbuckleDelay)
                {
                    return false;
                }

                if (!ActionBlockerSystem.CanInteract(user))
                {
                    user.PopupMessage(Loc.GetString("You can't do that!"));
                    return false;
                }

                if (!user.InRangeUnobstructed(oldBuckledTo, Range, popup: true))
                {
                    return false;
                }
            }

            BuckledTo = null;

            if (Owner.Transform.Parent == oldBuckledTo.Owner.Transform)
            {
                Owner.Transform.AttachParentToContainerOrGrid();
                Owner.Transform.WorldRotation = oldBuckledTo.Owner.Transform.WorldRotation;
            }

            AppearanceComponent?.SetData(BuckleVisuals.Buckled, false);

            if (_stunnableComponent != null && _stunnableComponent.KnockedDown)
            {
                EntitySystem.Get<StandingStateSystem>().Down(Owner);
            }
            else
            {
                EntitySystem.Get<StandingStateSystem>().Standing(Owner);
            }

            _mobStateComponent?.CurrentState?.EnterState(Owner);

            UpdateBuckleStatus();

            oldBuckledTo.Remove(this);
            EntitySystem.Get<AudioSystem>().PlayFromEntity(oldBuckledTo.UnbuckleSound, Owner);

            SendMessage(new UnbuckleMessage(Owner, oldBuckledTo.Owner));

            return true;
        }

        /// <summary>
        ///     Makes an entity toggle the buckling status of the owner to a
        ///     specific entity.
        /// </summary>
        /// <param name="user">The entity doing the buckling/unbuckling.</param>
        /// <param name="to">
        ///     The entity to toggle the buckle status of the owner to.
        /// </param>
        /// <param name="force">
        ///     Whether to force the unbuckling or not, if it happens. Does not
        ///     guarantee true to be returned, but guarantees the owner to be
        ///     unbuckled afterwards.
        /// </param>
        /// <returns>true if the buckling status was changed, false otherwise.</returns>
        public bool ToggleBuckle(IEntity user, IEntity to, bool force = false)
        {
            if (BuckledTo?.Owner == to)
            {
                return TryUnbuckle(user, force);
            }

            return TryBuckle(user, to);
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _size, "size", 100);

            var seconds = 0.25f;
            serializer.DataField(ref seconds, "cooldown", 0.25f);

            _unbuckleDelay = TimeSpan.FromSeconds(seconds);
        }

        protected override void Startup()
        {
            base.Startup();
            UpdateBuckleStatus();
        }

        public override void OnRemove()
        {
            base.OnRemove();

            BuckledTo?.Remove(this);
            TryUnbuckle(Owner, true);

            _buckleTime = default;
            UpdateBuckleStatus();
        }

        public override ComponentState GetComponentState()
        {
            int? drawDepth = null;

            if (BuckledTo != null &&
                Owner.Transform.WorldRotation.GetCardinalDir() == Direction.North &&
                BuckledTo.SpriteComponent != null)
            {
                drawDepth = BuckledTo.SpriteComponent.DrawDepth - 1;
            }


            return new BuckleComponentState(Buckled, drawDepth, LastEntityBuckledTo, DontCollide);
        }

        bool IInteractHand.InteractHand(InteractHandEventArgs eventArgs)
        {
            return TryUnbuckle(eventArgs.User);
        }


        public void Update()
        {
            if (!DontCollide || Body == null)
                return;

            Body.WakeBody();

            if (!IsOnStrapEntityThisFrame && DontCollide)
            {
                DontCollide = false;
                TryUnbuckle(Owner);
                Dirty();
            }

            IsOnStrapEntityThisFrame = false;
        }

        /// <summary>
        ///     Allows the unbuckling of the owning entity through a verb if
        ///     anyone right clicks them.
        /// </summary>
        [Verb]
        private sealed class BuckleVerb : Verb<BuckleComponent>
        {
            protected override void GetData(IEntity user, BuckleComponent component, VerbData data)
            {
                if (!ActionBlockerSystem.CanInteract(user) || !component.Buckled)
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.Text = Loc.GetString("Unbuckle");
            }

            protected override void Activate(IEntity user, BuckleComponent component)
            {
                component.TryUnbuckle(user);
            }
        }
    }
}
