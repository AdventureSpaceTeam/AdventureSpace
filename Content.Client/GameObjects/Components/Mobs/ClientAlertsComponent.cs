﻿using System.Collections.Generic;
using System.Linq;
using Content.Client.UserInterface;
using Content.Client.UserInterface.Controls;
using Content.Shared.Alert;
using Content.Shared.GameObjects.Components.Mobs;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.Interfaces.UserInterface;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.ViewVariables;

namespace Content.Client.GameObjects.Components.Mobs
{
    /// <inheritdoc/>
    [RegisterComponent]
    [ComponentReference(typeof(SharedAlertsComponent))]
    public sealed class ClientAlertsComponent : SharedAlertsComponent
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IResourceCache _resourceCache = default!;

        private AlertsUI _ui;
        private AlertOrderPrototype _alertOrder;

        [ViewVariables]
        private readonly Dictionary<AlertKey, AlertControl> _alertControls
            = new();

        /// <summary>
        /// Allows calculating if we need to act due to this component being controlled by the current mob
        /// </summary>
        [ViewVariables]
        private bool CurrentlyControlled => _playerManager.LocalPlayer != null && _playerManager.LocalPlayer.ControlledEntity == Owner;

        protected override void Shutdown()
        {
            base.Shutdown();
            PlayerDetached();
        }

        public override void HandleMessage(ComponentMessage message, IComponent component)
        {
            base.HandleMessage(message, component);
            switch (message)
            {
                case PlayerAttachedMsg _:
                    PlayerAttached();
                    break;
                case PlayerDetachedMsg _:
                    PlayerDetached();
                    break;
            }
        }

        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not AlertsComponentState)
            {
                return;
            }

            UpdateAlertsControls();
        }

        private void PlayerAttached()
        {
            if (!CurrentlyControlled || _ui != null)
            {
                return;
            }

            _alertOrder = IoCManager.Resolve<IPrototypeManager>().EnumeratePrototypes<AlertOrderPrototype>().FirstOrDefault();
            if (_alertOrder == null)
            {
                Logger.ErrorS("alert", "no alertOrder prototype found, alerts will be in random order");
            }

            _ui = new AlertsUI();
            IoCManager.Resolve<IUserInterfaceManager>().StateRoot.AddChild(_ui);

            UpdateAlertsControls();
        }

        private void PlayerDetached()
        {
            foreach (var alertControl in _alertControls.Values)
            {
                alertControl.OnPressed -= AlertControlOnPressed;
            }

            if (_ui != null)
            {
                IoCManager.Resolve<IUserInterfaceManager>().StateRoot.RemoveChild(_ui);
                _ui = null;
            }
            _alertControls.Clear();
        }

        /// <summary>
        /// Updates the displayed alerts based on current state of Alerts, performing
        /// a diff to ensure we only change what's changed (this avoids active tooltips disappearing any
        /// time state changes)
        /// </summary>
        private void UpdateAlertsControls()
        {
            if (!CurrentlyControlled || _ui == null)
            {
                return;
            }

            // remove any controls with keys no longer present
            var toRemove = new List<AlertKey>();
            foreach (var existingKey in _alertControls.Keys)
            {
                if (!IsShowingAlert(existingKey))
                {
                    toRemove.Add(existingKey);
                }
            }
            foreach (var alertKeyToRemove in toRemove)
            {
                _alertControls.Remove(alertKeyToRemove, out var control);
                if (control == null) return;
                _ui.Grid.Children.Remove(control);
            }

            // now we know that alertControls contains alerts that should still exist but
            // may need to updated,
            // also there may be some new alerts we need to show.
            // further, we need to ensure they are ordered w.r.t their configured order
            foreach (var (alertKey, alertState) in EnumerateAlertStates())
            {
                if (!alertKey.AlertType.HasValue)
                {
                    Logger.WarningS("alert", "found alertkey without alerttype," +
                                             " alert keys should never be stored without an alerttype set: {0}", alertKey);
                    continue;
                }
                var alertType = alertKey.AlertType.Value;
                if (!AlertManager.TryGet(alertType, out var newAlert))
                {
                    Logger.ErrorS("alert", "Unrecognized alertType {0}", alertType);
                    continue;
                }

                if (_alertControls.TryGetValue(newAlert.AlertKey, out var existingAlertControl) &&
                    existingAlertControl.Alert.AlertType == newAlert.AlertType)
                {
                    // key is the same, simply update the existing control severity / cooldown
                    existingAlertControl.SetSeverity(alertState.Severity);
                    existingAlertControl.Cooldown = alertState.Cooldown;
                }
                else
                {
                    if (existingAlertControl != null)
                    {
                        _ui.Grid.Children.Remove(existingAlertControl);
                    }

                    // this is a new alert + alert key or just a different alert with the same
                    // key, create the control and add it in the appropriate order
                    var newAlertControl = CreateAlertControl(newAlert, alertState);
                    if (_alertOrder != null)
                    {
                        var added = false;
                        foreach (var alertControl in _ui.Grid.Children)
                        {
                            if (_alertOrder.Compare(newAlert, ((AlertControl) alertControl).Alert) < 0)
                            {
                                var idx = alertControl.GetPositionInParent();
                                _ui.Grid.Children.Add(newAlertControl);
                                newAlertControl.SetPositionInParent(idx);
                                added = true;
                                break;
                            }
                        }

                        if (!added)
                        {
                            _ui.Grid.Children.Add(newAlertControl);
                        }
                    }
                    else
                    {
                        _ui.Grid.Children.Add(newAlertControl);
                    }

                    _alertControls[newAlert.AlertKey] = newAlertControl;
                }
            }
        }

        private AlertControl CreateAlertControl(AlertPrototype alert, AlertState alertState)
        {
            var alertControl = new AlertControl(alert, alertState.Severity, _resourceCache)
            {
                Cooldown = alertState.Cooldown
            };
            alertControl.OnPressed += AlertControlOnPressed;
            return alertControl;
        }

        private void AlertControlOnPressed(BaseButton.ButtonEventArgs args)
        {
            AlertPressed(args, args.Button as AlertControl);
        }

        private void AlertPressed(BaseButton.ButtonEventArgs args, AlertControl alert)
        {
            if (args.Event.Function != EngineKeyFunctions.UIClick)
            {
                return;
            }

            if (!alert.Alert.HasOnClick) return;
            SendNetworkMessage(new ClickAlertMessage(alert.Alert.AlertType));
        }

        protected override void AfterShowAlert()
        {
            UpdateAlertsControls();
        }

        protected override void AfterClearAlert()
        {
            UpdateAlertsControls();
        }
    }
}
