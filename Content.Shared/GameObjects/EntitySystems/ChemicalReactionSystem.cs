using Content.Shared.Chemistry;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using System.Collections.Generic;

namespace Content.Shared.GameObjects.EntitySystems
{
    //TODO: Reimplement sounds for reactions
    public class ChemicalReactionSystem : EntitySystem
    {
        private IEnumerable<ReactionPrototype> _reactions;

        private const int MaxReactionIterations = 20;

        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public override void Initialize()
        {
            base.Initialize();
            _reactions = _prototypeManager.EnumeratePrototypes<ReactionPrototype>();
        }

        /// <summary>
        ///     Checks if a solution can undergo a specified reaction.
        /// </summary>
        /// <param name="solution">The solution to check.</param>
        /// <param name="reaction">The reaction to check.</param>
        /// <param name="lowestUnitReactions">How many times this reaction can occur.</param>
        /// <returns></returns>
        private static bool CanReact(Solution solution, ReactionPrototype reaction, out ReagentUnit lowestUnitReactions)
        {
            lowestUnitReactions = ReagentUnit.MaxValue;

            foreach (var reactantData in reaction.Reactants)
            {
                var reactantName = reactantData.Key;
                var reactantCoefficient = reactantData.Value.Amount;

                if (!solution.ContainsReagent(reactantName, out var reactantQuantity))
                    return false;

                var unitReactions = reactantQuantity / reactantCoefficient;

                if (unitReactions < lowestUnitReactions)
                {
                    lowestUnitReactions = unitReactions;
                }
            }
            return true;
        }

        /// <summary>
        ///     Perform a reaction on a solution. This assumes all reaction criteria are met.
        ///     Removes the reactants from the solution, then returns a solution with all products.
        /// </summary>
        private static Solution PerformReaction(Solution solution, IEntity owner, ReactionPrototype reaction, ReagentUnit unitReactions)
        {
            //Remove reactants
            foreach (var reactant in reaction.Reactants)
            {
                if (!reactant.Value.Catalyst)
                {
                    var amountToRemove = unitReactions * reactant.Value.Amount;
                    solution.RemoveReagent(reactant.Key, amountToRemove);
                }
            }

            //Create products
            var products = new Solution();
            foreach (var product in reaction.Products)
            {
                products.AddReagent(product.Key, product.Value * unitReactions);
            }

            // Trigger reaction effects
            foreach (var effect in reaction.Effects)
            {
                effect.React(owner, unitReactions.Double());
            }

            return products;
        }

        /// <summary>
        ///     Performs all chemical reactions that can be run on a solution.
        ///     Removes the reactants from the solution, then returns a solution with all products.
        ///     WARNING: Does not trigger reactions between solution and new products.
        /// </summary>
        private Solution ProcessReactions(Solution solution, IEntity owner)
        {
            //TODO: make a hashmap at startup and then look up reagents in the contents for a reaction
            var overallProducts = new Solution();
            foreach (var reaction in _reactions)
            {
                if (CanReact(solution, reaction, out var unitReactions))
                {
                    var reactionProducts = PerformReaction(solution, owner, reaction, unitReactions);
                    overallProducts.AddSolution(reactionProducts);
                    break;
                }
            }
            return overallProducts;
        }

        /// <summary>
        ///     Continually react a solution until no more reactions occur.
        /// </summary>
        public void FullyReactSolution(Solution solution, IEntity owner)
        {
            for (var i = 0; i < MaxReactionIterations; i++)
            {
                var products = ProcessReactions(solution, owner);

                if (products.TotalVolume <= 0)
                    return;

                solution.AddSolution(products);
            }
            Logger.Error($"{nameof(Solution)} on {owner} (Uid: {owner.Uid}) could not finish reacting in under {MaxReactionIterations} loops.");
        }

        /// <summary>
        ///     Continually react a solution until no more reactions occur, with a volume constraint.
        ///     If a reaction's products would exceed the max volume, some product is deleted.
        /// </summary>
        public void FullyReactSolution(Solution solution, IEntity owner, ReagentUnit maxVolume)
        {
            for (var i = 0; i < MaxReactionIterations; i++)
            {
                var products = ProcessReactions(solution, owner);

                if (products.TotalVolume <= 0)
                    return;

                var totalVolume = solution.TotalVolume + products.TotalVolume;
                var excessVolume = totalVolume - maxVolume; 

                if (excessVolume > 0)
                {
                    products.RemoveSolution(excessVolume); //excess product is deleted to fit under volume limit
                }

                solution.AddSolution(products);
            }
            Logger.Error($"{nameof(Solution)} on {owner} (Uid: {owner.Uid}) could not finish reacting in under {MaxReactionIterations} loops.");
        }
    }
}
