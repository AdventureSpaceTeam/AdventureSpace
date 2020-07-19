﻿#nullable enable
using System;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.Components.Strap;
using Content.Server.Interfaces;
using Content.Server.Mobs;
using Content.Server.Utility;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Buckle;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.Components.Strap;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.EntitySystemMessages;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
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
    public class BuckleComponent : SharedBuckleComponent, IInteractHand, IDragDrop
    {
#pragma warning disable 649
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IEntitySystemManager _entitySystem = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IServerNotifyManager _notifyManager = default!;
#pragma warning restore 649

        private int _size;

        /// <summary>
        ///     The range from which this entity can buckle to a <see cref="StrapComponent"/>.
        /// </summary>
        [ViewVariables]
        private float _range;

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
        ///     True if the entity was inserted or removed from a container
        ///     before updating, false otherwise.
        /// </summary>
        [ViewVariables]
        private bool ContainerChanged { get; set; }

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
        private void BuckleStatus()
        {
            if (Owner.TryGetComponent(out ServerStatusEffectsComponent status))
            {
                status.ChangeStatusEffectIcon(StatusEffect.Buckled,
                    Buckled
                        ? BuckledTo!.BuckledIcon
                        : "/Textures/Interface/StatusEffects/Buckle/unbuckled.png");
            }
        }

        /// <summary>
        ///     Reattaches this entity to the strap, modifying its position and rotation.
        /// </summary>
        /// <param name="strap">The strap to reattach to.</param>
        private void ReAttach(StrapComponent strap)
        {
            var ownTransform = Owner.Transform;
            var strapTransform = strap.Owner.Transform;

            ownTransform.GridPosition = strapTransform.GridPosition;
            ownTransform.AttachParent(strapTransform);

            switch (strap.Position)
            {
                case StrapPosition.None:
                    ownTransform.WorldRotation = strapTransform.WorldRotation;
                    break;
                case StrapPosition.Stand:
                    StandingStateHelper.Standing(Owner);
                    ownTransform.WorldRotation = strapTransform.WorldRotation;
                    break;
                case StrapPosition.Down:
                    StandingStateHelper.Down(Owner);
                    ownTransform.WorldRotation = Angle.South;
                    break;
            }

            if (strapTransform.WorldRotation.GetCardinalDir() == Direction.North)
            {
                ownTransform.WorldPosition += (0, 0.15f);
            }
        }

        /// <summary>
        ///     Tries to make an entity buckle the owner of this component to another.
        /// </summary>
        /// <param name="user">
        ///     The entity buckling the owner of this component, can be the owner itself.
        /// </param>
        /// <param name="to">The entity to buckle the owner of this component to.</param>
        /// <returns>
        ///     true if the owner was buckled, otherwise false even if the owner was
        ///     previously already buckled.
        /// </returns>
        public bool TryBuckle(IEntity user, IEntity to)
        {
            if (user == null || user == to)
            {
                return false;
            }

            if (!ActionBlockerSystem.CanInteract(user))
            {
                _notifyManager.PopupMessage(user, user,
                    Loc.GetString("You can't do that!"));
                return false;
            }

            if (!to.TryGetComponent(out StrapComponent strap))
            {
                _notifyManager.PopupMessage(Owner, user,
                    Loc.GetString(Owner == user
                        ? "You can't buckle yourself there!"
                        : "You can't buckle {0:them} there!", Owner));
                return false;
            }

            var ownerPosition = Owner.Transform.MapPosition;
            var strapPosition = strap.Owner.Transform.MapPosition;
            var interaction = EntitySystem.Get<SharedInteractionSystem>();
            bool Ignored(IEntity entity) => entity == Owner || entity == user || entity == strap.Owner;

            if (!interaction.InRangeUnobstructed(ownerPosition, strapPosition, _range, predicate: Ignored))
            {
                _notifyManager.PopupMessage(strap.Owner, user,
                    Loc.GetString("You can't reach there!"));

                return false;
            }

            // If in a container
            if (ContainerHelpers.TryGetContainer(Owner, out var ownerContainer))
            {
                // And not in the same container as the strap
                if (!ContainerHelpers.TryGetContainer(strap.Owner, out var strapContainer) ||
                    ownerContainer != strapContainer)
                {
                    _notifyManager.PopupMessage(strap.Owner, user,
                        Loc.GetString("You can't reach there!"));
                    return false;
                }
            }

            if (!user.HasComponent<HandsComponent>())
            {
                _notifyManager.PopupMessage(user, user,
                    Loc.GetString("You don't have hands!"));
                return false;
            }

            if (Buckled)
            {
                _notifyManager.PopupMessage(Owner, user,
                    Loc.GetString(Owner == user
                        ? "You are already buckled in!"
                        : "{0:They} are already buckled in!", Owner));
                return false;
            }

            var parent = to.Transform.Parent;
            while (parent != null)
            {
                if (parent == user.Transform)
                {
                    _notifyManager.PopupMessage(Owner, user,
                        Loc.GetString(Owner == user
                            ? "You can't buckle yourself there!"
                            : "You can't buckle {0:them} there!", Owner));
                    return false;
                }

                parent = parent.Parent;
            }

            if (!strap.HasSpace(this))
            {
                _notifyManager.PopupMessage(Owner, user,
                    Loc.GetString(Owner == user
                        ? "You can't fit there!"
                        : "{0:They} can't fit there!", Owner));
                return false;
            }

            _entitySystem.GetEntitySystem<AudioSystem>()
                .PlayFromEntity(strap.BuckleSound, Owner);

            if (!strap.TryAdd(this))
            {
                _notifyManager.PopupMessage(Owner, user,
                    Loc.GetString(Owner == user
                        ? "You can't buckle yourself there!"
                        : "You can't buckle {0:them} there!", Owner));
                return false;
            }

            if (Owner.TryGetComponent(out AppearanceComponent appearance))
            {
                appearance.SetData(BuckleVisuals.Buckled, true);
            }

            ReAttach(strap);

            BuckledTo = strap;
            BuckleStatus();

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

            if (!force)
            {
                if (_gameTiming.CurTime < _buckleTime + _unbuckleDelay)
                {
                    return false;
                }

                if (!ActionBlockerSystem.CanInteract(user))
                {
                    _notifyManager.PopupMessage(user, user,
                        Loc.GetString("You can't do that!"));
                    return false;
                }

                var strapPosition = Owner.Transform.MapPosition;

                if (!InteractionChecks.InRangeUnobstructed(user, strapPosition, _range))
                {
                    return false;
                }
            }

            if (BuckledTo.Owner.TryGetComponent(out StrapComponent strap))
            {
                strap.Remove(this);
                _entitySystem.GetEntitySystem<AudioSystem>()
                    .PlayFromEntity(strap.UnbuckleSound, Owner);
            }

            if (Owner.Transform.Parent == BuckledTo.Owner.Transform)
            {
                Owner.Transform.DetachParent();
                Owner.Transform.WorldRotation = BuckledTo.Owner.Transform.WorldRotation;
            }

            BuckledTo = null;

            if (Owner.TryGetComponent(out AppearanceComponent appearance))
            {
                appearance.SetData(BuckleVisuals.Buckled, false);
            }

            if (Owner.TryGetComponent(out StunnableComponent stunnable) && stunnable.KnockedDown)
            {
                StandingStateHelper.Down(Owner);
            }
            else
            {
                StandingStateHelper.Standing(Owner);
            }

            if (Owner.TryGetComponent(out SpeciesComponent species))
            {
                species.CurrentDamageState.EnterState(Owner);
            }

            BuckleStatus();

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

        /// <summary>
        ///     Called when the owner is inserted or removed from a container,
        ///     to synchronize the state of buckling.
        /// </summary>
        /// <param name="message">The message received</param>
        private void InsertIntoContainer(ContainerModifiedMessage message)
        {
            if (message.Entity != Owner)
            {
                return;
            }

            ContainerChanged = true;
        }

        /// <summary>
        ///     Synchronizes the state of buckling depending on whether the entity
        ///     was inserted or removed from a container, and whether or not
        ///     its current strap (if there is one) has also been put into or removed
        ///     from the same container as well.
        /// </summary>
        public void Update()
        {
            if (!ContainerChanged || BuckledTo == null)
            {
                return;
            }

            var contained = ContainerHelpers.TryGetContainer(Owner, out var ownContainer);
            var strapContained = ContainerHelpers.TryGetContainer(BuckledTo.Owner, out var strapContainer);

            if (contained != strapContained || ownContainer != strapContainer)
            {
                TryUnbuckle(Owner, true);
                return;
            }

            if (!contained && !strapContained)
            {
                ReAttach(BuckledTo);
            }

            ContainerChanged = false;
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _size, "size", 100);
            serializer.DataField(ref _range, "range", SharedInteractionSystem.InteractionRange / 1.4f);

            var seconds = 0.25f;
            serializer.DataField(ref seconds, "cooldown", 0.25f);

            _unbuckleDelay = TimeSpan.FromSeconds(seconds);
        }

        public override void Initialize()
        {
            base.Initialize();

            _entityManager.EventBus.SubscribeEvent<EntInsertedIntoContainerMessage>(EventSource.Local, this, InsertIntoContainer);
            _entityManager.EventBus.SubscribeEvent<EntRemovedFromContainerMessage>(EventSource.Local, this, InsertIntoContainer);
        }

        protected override void Startup()
        {
            base.Startup();
            BuckleStatus();
        }

        public override void OnRemove()
        {
            base.OnRemove();

            _entityManager.EventBus.UnsubscribeEvents(this);

            if (BuckledTo != null &&
                BuckledTo.Owner.TryGetComponent(out StrapComponent strap))
            {
                strap.Remove(this);
            }

            TryUnbuckle(Owner, true);

            _buckleTime = default;
            BuckleStatus();
        }

        public override ComponentState GetComponentState()
        {
            int? drawDepth = null;

            if (BuckledTo != null &&
                Owner.Transform.WorldRotation.GetCardinalDir() == Direction.North &&
                BuckledTo.Owner.TryGetComponent(out SpriteComponent strapSprite))
            {
                drawDepth = strapSprite.DrawDepth - 1;
            }

            return new BuckleComponentState(Buckled, drawDepth);
        }

        bool IInteractHand.InteractHand(InteractHandEventArgs eventArgs)
        {
            return TryUnbuckle(eventArgs.User);
        }

        bool IDragDrop.DragDrop(DragDropEventArgs eventArgs)
        {
            return TryBuckle(eventArgs.User, eventArgs.Target);
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
