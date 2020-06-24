﻿using System;
using System.Linq;
using Content.Server.GameObjects.Components.Interactable;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.Sound;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Interactable;
using Content.Shared.GameObjects.Components.Storage;
using Content.Shared.Interfaces;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(IStorageComponent))]
    public class EntityStorageComponent : Component, IActivate, IStorageComponent, IInteractUsing, IDestroyAct
    {
        public override string Name => "EntityStorage";

        private const float MaxSize = 1.0f; // maximum width or height of an entity allowed inside the storage.

        private static readonly TimeSpan InternalOpenAttemptDelay = TimeSpan.FromSeconds(0.5);
        private TimeSpan _lastInternalOpenAttempt;

        [ViewVariables]
        private int StorageCapacityMax;
        [ViewVariables]
        private bool IsCollidableWhenOpen;
        [ViewVariables]
        private Container Contents;
        [ViewVariables]
        private IEntityQuery entityQuery;
        private bool _showContents;
        private bool _open;
        private bool _isWeldedShut;

        /// <summary>
        /// Determines if the container contents should be drawn when the container is closed.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool ShowContents
        {
            get => _showContents;
            set
            {
                _showContents = value;
                Contents.ShowContents = _showContents;
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Open
        {
            get => _open;
            private set => _open = value;
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public bool IsWeldedShut
        {
            get => _isWeldedShut;
            set
            {
                _isWeldedShut = value;

                if (Owner.TryGetComponent(out AppearanceComponent appearance))
                {
                    appearance.SetData(StorageVisuals.Welded, value);
                }
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public bool CanWeldShut { get; set; }

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();
            Contents = ContainerManagerComponent.Ensure<Container>(nameof(EntityStorageComponent), Owner);
            entityQuery = new IntersectingEntityQuery(Owner);

            Contents.ShowContents = _showContents;

            if (Owner.TryGetComponent<PlaceableSurfaceComponent>(out var placeableSurfaceComponent))
            {
                placeableSurfaceComponent.IsPlaceable = Open;
            }
        }

        /// <inheritdoc />
        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref StorageCapacityMax, "Capacity", 30);
            serializer.DataField(ref IsCollidableWhenOpen, "IsCollidableWhenOpen", false);
            serializer.DataField(ref _showContents, "showContents", false);
            serializer.DataField(ref _open, "open", false);
            serializer.DataField(this, a => a.IsWeldedShut, "IsWeldedShut", false);
            serializer.DataField(this, a => a.CanWeldShut, "CanWeldShut", true);
        }

        public virtual void Activate(ActivateEventArgs eventArgs)
        {
            ToggleOpen(eventArgs.User);
        }

        protected virtual void ToggleOpen(IEntity user)
        {
            if (IsWeldedShut)
            {
                Owner.PopupMessage(user, Loc.GetString("It's welded completely shut!"));
                return;
            }

            if (Open)
            {
                CloseStorage();
            }
            else
            {
                TryOpenStorage(user);
            }
        }

        private void CloseStorage()
        {
            Open = false;
            var entities = Owner.EntityManager.GetEntities(entityQuery);
            var count = 0;
            foreach (var entity in entities)
            {
                // prevents taking items out of inventories, out of containers, and orphaning child entities
                if(!entity.Transform.IsMapTransform)
                    continue;

                // only items that can be stored in an inventory, or a mob, can be eaten by a locker
                if (!entity.HasComponent<StoreableComponent>() && !entity.HasComponent<SpeciesComponent>())
                    continue;

                if (!AddToContents(entity))
                {
                    continue;
                }
                count++;
                if (count >= StorageCapacityMax)
                {
                    break;
                }
            }

            ModifyComponents();
            EntitySystem.Get<AudioSystem>().PlayFromEntity("/Audio/machines/closetclose.ogg", Owner);
            _lastInternalOpenAttempt = default;
        }

        private void OpenStorage()
        {
            Open = true;
            EmptyContents();
            ModifyComponents();
            EntitySystem.Get<AudioSystem>().PlayFromEntity("/Audio/machines/closetopen.ogg", Owner);

        }

        private void ModifyComponents()
        {
            if (Owner.TryGetComponent<ICollidableComponent>(out var collidableComponent))
            {
                collidableComponent.CanCollide = IsCollidableWhenOpen || !Open;
            }

            if (Owner.TryGetComponent<PlaceableSurfaceComponent>(out var placeableSurfaceComponent))
            {
                placeableSurfaceComponent.IsPlaceable = Open;
            }

            if (Owner.TryGetComponent(out AppearanceComponent appearance))
            {
                appearance.SetData(StorageVisuals.Open, Open);
            }
        }

        private bool AddToContents(IEntity entity)
        {
            var collidableComponent = Owner.GetComponent<ICollidableComponent>();
            ICollidableComponent entityCollidableComponent;
            if (entity.TryGetComponent(out entityCollidableComponent))
            {
                if(MaxSize < entityCollidableComponent.WorldAABB.Size.X
                    || MaxSize < entityCollidableComponent.WorldAABB.Size.Y)
                {
                    return false;
                }

                if (collidableComponent.WorldAABB.Left > entityCollidableComponent.WorldAABB.Left)
                {
                    entity.Transform.WorldPosition += new Vector2(collidableComponent.WorldAABB.Left - entityCollidableComponent.WorldAABB.Left, 0);
                }
                else if (collidableComponent.WorldAABB.Right < entityCollidableComponent.WorldAABB.Right)
                {
                    entity.Transform.WorldPosition += new Vector2(collidableComponent.WorldAABB.Right - entityCollidableComponent.WorldAABB.Right, 0);
                }
                if (collidableComponent.WorldAABB.Bottom > entityCollidableComponent.WorldAABB.Bottom)
                {
                    entity.Transform.WorldPosition += new Vector2(0, collidableComponent.WorldAABB.Bottom - entityCollidableComponent.WorldAABB.Bottom);
                }
                else if (collidableComponent.WorldAABB.Top < entityCollidableComponent.WorldAABB.Top)
                {
                    entity.Transform.WorldPosition += new Vector2(0, collidableComponent.WorldAABB.Top - entityCollidableComponent.WorldAABB.Top);
                }
            }
            if (Contents.CanInsert(entity))
            {
                // Because Insert sets the local position to (0,0), and we want to keep the contents spread out,
                // we re-apply the world position after inserting.
                Vector2 worldPos;
                if (entity.HasComponent<IActorComponent>())
                {
                    worldPos = Owner.Transform.WorldPosition;
                }
                else
                {
                    worldPos = entity.Transform.WorldPosition;
                }
                Contents.Insert(entity);
                entity.Transform.WorldPosition = worldPos;
                if (entityCollidableComponent != null)
                {
                    entityCollidableComponent.CanCollide = false;
                }
                return true;
            }
            return false;
        }

        private void EmptyContents()
        {
            foreach (var contained in Contents.ContainedEntities.ToArray())
            {
                if(Contents.Remove(contained))
                {
                    if (contained.TryGetComponent<ICollidableComponent>(out var entityCollidableComponent))
                    {
                        entityCollidableComponent.CanCollide = true;
                    }
                }
            }
        }

        /// <inheritdoc />
        public override void HandleMessage(ComponentMessage message, IComponent component)
        {
            base.HandleMessage(message, component);

            switch (message)
            {
                case RelayMovementEntityMessage msg:
                    if (msg.Entity.HasComponent<HandsComponent>())
                    {
                        var timing = IoCManager.Resolve<IGameTiming>();
                        if (timing.CurTime <
                            _lastInternalOpenAttempt + InternalOpenAttemptDelay)
                        {
                            break;
                        }

                        _lastInternalOpenAttempt = timing.CurTime;
                        TryOpenStorage(msg.Entity);
                    }
                    break;
            }
        }

        protected virtual void TryOpenStorage(IEntity user)
        {
            if (IsWeldedShut)
            {
                Owner.PopupMessage(user, Loc.GetString("It's welded completely shut!"));
                return;
            }
            OpenStorage();
        }

        /// <inheritdoc />
        public bool Remove(IEntity entity)
        {
            return Contents.CanRemove(entity);
        }

        /// <inheritdoc />
        public bool Insert(IEntity entity)
        {
            // Trying to add while open just dumps it on the ground below us.
            if (Open)
            {
                entity.Transform.WorldPosition = Owner.Transform.WorldPosition;
                return true;
            }

            return Contents.Insert(entity);
        }

        /// <inheritdoc />
        public bool CanInsert(IEntity entity)
        {
            if (Open)
            {
                return true;
            }

            if (Contents.ContainedEntities.Count >= StorageCapacityMax)
            {
                return false;
            }

            return Contents.CanInsert(entity);
        }

        [Verb]
        private sealed class OpenToggleVerb : Verb<EntityStorageComponent>
        {
            protected override void GetData(IEntity user, EntityStorageComponent component, VerbData data)
            {
                if (!ActionBlockerSystem.CanInteract(user))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                component.OpenVerbGetData(user, component, data);
            }

            /// <inheritdoc />
            protected override void Activate(IEntity user, EntityStorageComponent component)
            {
                component.ToggleOpen(user);
            }
        }

        protected virtual void OpenVerbGetData(IEntity user, EntityStorageComponent component, VerbData data)
        {
            if (!ActionBlockerSystem.CanInteract(user))
            {
                data.Visibility = VerbVisibility.Invisible;
                return;
            }

            if (IsWeldedShut)
            {
                data.Visibility = VerbVisibility.Disabled;
                var verb = Loc.GetString(component.Open ? "Close" : "Open");
                data.Text = Loc.GetString("{0} (welded shut)", verb);
                return;
            }

            data.Text = component.Open ? "Close" : "Open";
        }

        public bool InteractUsing(InteractUsingEventArgs eventArgs)
        {

            if (Open)
                return false;

            if (!CanWeldShut)
                return false;

            if (!eventArgs.Using.TryGetComponent(out WelderComponent tool))
                return false;

            if (!tool.UseTool(eventArgs.User, Owner, ToolQuality.Welding, 1f))
                return false;

            IsWeldedShut ^= true;
            return true;
        }

        public void OnDestroy(DestructionEventArgs eventArgs)
        {
            EmptyContents();
        }
    }
}
