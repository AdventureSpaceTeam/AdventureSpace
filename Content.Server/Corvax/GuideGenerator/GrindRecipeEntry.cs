using System.Text.Json.Serialization;
using Robust.Shared.Prototypes;
using Content.Shared.Chemistry.Reagent;
using Content.Server.Kitchen.Components;
using Content.Shared.Chemistry.Components.SolutionManager;

namespace Content.Server.GuideGenerator;

public sealed class GrindRecipeEntry
{
    /// <summary>
    ///     Id of grindable item
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; }

    /// <summary>
    ///     Human-readable name of recipe.
    ///     Should automatically be localized by default
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; }

    /// <summary>
    ///     Type of recipe
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; }

    /// <summary>
    ///     Item that will be grinded into something
    /// </summary>
    [JsonPropertyName("input")]
    public string Input { get; }

    /// <summary>
    ///     Dictionary of reagents that entity contains; aka "Recipe Result"
    /// </summary>
    [JsonPropertyName("result")]
    public Dictionary<string, int> Result { get; } = new Dictionary<string, int>();


    public GrindRecipeEntry(EntityPrototype proto)
    {
        Id = proto.ID;
        if (proto.Name.Length > 1)
        {
            Name = char.ToUpper(proto.Name[0]) + proto.Name.Remove(0, 1);
        }
        else if (proto.Name.Length == 1)
        {
            Name = char.ToUpper(proto.Name[0]).ToString();
        }
        else
        {
            Name = proto.Name;
        }
        Type = "grindableRecipes";
        Input = proto.ID;
        var foodSulitonName = "food"; // default to food because everything in prototypes defaults to "food"

        // Now, to become a recipe, entity must:
        // A) Have "Extractable" component on it.
        // B) Have "SolutionContainerManager" component on it.
        // C) Have "GrindableSolution" declared in "SolutionContainerManager" component.
        // D) Have solution with name declared in "SolutionContainerManager.GrindableSolution" inside its "SolutionContainerManager" component.
        // F) Have "Food" in its name (see Content.Server/Corvax/GuideGenerator/MealsRecipesJsonGenerator.cs)
        if (proto.Components.TryGetComponent("Extractable", out var extractableComp) && proto.Components.TryGetComponent("SolutionContainerManager", out var solutionCompRaw))
        {
            var extractable = (ExtractableComponent) extractableComp;
            var solutionComp = (SolutionContainerManagerComponent) solutionCompRaw;
            foodSulitonName = extractable.GrindableSolution;

            if (foodSulitonName != null && solutionComp.Solutions.ContainsKey(foodSulitonName))
            {
                foreach (ReagentQuantity reagent in solutionComp.Solutions[(string) foodSulitonName].Contents)
                {
                    Result[reagent.Reagent.Prototype] = reagent.Quantity.Int();
                }
            }
        }
    }
}
