#nullable enable
using System;
using Content.Server.Battery.Components;
using Content.Server.Power.Components;
using Content.Shared.Interaction;
using Content.Shared.Notification;
using Content.Shared.Notification.Managers;
using Content.Shared.Radiation;
using Content.Shared.Singularity.Components;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;

namespace Content.Server.Singularity.Components
{
    [RegisterComponent]
    public class RadiationCollectorComponent : Component, IInteractHand, IRadiationAct
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        public override string Name => "RadiationCollector";
        private bool _enabled;
        private TimeSpan _coolDownEnd;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Collecting {
            get => _enabled;
            set
            {
                if (_enabled == value) return;
                _enabled = value;
                SetAppearance(_enabled ? RadiationCollectorVisualState.Activating : RadiationCollectorVisualState.Deactivating);
            }
        }

        [ComponentDependency] private readonly BatteryComponent? _batteryComponent = default!;
        [ComponentDependency] private readonly BatteryDischargerComponent? _batteryDischargerComponent = default!;

        bool IInteractHand.InteractHand(InteractHandEventArgs eventArgs)
        {
            var curTime = _gameTiming.CurTime;

            if(curTime < _coolDownEnd)
                return true;

            if (!_enabled)
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("radiation-collector-component-use-on"));
                Collecting = true;
            }
            else
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("radiation-collector-component-use-off"));
                Collecting = false;
            }

            _coolDownEnd = curTime + TimeSpan.FromSeconds(0.81f);

            return true;
        }

        void IRadiationAct.RadiationAct(float frameTime, SharedRadiationPulseComponent radiation)
        {
            if (!_enabled) return;

            // No idea if this is even vaguely accurate to the previous logic.
            // The maths is copied from that logic even though it works differently.
            // But the previous logic would also make the radiation collectors never ever stop providing energy.
            // And since frameTime was used there, I'm assuming that this is what the intent was.
            // This still won't stop things being potentially hilarously unbalanced though.
            if (_batteryComponent != null)
            {
                _batteryComponent!.CurrentCharge += frameTime * radiation.RadsPerSecond * 3000f;
                if (_batteryDischargerComponent != null)
                {
                    // The battery discharger is controlled like this to ensure it won't drain the entire battery in a single tick.
                    // If that occurs then the battery discharger ends up shutting down.
                    _batteryDischargerComponent!.ActiveSupplyRate = (int) Math.Max(1, _batteryComponent!.CurrentCharge);
                }
            }
        }

        protected void SetAppearance(RadiationCollectorVisualState state)
        {
            if (Owner.TryGetComponent<AppearanceComponent>(out var appearance))
            {
                appearance.SetData(RadiationCollectorVisuals.VisualState, state);
            }
        }
    }
}
