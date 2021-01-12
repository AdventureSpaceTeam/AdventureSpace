﻿using System;
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Mobs;
using Content.Shared.Alert;
using Content.Shared.Damage;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.Components.Mobs.State;
using Content.Shared.GameObjects.Components.Movement;
using Content.Shared.GameObjects.Components.Nutrition;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Nutrition
{
    [RegisterComponent]
    public sealed class HungerComponent : SharedHungerComponent
    {
        [Dependency] private readonly IRobustRandom _random = default!;

        // Base stuff
        [ViewVariables(VVAccess.ReadWrite)]
        public float BaseDecayRate
        {
            get => _baseDecayRate;
            set => _baseDecayRate = value;
        }
        private float _baseDecayRate;

        [ViewVariables(VVAccess.ReadWrite)]
        public float ActualDecayRate
        {
            get => _actualDecayRate;
            set => _actualDecayRate = value;
        }
        private float _actualDecayRate;

        // Hunger
        [ViewVariables(VVAccess.ReadOnly)]
        public override HungerThreshold CurrentHungerThreshold => _currentHungerThreshold;
        private HungerThreshold _currentHungerThreshold;

        private HungerThreshold _lastHungerThreshold;

        [ViewVariables(VVAccess.ReadWrite)]
        public float CurrentHunger
        {
            get => _currentHunger;
            set => _currentHunger = value;
        }
        private float _currentHunger;

        [ViewVariables(VVAccess.ReadOnly)]
        public Dictionary<HungerThreshold, float> HungerThresholds => _hungerThresholds;
        private readonly Dictionary<HungerThreshold, float> _hungerThresholds = new()
        {
            {HungerThreshold.Overfed, 600.0f},
            {HungerThreshold.Okay, 450.0f},
            {HungerThreshold.Peckish, 300.0f},
            {HungerThreshold.Starving, 150.0f},
            {HungerThreshold.Dead, 0.0f},
        };

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _baseDecayRate, "base_decay_rate", 0.1f);
        }


        public static readonly Dictionary<HungerThreshold, AlertType> HungerThresholdAlertTypes = new()
        {
            { HungerThreshold.Overfed, AlertType.Overfed },
            { HungerThreshold.Peckish, AlertType.Peckish },
            { HungerThreshold.Starving, AlertType.Starving },
        };

        public void HungerThresholdEffect(bool force = false)
        {
            if (_currentHungerThreshold != _lastHungerThreshold || force)
            {
                // Revert slow speed if required
                if (_lastHungerThreshold == HungerThreshold.Starving && _currentHungerThreshold != HungerThreshold.Dead &&
                    Owner.TryGetComponent(out MovementSpeedModifierComponent movementSlowdownComponent))
                {
                    movementSlowdownComponent.RefreshMovementSpeedModifiers();
                }

                // Update UI
                Owner.TryGetComponent(out ServerAlertsComponent alertsComponent);

                if (HungerThresholdAlertTypes.TryGetValue(_currentHungerThreshold, out var alertId))
                {
                    alertsComponent?.ShowAlert(alertId);
                }
                else
                {
                    alertsComponent?.ClearAlertCategory(AlertCategory.Hunger);
                }

                switch (_currentHungerThreshold)
                {
                    case HungerThreshold.Overfed:
                        _lastHungerThreshold = _currentHungerThreshold;
                        _actualDecayRate = _baseDecayRate * 1.2f;
                        return;

                    case HungerThreshold.Okay:
                        _lastHungerThreshold = _currentHungerThreshold;
                        _actualDecayRate = _baseDecayRate;
                        return;

                    case HungerThreshold.Peckish:
                        // Same as okay except with UI icon saying eat soon.
                        _lastHungerThreshold = _currentHungerThreshold;
                        _actualDecayRate = _baseDecayRate * 0.8f;
                        return;

                    case HungerThreshold.Starving:
                        // TODO: If something else bumps this could cause mega-speed.
                        // If some form of speed update system if multiple things are touching it use that.
                        if (Owner.TryGetComponent(out MovementSpeedModifierComponent movementSlowdownComponent1))
                        {
                            movementSlowdownComponent1.RefreshMovementSpeedModifiers();
                        }
                        _lastHungerThreshold = _currentHungerThreshold;
                        _actualDecayRate = _baseDecayRate * 0.6f;
                        return;

                    case HungerThreshold.Dead:
                        return;
                    default:
                        Logger.ErrorS("hunger", $"No hunger threshold found for {_currentHungerThreshold}");
                        throw new ArgumentOutOfRangeException($"No hunger threshold found for {_currentHungerThreshold}");
                }
            }
        }

        protected override void Startup()
        {
            base.Startup();
            // Similar functionality to SS13. Should also stagger people going to the chef.
            _currentHunger = _random.Next(
                (int)_hungerThresholds[HungerThreshold.Peckish] + 10,
                (int)_hungerThresholds[HungerThreshold.Okay] - 1);
            _currentHungerThreshold = GetHungerThreshold(_currentHunger);
            _lastHungerThreshold = HungerThreshold.Okay; // TODO: Potentially change this -> Used Okay because no effects.
            HungerThresholdEffect(true);
            Dirty();
        }

        public HungerThreshold GetHungerThreshold(float food)
        {
            HungerThreshold result = HungerThreshold.Dead;
            var value = HungerThresholds[HungerThreshold.Overfed];
            foreach (var threshold in _hungerThresholds)
            {
                if (threshold.Value <= value && threshold.Value >= food)
                {
                    result = threshold.Key;
                    value = threshold.Value;
                }
            }

            return result;
        }

        public void UpdateFood(float amount)
        {
            _currentHunger = Math.Min(_currentHunger + amount, HungerThresholds[HungerThreshold.Overfed]);
        }

        // TODO: If mob is moving increase rate of consumption?
        //  Should use a multiplier as something like a disease would overwrite decay rate.
        public void OnUpdate(float frametime)
        {
            _currentHunger -= frametime * ActualDecayRate;
            UpdateCurrentThreshold();

            if (_currentHungerThreshold != HungerThreshold.Dead)
                return;

            if (!Owner.TryGetComponent(out IDamageableComponent damageable))
                return;

            if (!Owner.TryGetComponent(out IMobStateComponent mobState))
                return;

            if (!mobState.IsDead())
            {
                damageable.ChangeDamage(DamageType.Blunt, 2, true);
            }
        }

        private void UpdateCurrentThreshold()
        {
            var calculatedHungerThreshold = GetHungerThreshold(_currentHunger);
            // _trySound(calculatedThreshold);
            if (calculatedHungerThreshold != _currentHungerThreshold)
            {
                _currentHungerThreshold = calculatedHungerThreshold;
                HungerThresholdEffect();
                Dirty();
            }
        }

        public void ResetFood()
        {
            _currentHunger = HungerThresholds[HungerThreshold.Okay];
            UpdateCurrentThreshold();
        }

        public override ComponentState GetComponentState()
        {
            return new HungerComponentState(_currentHungerThreshold);
        }
    }
}
