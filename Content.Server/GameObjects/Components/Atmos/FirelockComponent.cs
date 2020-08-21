﻿using System;
using System.Threading.Tasks;
using Content.Server.Atmos;
using Content.Server.GameObjects.Components.Doors;
using Content.Server.GameObjects.Components.Interactable;
using Content.Server.Interfaces;
using Content.Shared.GameObjects.Components.Doors;
using Content.Shared.GameObjects.Components.Interactable;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.GameObjects.Components.Atmos
{
    [RegisterComponent]
    public class FirelockComponent : ServerDoorComponent, IInteractUsing, ICollideBehavior
    {
        [Dependency] private IServerNotifyManager _notifyManager = default!;

        public override string Name => "Firelock";

        protected override TimeSpan CloseTimeOne => TimeSpan.FromSeconds(0.1f);
        protected override TimeSpan CloseTimeTwo => TimeSpan.FromSeconds(0.6f);
        protected override TimeSpan OpenTimeOne => TimeSpan.FromSeconds(0.1f);
        protected override TimeSpan OpenTimeTwo => TimeSpan.FromSeconds(0.6f);

        public void CollideWith(IEntity collidedWith)
        {
            // We do nothing.
        }

        protected override void Startup()
        {
            base.Startup();

            var airtightComponent = Owner.EnsureComponent<AirtightComponent>();
            var collidableComponent = Owner.GetComponent<ICollidableComponent>();

            Safety = false;
            airtightComponent.AirBlocked = false;
            collidableComponent.Hard = false;

            if (Occludes && Owner.TryGetComponent(out OccluderComponent occluder))
            {
                occluder.Enabled = false;
            }

            State = DoorState.Open;
            SetAppearance(DoorVisualState.Open);
        }

        public bool EmergencyPressureStop()
        {
            var closed = State == DoorState.Open && Close();

            if(closed)
                Owner.GetComponent<AirtightComponent>().AirBlocked = true;

            return closed;
        }

        public override bool CanOpen()
        {
            return !IsHoldingFire() && !IsHoldingPressure() && base.CanOpen();
        }

        public override bool CanClose(IEntity user) => true;
        public override bool CanOpen(IEntity user) => CanOpen();

        public async Task<bool> InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (!eventArgs.Using.TryGetComponent<ToolComponent>(out var tool))
                return false;

            if (tool.HasQuality(ToolQuality.Prying))
            {
                var holdingPressure = IsHoldingPressure();
                var holdingFire = IsHoldingFire();

                if (State == DoorState.Closed)
                {
                    if(holdingPressure)
                        _notifyManager.PopupMessage(Owner, eventArgs.User, "A gush of air blows in your face... Maybe you should reconsider.");
                }

                if (!await tool.UseTool(eventArgs.User, Owner, holdingPressure || holdingFire ? 1.5f : 0.25f, ToolQuality.Prying)) return false;

                if (State == DoorState.Closed)
                    Open();
                else if (State == DoorState.Open)
                    Close();

                return true;
            }

            return false;
        }
    }
}
