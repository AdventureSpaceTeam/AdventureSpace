﻿using System;
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Access;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Utility;
using Content.Shared.GameObjects.Components.Doors;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Timers;
using Robust.Shared.ViewVariables;
using CancellationTokenSource = System.Threading.CancellationTokenSource;

namespace Content.Server.GameObjects
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class ServerDoorComponent : Component, IActivate, ICollideBehavior
    {
        public override string Name => "Door";

        private DoorState _state = DoorState.Closed;

        protected virtual DoorState State
        {
            get => _state;
            set => _state = value;
        }

        private float OpenTimeCounter;

        private CollidableComponent collidableComponent;
        private AppearanceComponent _appearance;
        private CancellationTokenSource _cancellationTokenSource;

        private static readonly TimeSpan CloseTime = TimeSpan.FromSeconds(1.2f);
        private static readonly TimeSpan OpenTimeOne = TimeSpan.FromSeconds(0.3f);
        private static readonly TimeSpan OpenTimeTwo = TimeSpan.FromSeconds(0.9f);
        private static readonly TimeSpan DenyTime = TimeSpan.FromSeconds(0.45f);

        [ViewVariables]
        private bool _occludes;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _occludes, "occludes", true);
        }

        public override void Initialize()
        {
            base.Initialize();

            collidableComponent = Owner.GetComponent<CollidableComponent>();
            _appearance = Owner.GetComponent<AppearanceComponent>();
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public override void OnRemove()
        {
            _cancellationTokenSource.Cancel();
            collidableComponent = null;
            _appearance = null;

            base.OnRemove();
        }

        protected virtual void ActivateImpl(ActivateEventArgs eventArgs)
        {
            if (State == DoorState.Open)
            {
                TryClose(eventArgs.User);
            }
            else if (State == DoorState.Closed)
            {
                TryOpen(eventArgs.User);
            }
        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            ActivateImpl(eventArgs);
        }


        void ICollideBehavior.CollideWith(IEntity entity)
        {
            if (State != DoorState.Closed)
            {
                return;
            }
            if (entity.HasComponent(typeof(SpeciesComponent)))
            {
                    TryOpen(entity);
            }
        }

        private void SetAppearance(DoorVisualState state)
        {
            if (_appearance != null || Owner.TryGetComponent(out _appearance))
                _appearance.SetData(DoorVisuals.VisualState, state);
        }

        public virtual bool CanOpen()
        {
            return true;
        }

        public bool CanOpen(IEntity user)
        {
            if (!CanOpen()) return false;
            if (!Owner.TryGetComponent(out AccessReader accessReader))
            {
                return true;
            }
            return accessReader.IsAllowed(user);
        }

        public void TryOpen(IEntity user)
        {
            if (!CanOpen(user))
            {
                Deny();
                return;
            }
            Open();
        }

        public void Open()
        {
            if (State != DoorState.Closed)
            {
                return;
            }

            State = DoorState.Opening;
            SetAppearance(DoorVisualState.Opening);
            if (_occludes && Owner.TryGetComponent(out OccluderComponent occluder))
            {
                occluder.Enabled = false;
            }

            Timer.Spawn(OpenTimeOne, async () =>
            {
                collidableComponent.CanCollide = false;

                await Timer.Delay(OpenTimeTwo, _cancellationTokenSource.Token);

                State = DoorState.Open;
                SetAppearance(DoorVisualState.Open);
            }, _cancellationTokenSource.Token);
            
            Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, new AccessReaderChangeMessage(Owner.Uid, false));
        }

        public virtual bool CanClose()
        {
            return true;
        }

        public bool CanClose(IEntity user)
        {
            if (!CanClose()) return false;
            if (!Owner.TryGetComponent(out AccessReader accessReader))
            {
                return true;
            }
            return accessReader.IsAllowed(user);
        }

        public void TryClose(IEntity user)
        {
            if (!CanClose(user))
            {
                Deny();
                return;
            }
            Close();
        }

        public bool Close()
        {
            if (collidableComponent.IsColliding(Vector2.Zero))
            {
                // Do nothing, somebody's in the door.
                return false;
            }

            State = DoorState.Closing;
            collidableComponent.CanCollide = true;
            OpenTimeCounter = 0;
            SetAppearance(DoorVisualState.Closing);

            Timer.Spawn(CloseTime, () =>
            {
                State = DoorState.Closed;
                SetAppearance(DoorVisualState.Closed);
                if (_occludes && Owner.TryGetComponent(out OccluderComponent occluder))
                {
                    occluder.Enabled = true;
                }
            }, _cancellationTokenSource.Token);
            Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, new AccessReaderChangeMessage(Owner.Uid, true));
            return true;
        }

        public virtual void Deny()
        {
            SetAppearance(DoorVisualState.Deny);
            Timer.Spawn(DenyTime, () =>
            {
                SetAppearance(DoorVisualState.Closed);
            }, _cancellationTokenSource.Token);
        }

        private const float AUTO_CLOSE_DELAY = 5;
        public virtual void OnUpdate(float frameTime)
        {
            if (State != DoorState.Open)
            {
                return;
            }

            OpenTimeCounter += frameTime;
            if (OpenTimeCounter > AUTO_CLOSE_DELAY)
            {
                if (!CanClose() || !Close())
                {
                    // Try again in 2 seconds if it's jammed or something.
                    OpenTimeCounter -= 2;
                }
            }
        }

        protected enum DoorState
        {
            Closed,
            Open,
            Closing,
            Opening,
        }
    }
}
