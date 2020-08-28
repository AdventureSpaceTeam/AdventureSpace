﻿using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Atmos;
using Content.Server.GameObjects.Components.Atmos;
using Content.Server.GameObjects.Components.Body.Circulatory;
using Content.Shared.Atmos;
using Content.Shared.Chemistry;
using Content.Shared.Damage;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.Interfaces.Chemistry;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Metabolism
{
    [RegisterComponent]
    public class MetabolismComponent : Component
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public override string Name => "Metabolism";

        private float _accumulatedFrameTime;

        [ViewVariables(VVAccess.ReadWrite)] private int _suffocationDamage;

        [ViewVariables] public Dictionary<Gas, float> NeedsGases { get; set; }

        [ViewVariables] public Dictionary<Gas, float> ProducesGases { get; set; }

        [ViewVariables] public Dictionary<Gas, float> DeficitGases { get; set; }

        [ViewVariables] public bool Suffocating => SuffocatingPercentage() > 0;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, b => b.NeedsGases, "needsGases", new Dictionary<Gas, float>());
            serializer.DataField(this, b => b.ProducesGases, "producesGases", new Dictionary<Gas, float>());
            serializer.DataField(this, b => b.DeficitGases, "deficitGases", new Dictionary<Gas, float>());
            serializer.DataField(ref _suffocationDamage, "suffocationDamage", 1);
        }

        private Dictionary<Gas, float> NeedsAndDeficit(float frameTime)
        {
            var needs = new Dictionary<Gas, float>(NeedsGases);
            foreach (var (gas, amount) in DeficitGases)
            {
                var newAmount = (needs.GetValueOrDefault(gas) + amount) * frameTime;
                needs[gas] = newAmount;
            }

            return needs;
        }

        private void ClampDeficit()
        {
            var deficitGases = new Dictionary<Gas, float>(DeficitGases);

            foreach (var (gas, deficit) in deficitGases)
            {
                if (!NeedsGases.TryGetValue(gas, out var need))
                {
                    DeficitGases.Remove(gas);
                    continue;
                }

                if (deficit > need)
                {
                    DeficitGases[gas] = need;
                }
            }
        }

        private float SuffocatingPercentage()
        {
            var percentages = new float[Atmospherics.TotalNumberOfGases];

            foreach (var (gas, deficit) in DeficitGases)
            {
                if (!NeedsGases.TryGetValue(gas, out var needed))
                {
                    percentages[(int) gas] = 1;
                    continue;
                }

                percentages[(int) gas] = deficit / needed;
            }

            return percentages.Average();
        }

        private float GasProducedMultiplier(Gas gas, float usedAverage)
        {
            if (!NeedsGases.TryGetValue(gas, out var needs) ||
                !ProducesGases.TryGetValue(gas, out var produces))
            {
                return 0;
            }

            return needs * produces * usedAverage;
        }

        private Dictionary<Gas, float> GasProduced(float usedAverage)
        {
            return ProducesGases.ToDictionary(pair => pair.Key, pair => GasProducedMultiplier(pair.Key, usedAverage));
        }

        private void ProcessGases(float frameTime)
        {
            if (!Owner.TryGetComponent(out BloodstreamComponent bloodstream))
            {
                return;
            }

            var usedPercentages = new float[Atmospherics.TotalNumberOfGases];
            var needs = NeedsAndDeficit(frameTime);
            foreach (var (gas, amountNeeded) in needs)
            {
                var bloodstreamAmount = bloodstream.Air.GetMoles(gas);
                var deficit = 0f;

                if (bloodstreamAmount >= amountNeeded)
                {
                    bloodstream.Air.AdjustMoles(gas, -amountNeeded);
                }
                else
                {
                    deficit = amountNeeded - bloodstreamAmount;
                    bloodstream.Air.SetMoles(gas, 0);
                }

                DeficitGases[gas] = deficit;

                var used = amountNeeded - deficit;
                usedPercentages[(int) gas] = used / amountNeeded;
            }

            var usedAverage = usedPercentages.Average();
            var produced = GasProduced(usedAverage);

            foreach (var (gas, amountProduced) in produced)
            {
                bloodstream.Air.AdjustMoles(gas, amountProduced);
            }

            ClampDeficit();
        }

        /// <summary>
        ///     Loops through each reagent in _internalSolution,
        ///     and calls <see cref="IMetabolizable.Metabolize"/> for each of them.
        /// </summary>
        /// <param name="frameTime">The time since the last metabolism tick in seconds.</param>
        private void ProcessNutrients(float frameTime)
        {
            if (!Owner.TryGetComponent(out BloodstreamComponent bloodstream))
            {
                return;
            }

            if (bloodstream.Solution.CurrentVolume == 0)
            {
                return;
            }

            // Run metabolism for each reagent, remove metabolized reagents
            // Using ToList here lets us edit reagents while iterating
            foreach (var reagent in bloodstream.Solution.ReagentList.ToList())
            {
                if (!_prototypeManager.TryIndex(reagent.ReagentId, out ReagentPrototype prototype))
                {
                    continue;
                }

                // Run metabolism code for each reagent
                foreach (var metabolizable in prototype.Metabolism)
                {
                    var reagentDelta = metabolizable.Metabolize(Owner, reagent.ReagentId, frameTime);
                    bloodstream.Solution.TryRemoveReagent(reagent.ReagentId, reagentDelta);
                }
            }
        }

        /// <summary>
        ///     Processes gases in the bloodstream and triggers metabolism of the
        ///     reagents inside of it.
        /// </summary>
        /// <param name="frameTime">
        ///     The time since the last metabolism tick in seconds.
        /// </param>
        public void Update(float frameTime)
        {
            _accumulatedFrameTime += frameTime;

            if (_accumulatedFrameTime < 1)
            {
                return;
            }

            _accumulatedFrameTime -= 1;

            ProcessGases(frameTime);
            ProcessNutrients(frameTime);

            if (Suffocating &&
                Owner.TryGetComponent(out IDamageableComponent damageable))
            {
                // damageable.ChangeDamage(DamageClass.Airloss, _suffocationDamage, false);
            }
        }

        public void Transfer(BloodstreamComponent @from, GasMixture to, Gas gas, float pressure)
        {
            var transfer = new GasMixture();
            var molesInBlood = @from.Air.GetMoles(gas);

            transfer.SetMoles(gas, molesInBlood);
            transfer.ReleaseGasTo(to, pressure);

            @from.Air.Merge(transfer);
        }

        public GasMixture Clean(BloodstreamComponent bloodstream, float pressure = 100)
        {
            var gasMixture = new GasMixture(bloodstream.Air.Volume);

            for (Gas gas = 0; gas < (Gas) Atmospherics.TotalNumberOfGases; gas++)
            {
                if (NeedsGases.TryGetValue(gas, out var needed) &&
                    bloodstream.Air.GetMoles(gas) < needed * 1.5f)
                {
                    continue;
                }

                Transfer(bloodstream, gasMixture, gas, pressure);
            }

            return gasMixture;
        }
    }
}
