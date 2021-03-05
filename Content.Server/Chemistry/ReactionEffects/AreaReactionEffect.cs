﻿#nullable enable
using System;
using Content.Server.GameObjects.Components.Chemistry;
using Content.Server.Utility;
using Content.Shared.Audio;
using Content.Shared.Interfaces.Chemistry;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Chemistry.ReactionEffects
{
    /// <summary>
    /// Basically smoke and foam reactions.
    /// </summary>
    [UsedImplicitly]
    [ImplicitDataDefinitionForInheritors]
    public abstract class AreaReactionEffect : IReactionEffect
    {
        [Dependency] private readonly IMapManager _mapManager = default!;

        /// <summary>
        /// Used for calculating the spread range of the effect based on the intensity of the reaction.
        /// </summary>
        [DataField("rangeConstant")] private float _rangeConstant;
        [DataField("rangeMultiplier")] private float _rangeMultiplier = 1.1f;
        [DataField("maxRange")] private int _maxRange = 10;

        /// <summary>
        /// If true the reagents get diluted or concentrated depending on the range of the effect
        /// </summary>
        [DataField("diluteReagents")] private bool _diluteReagents;

        /// <summary>
        /// At what range should the reagents volume stay the same. If the effect range is higher than this then the reagents
        /// will get diluted. If the effect range is lower than this then the reagents will get concentrated.
        /// </summary>
        [DataField("reagentDilutionStart")] private int _reagentDilutionStart = 4;

        /// <summary>
        /// Used to calculate dilution. Increasing this makes the reagents get more diluted. This means that a lower range
        /// will be needed to make the reagents volume get closer to zero.
        /// </summary>
        [DataField("reagentDilutionFactor")] private float _reagentDilutionFactor = 1;

        /// <summary>
        /// Used to calculate concentration. Reagents get linearly more concentrated as the range goes from
        /// _reagentDilutionStart to zero. When the range is zero the reagents volume gets multiplied by this.
        /// </summary>
        [DataField("reagentMaxConcentrationFactor")]
        private float _reagentMaxConcentrationFactor = 2;

        /// <summary>
        /// How many seconds will the effect stay, counting after fully spreading.
        /// </summary>
        [DataField("duration")] private float _duration = 10;

        /// <summary>
        /// How many seconds between each spread step.
        /// </summary>
        [DataField("spreadDelay")] private float _spreadDelay = 0.5f;

        /// <summary>
        /// How many seconds between each remove step.
        /// </summary>
        [DataField("removeDelay")] private float _removeDelay = 0.5f;

        /// <summary>
        /// The entity prototype that will be spawned as the effect. It needs a component derived from SolutionAreaEffectComponent.
        /// </summary>
        [DataField("prototypeId", required: true)]
        private string _prototypeId = default!;

        /// <summary>
        /// Sound that will get played when this reaction effect occurs.
        /// </summary>
        [DataField("sound")] private string? _sound;

        protected AreaReactionEffect()
        {
            IoCManager.InjectDependencies(this);
        }

        public void React(IEntity solutionEntity, double intensity)
        {
            if (!solutionEntity.TryGetComponent(out SolutionContainerComponent? contents))
                return;

            var solution = contents.SplitSolution(contents.MaxVolume);
            // We take the square root so it becomes harder to reach higher amount values
            var amount = (int) Math.Round(_rangeConstant + _rangeMultiplier*Math.Sqrt(intensity));
            amount = Math.Min(amount, _maxRange);

            if (_diluteReagents)
            {
                // The maximum value of solutionFraction is _reagentMaxConcentrationFactor, achieved when amount = 0
                // The infimum of solutionFraction is 0, which is approached when amount tends to infinity
                // solutionFraction is equal to 1 only when amount equals _reagentDilutionStart
                float solutionFraction;
                if (amount >= _reagentDilutionStart)
                {
                    // Weird formulas here but basically when amount increases, solutionFraction gets closer to 0 in a reciprocal manner
                    // _reagentDilutionFactor defines how fast solutionFraction gets closer to 0
                    solutionFraction = 1 / (_reagentDilutionFactor*(amount - _reagentDilutionStart) + 1);
                }
                else
                {
                    // Here when amount decreases, solutionFraction gets closer to _reagentMaxConcentrationFactor in a linear manner
                    solutionFraction = amount * (1 - _reagentMaxConcentrationFactor) / _reagentDilutionStart +
                                       _reagentMaxConcentrationFactor;
                }
                solution.RemoveSolution(solution.TotalVolume * solutionFraction);
            }

            if (!_mapManager.TryFindGridAt(solutionEntity.Transform.MapPosition, out var grid)) return;

            var coords = grid.MapToGrid(solutionEntity.Transform.MapPosition);

            var ent = solutionEntity.EntityManager.SpawnEntity(_prototypeId, coords.SnapToGrid());

            var areaEffectComponent = GetAreaEffectComponent(ent);

            if (areaEffectComponent == null)
            {
                Logger.Error("Couldn't get AreaEffectComponent from " + _prototypeId);
                ent.Delete();
                return;
            }

            areaEffectComponent.TryAddSolution(solution);
            areaEffectComponent.Start(amount, _duration, _spreadDelay, _removeDelay);

            if (!string.IsNullOrEmpty(_sound))
            {
                EntitySystem.Get<AudioSystem>().PlayFromEntity(_sound, solutionEntity, AudioHelpers.WithVariation(0.125f));
            }
        }

        protected abstract SolutionAreaEffectComponent? GetAreaEffectComponent(IEntity entity);
    }
}
