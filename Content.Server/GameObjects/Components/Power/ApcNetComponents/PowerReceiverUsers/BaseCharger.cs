﻿using System;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Content.Server.GameObjects.Components.Weapon.Ranged.Barrels;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Power;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.Container;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Power.Chargers
{
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(IInteractUsing))]
    public abstract class BaseCharger : Component, IActivate, IInteractUsing
    {
        [ViewVariables]
        private BatteryComponent _heldBattery;

        [ViewVariables]
        private ContainerSlot _container;

        [ViewVariables]
        private PowerReceiverComponent _powerReceiver;

        [ViewVariables]
        private CellChargerStatus _status;

        private AppearanceComponent _appearanceComponent;

        [ViewVariables]
        private int _chargeRate;

        [ViewVariables]
        private float _transferEfficiency;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _chargeRate, "chargeRate", 100);
            serializer.DataField(ref _transferEfficiency, "transferEfficiency", 0.85f);
        }

        public override void Initialize()
        {
            base.Initialize();
            _powerReceiver = Owner.GetComponent<PowerReceiverComponent>();
            _container = ContainerManagerComponent.Ensure<ContainerSlot>($"{Name}-powerCellContainer", Owner);
            _appearanceComponent = Owner.GetComponent<AppearanceComponent>();
            // Default state in the visualizer is OFF, so when this gets powered on during initialization it will generally show empty
            _powerReceiver.OnPowerStateChanged += PowerUpdate;
        }

        public override void OnRemove()
        {
            _powerReceiver.OnPowerStateChanged -= PowerUpdate;
            base.OnRemove();
        }

        bool IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            var result = TryInsertItem(eventArgs.Using);
            if (!result)
            {
                var localizationManager = IoCManager.Resolve<ILocalizationManager>();
                eventArgs.User.PopupMessage(Owner, localizationManager.GetString("Unable to insert capacitor"));
            }

            return result;
        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            RemoveItem(eventArgs.User);
        }

        /// <summary>
        /// This will remove the item directly into the user's hand / floor
        /// </summary>
        /// <param name="user"></param>
        private void RemoveItem(IEntity user)
        {
            var heldItem = _container.ContainedEntity;
            if (heldItem == null)
            {
                return;
            }

            _container.Remove(heldItem);
            _heldBattery = null;
            if (user.TryGetComponent(out HandsComponent handsComponent))
            {
                handsComponent.PutInHandOrDrop(heldItem.GetComponent<ItemComponent>());
            }

            if (heldItem.TryGetComponent(out ServerBatteryBarrelComponent batteryBarrelComponent))
            {
                batteryBarrelComponent.UpdateAppearance();
            }

            UpdateStatus();
        }

        private void PowerUpdate(object sender, PowerStateEventArgs eventArgs)
        {
            UpdateStatus();
        }

        [Verb]
        private sealed class InsertVerb : Verb<BaseCharger>
        {
            protected override void GetData(IEntity user, BaseCharger component, VerbData data)
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

            protected override void Activate(IEntity user, BaseCharger component)
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
        private sealed class EjectVerb : Verb<BaseCharger>
        {
            protected override void GetData(IEntity user, BaseCharger component, VerbData data)
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

            protected override void Activate(IEntity user, BaseCharger component)
            {
                component.RemoveItem(user);
            }
        }

        private CellChargerStatus GetStatus()
        {
            if (!_powerReceiver.Powered)
            {
                return CellChargerStatus.Off;
            }
            if (_container.ContainedEntity == null)
            {
                return CellChargerStatus.Empty;
            }
            if (_heldBattery != null && Math.Abs(_heldBattery.MaxCharge - _heldBattery.CurrentCharge) < 0.01)
            {
                return CellChargerStatus.Charged;
            }
            return CellChargerStatus.Charging;
        }

        private bool TryInsertItem(IEntity entity)
        {
            if (!IsEntityCompatible(entity) || _container.ContainedEntity != null)
            {
                return false;
            }
            if (!_container.Insert(entity))
            {
                return false;
            }
            _heldBattery = GetBatteryFrom(entity);
            UpdateStatus();
            return true;
        }

        /// <summary>
        ///     If the supplied entity should fit into the charger.
        /// </summary>
        protected abstract bool IsEntityCompatible(IEntity entity);

        protected abstract BatteryComponent GetBatteryFrom(IEntity entity);

        private void UpdateStatus()
        {
            // Not called UpdateAppearance just because it messes with the load
            var status = GetStatus();
            if (_status == status)
            {
                return;
            }
            _status = status;
            switch (_status)
            {
                // Update load just in case
                case CellChargerStatus.Off:
                    _powerReceiver.Load = 0;
                    _appearanceComponent?.SetData(CellVisual.Light, CellChargerStatus.Off);
                    break;
                case CellChargerStatus.Empty:
                    _powerReceiver.Load = 0;
                    _appearanceComponent?.SetData(CellVisual.Light, CellChargerStatus.Empty); ;
                    break;
                case CellChargerStatus.Charging:
                    _powerReceiver.Load = (int) (_chargeRate / _transferEfficiency);
                    _appearanceComponent?.SetData(CellVisual.Light, CellChargerStatus.Charging);
                    break;
                case CellChargerStatus.Charged:
                    _powerReceiver.Load = 0;
                    _appearanceComponent?.SetData(CellVisual.Light, CellChargerStatus.Charged);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            _appearanceComponent?.SetData(CellVisual.Occupied, _container.ContainedEntity != null);
        }

        public void OnUpdate(float frameTime) //todo: make single system for this
        {
            if (_status == CellChargerStatus.Empty || _status == CellChargerStatus.Charged || _container.ContainedEntity == null)
            {
                return;
            }
            TransferPower(frameTime);
        }

        private void TransferPower(float frameTime)
        {
            if (!_powerReceiver.Powered)
            {
                return;
            }
            _heldBattery.CurrentCharge += _chargeRate * frameTime;
            // Just so the sprite won't be set to 99.99999% visibility
            if (_heldBattery.MaxCharge - _heldBattery.CurrentCharge < 0.01)
            {
                _heldBattery.CurrentCharge = _heldBattery.MaxCharge;
            }
            UpdateStatus();
        }
    }
}
