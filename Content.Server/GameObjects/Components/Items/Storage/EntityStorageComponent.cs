﻿using System.Linq;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.Sound;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Storage;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.Container;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(IStorageComponent))]
    public class EntityStorageComponent : Component, IActivate, IStorageComponent
    {
        public override string Name => "EntityStorage";

        private const float MaxSize = 1.0f; // maximum width or height of an entity allowed inside the storage.

        private int StorageCapacityMax;
        private bool IsCollidableWhenOpen;
        private Container Contents;
        private IEntityQuery entityQuery;

        public override void Initialize()
        {
            base.Initialize();
            Contents = ContainerManagerComponent.Ensure<Container>(nameof(EntityStorageComponent), Owner);
            entityQuery = new IntersectingEntityQuery(Owner);
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref StorageCapacityMax, "Capacity", 30);
            serializer.DataField(ref IsCollidableWhenOpen, "IsCollidableWhenOpen", false);
        }

        [ViewVariables]
        public bool Open { get; private set; }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            if (Open)
            {
                CloseStorage();
            }
            else
            {
                OpenStorage();
            }
        }

        private void CloseStorage()
        {
            Open = false;
            var entities = Owner.EntityManager.GetEntities(entityQuery);
            var count = 0;
            foreach (var entity in entities)
            {
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
            if (Owner.TryGetComponent(out SoundComponent soundComponent))
            {
                soundComponent.Play("/Audio/machines/closetclose.ogg");
            }
        }

        private void OpenStorage()
        {
            Open = true;
            EmptyContents();
            ModifyComponents();
            if (Owner.TryGetComponent(out SoundComponent soundComponent))
            {
                soundComponent.Play("/Audio/machines/closetopen.ogg");
            }
        }

        private void ModifyComponents()
        {
            if (Owner.TryGetComponent<ICollidableComponent>(out var collidableComponent))
            {
                collidableComponent.CollisionEnabled = IsCollidableWhenOpen || !Open;
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
            if(entity.TryGetComponent<ICollidableComponent>(out var entityCollidableComponent))
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
                Contents.Insert(entity);
                return true;
            }
            return false;
        }

        private void EmptyContents()
        {
            foreach (var contained in Contents.ContainedEntities.ToArray())
            {
                Contents.Remove(contained);
                contained.Transform.WorldPosition = Owner.Transform.WorldPosition;
            }
        }

        public override void HandleMessage(ComponentMessage message, INetChannel netChannel = null, IComponent component = null)
        {
            base.HandleMessage(message, netChannel, component);

            switch (message)
            {
                case RelayMovementEntityMessage msg:
                    if (msg.Entity.HasComponent<HandsComponent>())
                    {
                        OpenStorage();
                    }
                    break;
            }
        }

        public bool Remove(IEntity entity)
        {
            return Contents.CanRemove(entity);
        }

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
    }
}
