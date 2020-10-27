﻿#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Content.Server.Atmos.Reactions;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces;
using Content.Shared.Atmos;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.Atmos
{
    /// <summary>
    ///     A general-purpose, variable volume gas mixture.
    /// </summary>
    [Serializable]
    public class GasMixture : IExposeData, IEquatable<GasMixture>, ICloneable
    {
        private readonly AtmosphereSystem _atmosphereSystem;

        public static GasMixture SpaceGas => new GasMixture() {Volume = 2500f, Immutable = true, Temperature = Atmospherics.TCMB};

        [ViewVariables]
        private float[] _moles = new float[Atmospherics.TotalNumberOfGases];

        [ViewVariables]
        private float[] _molesArchived = new float[Atmospherics.TotalNumberOfGases];

        [ViewVariables]
        private float _temperature = Atmospherics.TCMB;

        public IReadOnlyList<float> Gases => _moles;

        [ViewVariables]
        public bool Immutable { get; private set; }

        [ViewVariables]
        public float LastShare { get; private set; } = 0;

        [ViewVariables]
        public readonly Dictionary<GasReaction, float> ReactionResults = new Dictionary<GasReaction, float>()
        {
            // We initialize the dictionary here.
            { GasReaction.Fire, 0f }
        };

        [ViewVariables]
        public float HeatCapacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var capacity = 0f;

                for (var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
                {
                    capacity += _atmosphereSystem.GetGas(i).SpecificHeat * _moles[i];
                }

                return MathF.Max(capacity, Atmospherics.MinimumHeatCapacity);
            }
        }

        /// <summary>
        /// Heat capacity ratio of gas mixture
        /// </summary>
        [ViewVariables]
        public float HeatCapacityRatio
        {
            get
            {
                var delimiterSum = 0f;
                for (var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
                {
                    delimiterSum += _moles[i] / (_atmosphereSystem.GetGas(i).HeatCapacityRatio - 1);
                }
                return 1 + TotalMoles / delimiterSum;
            }
        }

        public float MolarMass
        {
            get
            {
                var molarMass = 0f;
                var totalMoles = TotalMoles;
                for (var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
                {
                    molarMass += _atmosphereSystem.GetGas(i).MolarMass * (_moles[i] / totalMoles);
                }

                return molarMass;
            }
        }

        [ViewVariables]
        public float HeatCapacityArchived
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var capacity = 0f;

                for (var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
                {
                    capacity += _atmosphereSystem.GetGas(i).SpecificHeat * _molesArchived[i];
                }

                return MathF.Max(capacity, Atmospherics.MinimumHeatCapacity);
            }
        }

        [ViewVariables]
        public float TotalMoles
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var moles = 0f;

                foreach (var gas in _moles)
                {
                    moles += gas;
                }

                return moles;
            }
        }

        [ViewVariables]
        public float Pressure
        {
            get
            {
                if (Volume <= 0) return 0f;
                return TotalMoles * Atmospherics.R * Temperature / Volume;
            }
        }

        [ViewVariables]
        public float Temperature
        {
            get => _temperature;
            set
            {
                if (Immutable) return;
                _temperature = MathF.Max(value, Atmospherics.TCMB);
            }
        }

        [ViewVariables]
        public float ThermalEnergy => Temperature * HeatCapacity;

        [ViewVariables]
        public float TemperatureArchived { get; private set; }

        [ViewVariables]
        public float Volume { get; set; }

        public GasMixture() : this(null)
        {
        }

        public GasMixture(AtmosphereSystem? atmosphereSystem)
        {
            _atmosphereSystem = atmosphereSystem ?? EntitySystem.Get<AtmosphereSystem>();
            _moles = new float[_atmosphereSystem.Gases.Count()];
            _molesArchived = new float[_moles.Length];
        }

        public GasMixture(float volume, AtmosphereSystem? atmosphereSystem = null): this(atmosphereSystem)
        {
            if (volume < 0)
                volume = 0;
            Volume = volume;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MarkImmutable()
        {
            Immutable = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Archive()
        {
            _moles.AsSpan().CopyTo(_molesArchived.AsSpan());
            TemperatureArchived = Temperature;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Merge(GasMixture giver)
        {
            if (Immutable || giver == null) return;

            if (MathF.Abs(Temperature - giver.Temperature) > Atmospherics.MinimumTemperatureDeltaToConsider)
            {
                var combinedHeatCapacity = HeatCapacity + giver.HeatCapacity;
                if (combinedHeatCapacity > 0f)
                {
                    Temperature =
                        (giver.Temperature * giver.HeatCapacity + Temperature * HeatCapacity) / combinedHeatCapacity;
                }
            }

            for (var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
            {
                _moles[i] += giver._moles[i];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetMoles(int gasId)
        {
            return _moles[gasId];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetMoles(Gas gas)
        {
            return GetMoles((int)gas);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetMoles(int gasId, float quantity)
        {
            if (!Immutable)
                _moles[gasId] = quantity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetMoles(Gas gas, float quantity)
        {
            SetMoles((int)gas, quantity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AdjustMoles(int gasId, float quantity)
        {
            if (!Immutable)
                _moles[gasId] += quantity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AdjustMoles(Gas gas, float moles)
        {
            AdjustMoles((int)gas, moles);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GasMixture Remove(float amount)
        {
            return RemoveRatio(amount / TotalMoles);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GasMixture RemoveRatio(float ratio)
        {
            if(ratio <= 0)
                return new GasMixture(Volume, _atmosphereSystem);

            if (ratio > 1)
                ratio = 1;

            var removed = new GasMixture(_atmosphereSystem) {Volume = Volume, Temperature = Temperature};

            for (var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
            {
                var moles = _moles[i];
                if (moles < Atmospherics.GasMinMoles)
                    removed._moles[i] = 0f;
                else
                {
                    var removedMoles = moles * ratio;
                    removed._moles[i] = removedMoles;
                    if (!Immutable)
                        _moles[i] -= removedMoles;
                }
            }

            return removed;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyFromMutable(GasMixture sample)
        {
            if (Immutable) return;
            sample._moles.CopyTo(_moles, 0);
            Temperature = sample.Temperature;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Share(GasMixture sharer, int atmosAdjacentTurfs)
        {
            var temperatureDelta = TemperatureArchived - sharer.TemperatureArchived;
            var absTemperatureDelta = Math.Abs(temperatureDelta);
            var oldHeatCapacity = 0f;
            var oldSharerHeatCapacity = 0f;

            if (absTemperatureDelta > Atmospherics.MinimumTemperatureDeltaToConsider)
            {
                oldHeatCapacity = HeatCapacity;
                oldSharerHeatCapacity = sharer.HeatCapacity;
            }

            var heatCapacityToSharer = 0f;
            var heatCapacitySharerToThis = 0f;
            var movedMoles = 0f;
            var absMovedMoles = 0f;

            for(var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
            {
                var thisValue = _moles[i];
                var sharerValue = sharer._moles[i];
                var delta = (thisValue - sharerValue) / (atmosAdjacentTurfs + 1);
                if (!(MathF.Abs(delta) >= Atmospherics.GasMinMoles)) continue;
                if (absTemperatureDelta > Atmospherics.MinimumTemperatureDeltaToConsider)
                {
                    var gasHeatCapacity = delta * _atmosphereSystem.GetGas(i).SpecificHeat;
                    if (delta > 0)
                    {
                        heatCapacityToSharer += gasHeatCapacity;
                    }
                    else
                    {
                        heatCapacitySharerToThis -= gasHeatCapacity;
                    }
                }

                if (!Immutable) _moles[i] -= delta;
                if (!sharer.Immutable) sharer._moles[i] += delta;
                movedMoles += delta;
                absMovedMoles += MathF.Abs(delta);
            }

            LastShare = absMovedMoles;

            if (absTemperatureDelta > Atmospherics.MinimumTemperatureDeltaToConsider)
            {
                var newHeatCapacity = oldHeatCapacity + heatCapacitySharerToThis - heatCapacityToSharer;
                var newSharerHeatCapacity = oldSharerHeatCapacity + heatCapacityToSharer - heatCapacitySharerToThis;

                // Transfer of thermal energy (via changed heat capacity) between self and sharer.
                if (!Immutable && newHeatCapacity > Atmospherics.MinimumHeatCapacity)
                {
                    Temperature = ((oldHeatCapacity * Temperature) - (heatCapacityToSharer * TemperatureArchived) + (heatCapacitySharerToThis * sharer.TemperatureArchived)) / newHeatCapacity;
                }

                if (!sharer.Immutable && newSharerHeatCapacity > Atmospherics.MinimumHeatCapacity)
                {
                    sharer.Temperature = ((oldSharerHeatCapacity * sharer.Temperature) - (heatCapacitySharerToThis * sharer.TemperatureArchived) + (heatCapacityToSharer*TemperatureArchived)) / newSharerHeatCapacity;
                }

                // Thermal energy of the system (self and sharer) is unchanged.

                if (MathF.Abs(oldSharerHeatCapacity) > Atmospherics.MinimumHeatCapacity)
                {
                    if (MathF.Abs(newSharerHeatCapacity / oldSharerHeatCapacity - 1) < 0.1)
                    {
                        TemperatureShare(sharer, Atmospherics.OpenHeatTransferCoefficient);
                    }
                }
            }

            if (!(temperatureDelta > Atmospherics.MinimumTemperatureToMove) &&
                !(MathF.Abs(movedMoles) > Atmospherics.MinimumMolesDeltaToMove)) return 0f;
            var moles = TotalMoles;
            var theirMoles = sharer.TotalMoles;

            return (TemperatureArchived * (moles + movedMoles)) - (sharer.TemperatureArchived * (theirMoles - movedMoles)) * Atmospherics.R / Volume;

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float TemperatureShare(GasMixture sharer, float conductionCoefficient)
        {
            var temperatureDelta = TemperatureArchived - sharer.TemperatureArchived;
            if (MathF.Abs(temperatureDelta) > Atmospherics.MinimumTemperatureDeltaToConsider)
            {
                var heatCapacity = HeatCapacityArchived;
                var sharerHeatCapacity = sharer.HeatCapacityArchived;

                if (sharerHeatCapacity > Atmospherics.MinimumHeatCapacity && heatCapacity > Atmospherics.MinimumHeatCapacity)
                {
                    var heat = conductionCoefficient * temperatureDelta * (heatCapacity * sharerHeatCapacity / (heatCapacity + sharerHeatCapacity));

                    if (!Immutable)
                        Temperature = MathF.Abs(MathF.Max(Temperature - heat / heatCapacity, Atmospherics.TCMB));

                    if (!sharer.Immutable)
                        sharer.Temperature = MathF.Abs(MathF.Max(sharer.Temperature + heat / sharerHeatCapacity, Atmospherics.TCMB));
                }
            }

            return sharer.Temperature;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float TemperatureShare(float conductionCoefficient, float sharerTemperature, float sharerHeatCapacity)
        {
            var temperatureDelta = TemperatureArchived - sharerTemperature;
            if (MathF.Abs(temperatureDelta) > Atmospherics.MinimumTemperatureDeltaToConsider)
            {
                var heatCapacity = HeatCapacityArchived;

                if (sharerHeatCapacity > Atmospherics.MinimumHeatCapacity && heatCapacity > Atmospherics.MinimumHeatCapacity)
                {
                    var heat = conductionCoefficient * temperatureDelta * (heatCapacity * sharerHeatCapacity / (heatCapacity + sharerHeatCapacity));

                    if (!Immutable)
                        Temperature = MathF.Abs(MathF.Max(Temperature - heat / heatCapacity, Atmospherics.TCMB));

                    sharerTemperature = MathF.Abs(MathF.Max(sharerTemperature + heat / sharerHeatCapacity, Atmospherics.TCMB));
                }
            }

            return sharerTemperature;
        }

        public enum GasCompareResult
        {
            NoExchange = -2,
            TemperatureExchange = -1,
        }

        /// <summary>
        ///     Compares sample to self to see if within acceptable ranges that group processing may be enabled.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GasCompareResult Compare(GasMixture sample)
        {
            var moles = 0f;

            for(var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
            {
                var gasMoles = _moles[i];
                var delta = MathF.Abs(gasMoles - sample._moles[i]);
                if (delta > Atmospherics.MinimumMolesDeltaToMove && (delta > gasMoles * Atmospherics.MinimumAirRatioToMove))
                    return (GasCompareResult)i; // We can move gases!
                moles += gasMoles;
            }

            if (moles > Atmospherics.MinimumMolesDeltaToMove)
            {
                var tempDelta = MathF.Abs(Temperature - sample.Temperature);
                if (tempDelta > Atmospherics.MinimumTemperatureDeltaToSuspend)
                    return GasCompareResult.TemperatureExchange; // There can be temperature exchange.
            }

            // No exchange at all!
            return GasCompareResult.NoExchange;
        }

        /// <summary>
        ///     Pump gas from this mixture to the output mixture.
        ///     Amount depends on target pressure.
        /// </summary>
        /// <param name="outputAir">The mixture to pump the gas to</param>
        /// <param name="targetPressure">The target pressure to reach</param>
        /// <returns>Whether we could pump air to the output or not</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool PumpGasTo(GasMixture outputAir, float targetPressure)
        {
            var outputStartingPressure = outputAir.Pressure;
            var pressureDelta = targetPressure - outputStartingPressure;

            if (pressureDelta < 0.01)
                // No need to pump gas, we've reached the target.
                return false;

            if (!(TotalMoles > 0) || !(Temperature > 0)) return false;

            // We calculate the necessary moles to transfer with the ideal gas law.
            var transferMoles = pressureDelta * outputAir.Volume / (Temperature * Atmospherics.R);

            // And now we transfer the gas.
            var removed = Remove(transferMoles);
            outputAir.Merge(removed);
            return true;
        }

        /// <summary>
        ///     Releases gas from this mixture to the output mixture.
        ///     It can't transfer air to a mixture with higher pressure.
        /// </summary>
        /// <param name="outputAir"></param>
        /// <param name="targetPressure"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ReleaseGasTo(GasMixture outputAir, float targetPressure)
        {
            var outputStartingPressure = outputAir.Pressure;
            var inputStartingPressure = Pressure;

            if (outputStartingPressure >= MathF.Min(targetPressure, inputStartingPressure - 10))
                // No need to pump gas if the target is already reached or input pressure is too low.
                // Need at least 10 kPa difference to overcome friction in the mechanism.
                return false;

            if (!(TotalMoles > 0) || !(Temperature > 0)) return false;

            // We calculate the necessary moles to transfer with the ideal gas law.
            var pressureDelta = MathF.Min(targetPressure - outputStartingPressure, (inputStartingPressure - outputStartingPressure) / 2f);
            var transferMoles = pressureDelta * outputAir.Volume / (Temperature * Atmospherics.R);

            // And now we transfer the gas.
            var removed = Remove(transferMoles);
            outputAir.Merge(removed);

            return true;

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReactionResult React(IGasMixtureHolder holder)
        {
            var reaction = ReactionResult.NoReaction;
            var temperature = Temperature;
            var energy = ThermalEnergy;

            foreach (var prototype in _atmosphereSystem.GasReactions)
            {
                if (energy < prototype.MinimumEnergyRequirement ||
                    temperature < prototype.MinimumTemperatureRequirement)
                    continue;

                var doReaction = true;
                for (var i = 0; i < prototype.MinimumRequirements.Length; i++)
                {
                    if(i > Atmospherics.TotalNumberOfGases)
                        throw new IndexOutOfRangeException("Reaction Gas Minimum Requirements Array Prototype exceeds total number of gases!");

                    var req = prototype.MinimumRequirements[i];

                    if (!(GetMoles(i) < req)) continue;
                    doReaction = false;
                    break;
                }

                if (!doReaction)
                    continue;

                reaction = prototype.React(this, holder, _atmosphereSystem.GridTileLookupSystem);
                if(reaction.HasFlag(ReactionResult.StopReactions))
                    break;
            }

            return reaction;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            if (Immutable) return;
            Array.Clear(_moles, 0, Atmospherics.TotalNumberOfGases);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Multiply(float multiplier)
        {
            if (Immutable) return;
            for(var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
            {
                _moles[i] *= multiplier;
            }
        }

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => Immutable, "immutable", false);
            serializer.DataField(this, x => Volume, "volume", 0f);
            serializer.DataField(this, x => LastShare, "lastShare", 0f);
            serializer.DataField(this, x => TemperatureArchived, "temperatureArchived", 0f);
            serializer.DataField(ref _moles, "moles", new float[Atmospherics.TotalNumberOfGases]);
            serializer.DataField(ref _molesArchived, "molesArchived", new float[Atmospherics.TotalNumberOfGases]);
            serializer.DataField(ref _temperature, "temperature", Atmospherics.TCMB);

            // The arrays MUST have a specific length.
            Array.Resize(ref _moles, Atmospherics.TotalNumberOfGases);
            Array.Resize(ref _molesArchived, Atmospherics.TotalNumberOfGases);
        }

        public override bool Equals(object? obj)
        {
            if (obj is GasMixture mix)
                return Equals(mix);
            return false;
        }

        public bool Equals(GasMixture? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return _moles.SequenceEqual(other._moles)
                   && _molesArchived.SequenceEqual(other._molesArchived)
                   && _temperature.Equals(other._temperature)
                   && ReactionResults.SequenceEqual(other.ReactionResults)
                   && Immutable == other.Immutable
                   && LastShare.Equals(other.LastShare)
                   && TemperatureArchived.Equals(other.TemperatureArchived)
                   && Volume.Equals(other.Volume);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();

            for (var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
            {
                var moles = _moles[i];
                var molesArchived = _molesArchived[i];
                hashCode.Add(moles);
                hashCode.Add(molesArchived);
            }

            hashCode.Add(_temperature);
            hashCode.Add(TemperatureArchived);
            hashCode.Add(Immutable);
            hashCode.Add(LastShare);
            hashCode.Add(Volume);

            return hashCode.ToHashCode();
        }

        public object Clone()
        {
            var newMixture = new GasMixture(_atmosphereSystem)
            {
                _moles = (float[])_moles.Clone(),
                _molesArchived = (float[])_molesArchived.Clone(),
                _temperature = _temperature,
                Immutable = Immutable,
                LastShare = LastShare,
                TemperatureArchived = TemperatureArchived,
                Volume = Volume,
            };
            return newMixture;
        }
    }
}
