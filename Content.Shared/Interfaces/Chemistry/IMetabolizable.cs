﻿using Content.Shared.Chemistry;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Serialization;

namespace Content.Shared.Interfaces.Chemistry
{
    /// <summary>
    /// Metabolism behavior for a reagent.
    /// </summary>
    public interface IMetabolizable : IExposeData
    {
        /// <summary>
        /// Metabolize the attached reagent. Return the amount of reagent to be removed from the solution.
        /// You shouldn't remove the reagent yourself to avoid invalidating the iterator of the metabolism
        /// organ that is processing it's reagents.
        /// </summary>
        /// <param name="solutionEntity">The entity containing the solution.</param>
        /// <param name="reagentId">The reagent id</param>
        /// <param name="tickTime">The time since the last metabolism tick in seconds.</param>
        /// <returns>The amount of reagent to be removed. The metabolizing organ should handle removing the reagent.</returns>
        ReagentUnit Metabolize(IEntity solutionEntity, string reagentId, float tickTime);
    }
}
