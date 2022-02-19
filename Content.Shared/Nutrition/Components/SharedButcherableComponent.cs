using Content.Shared.DragDrop;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Nutrition.Components
{
    /// <summary>
    /// Indicates that the entity can be thrown on a kitchen spike for butchering.
    /// </summary>
    [RegisterComponent]
    public sealed class SharedButcherableComponent : Component, IDraggable
    {
        //TODO: List for sub-products like animal-hides, organs and etc?
        [ViewVariables]
        [DataField("spawned", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string SpawnedPrototype = "FoodMeat";

        [ViewVariables]
        [DataField("pieces")]
        public int Pieces = 5;

        [DataField("butcherDelay")]
        public float ButcherDelay = 8.0f;

        [DataField("butcheringType")]
        public ButcheringType Type = ButcheringType.Knife;

        /// <summary>
        /// Prevents butchering same entity on two and more spikes simultaneously and multiple doAfters on the same Spike
        /// </summary>
        public bool BeingButchered;

        // TODO: ECS this out!, my guess CanDropEvent should be client side only and then "ValidDragDrop" in the DragDropSystem needs a little touch
        // But this may lead to creating client-side systems for every Draggable component subbed to CanDrop. Actually those systems could control
        // CanDropOn behaviors as well (IDragDropOn)
        bool IDraggable.CanDrop(CanDropEvent args)
        {
            return Type != ButcheringType.Knife;
        }
    }

    public enum ButcheringType
    {
        Knife, // e.g. goliaths
        Spike, // e.g. monkeys
        Gibber // e.g. humans. TODO
    }
}
