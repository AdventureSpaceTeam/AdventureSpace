using System;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Utility;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Power;
using Content.Shared.Interfaces;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.Container;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.GameObjects.Components.Power.Chargers
{
    /// <summary>
    /// This is used for the standalone cell rechargers (e.g. from a flashlight)
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(IInteractUsing))]
    public sealed class PowerCellChargerComponent : BaseCharger, IActivate, IInteractUsing
    {
        public override string Name => "PowerCellCharger";
        public override double CellChargePercent => _container.ContainedEntity != null ?
            _container.ContainedEntity.GetComponent<PowerCellComponent>().Charge /
            _container.ContainedEntity.GetComponent<PowerCellComponent>().Capacity * 100 : 0.0f;

        public override void Initialize()
        {
            base.Initialize();
            _powerDevice = Owner.GetComponent<PowerDeviceComponent>();
            _container =
                ContainerManagerComponent.Ensure<ContainerSlot>($"{Name}-powerCellContainer", Owner);
            _appearanceComponent = Owner.GetComponent<AppearanceComponent>();
            // Default state in the visualizer is OFF, so when this gets powered on during initialization it will generally show empty
            _powerDevice.OnPowerStateChanged += PowerUpdate;
        }

        bool IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            var result = TryInsertItem(eventArgs.Using);
            if (result)
            {
                return true;
            }

            var localizationManager = IoCManager.Resolve<ILocalizationManager>();
            eventArgs.User.PopupMessage(Owner, localizationManager.GetString("Unable to insert capacitor"));

            return false;
        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            RemoveItem(eventArgs.User);
        }

        [Verb]
        private sealed class InsertVerb : Verb<PowerCellChargerComponent>
        {
            protected override void GetData(IEntity user, PowerCellChargerComponent component, VerbData data)
            {
                if (!ActionBlockerSystem.CanInteract(user))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                if (!user.TryGetComponent(out HandsComponent handsComponent))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                if (component._container.ContainedEntity != null || handsComponent.GetActiveHand == null)
                {
                    data.Visibility = VerbVisibility.Disabled;
                    data.Text = "Insert";
                    return;
                }

                data.Text = $"Insert {handsComponent.GetActiveHand.Owner.Name}";
            }

            protected override void Activate(IEntity user, PowerCellChargerComponent component)
            {
                if (!user.TryGetComponent(out HandsComponent handsComponent))
                {
                    return;
                }

                if (handsComponent.GetActiveHand == null)
                {
                    return;
                }
                var userItem = handsComponent.GetActiveHand.Owner;
                handsComponent.Drop(userItem);
                component.TryInsertItem(userItem);
            }
        }

        [Verb]
        private sealed class EjectVerb : Verb<PowerCellChargerComponent>
        {
            protected override void GetData(IEntity user, PowerCellChargerComponent component, VerbData data)
            {
                if (!ActionBlockerSystem.CanInteract(user))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                if (component._container.ContainedEntity == null)
                {
                    data.Text = "Eject";
                    data.Visibility = VerbVisibility.Disabled;
                    return;
                }

                data.Text = $"Eject {component._container.ContainedEntity.Name}";
            }

            protected override void Activate(IEntity user, PowerCellChargerComponent component)
            {
                component.RemoveItem(user);
            }
        }

        public bool TryInsertItem(IEntity entity)
        {
            if (!entity.HasComponent<PowerCellComponent>() ||
                _container.ContainedEntity != null)
            {
                return false;
            }
            
            if (!_container.Insert(entity))
            {
                return false;
            }
            UpdateStatus();
            return true;
        }

        protected override CellChargerStatus GetStatus()
        {
            if (!_powerDevice.Powered)
            {
                return CellChargerStatus.Off;
            }

            if (_container.ContainedEntity == null)
            {
                return CellChargerStatus.Empty;
            }

            if (_container.ContainedEntity.TryGetComponent(out PowerCellComponent component) &&
                Math.Abs(component.Capacity - component.Charge) < 0.01)
            {
                return CellChargerStatus.Charged;
            }

            return CellChargerStatus.Charging;
        }

        protected override void TransferPower(float frameTime)
        {
            // Two numbers: One for how much power actually goes into the device (chargeAmount) and
            // chargeLoss which is how much is drawn from the powernet
            var cellComponent = _container.ContainedEntity.GetComponent<PowerCellComponent>();
            var chargeLoss = cellComponent.RequestCharge(frameTime) * _transferRatio;
            _powerDevice.Load = chargeLoss;

            if (!_powerDevice.Powered)
            {
                // No power: Event should update to Off status
                return;
            }

            var chargeAmount = chargeLoss * _transferEfficiency;

            cellComponent.AddCharge(chargeAmount);
            // Just so the sprite won't be set to 99.99999% visibility
            if (cellComponent.Capacity - cellComponent.Charge < 0.01)
            {
                cellComponent.Charge = cellComponent.Capacity;
            }
            UpdateStatus();
        }
    }
}
