using Content.Server.UserInterface;
using Content.Shared.Lathe;
using Content.Shared.Research.Prototypes;
using Robust.Server.GameObjects;
using Content.Shared.Sound;

namespace Content.Server.Lathe.Components
{
    [RegisterComponent]
    public sealed class LatheComponent : SharedLatheComponent
    {
        /// <summary>
        /// How much volume in cm^3 each sheet of material adds
        /// </summary>
        public int VolumePerSheet = 100;

        /// <summary>
        /// The lathe's construction queue
        /// </summary>
        [ViewVariables]
        public Queue<LatheRecipePrototype> Queue { get; } = new();
        /// <summary>
        /// The recipe the lathe is currently producing
        /// </summary>
        [ViewVariables]
        public LatheRecipePrototype? ProducingRecipe;
        /// <summary>
        /// How long the inserting animation will play
        /// </summary>
        [ViewVariables]
        public float InsertionTime = 0.79f; // 0.01 off for animation timing
        /// <summary>
        /// Update accumulator for the insertion time
        /// </suummary>
        public float InsertionAccumulator = 0f;
        /// <summary>
        /// Production accumulator for the production time.
        /// </summary>
        [ViewVariables]
        public float ProducingAccumulator = 0f;

        /// <summary>
        /// The sound that plays when the lathe is producing an item, if any
        /// </summary>
        [DataField("producingSound")]
        public SoundSpecifier? ProducingSound;

        /// <summmary>
        /// The lathe's UI.
        /// </summary>
        [ViewVariables] public BoundUserInterface? UserInterface;
    }
}
