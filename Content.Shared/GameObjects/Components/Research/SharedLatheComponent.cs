using System;
using System.Collections.Generic;
using Content.Shared.Research;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Research
{
    public class SharedLatheComponent : Component
    {
        [Dependency] protected readonly IPrototypeManager PrototypeManager = default!;

        public override string Name => "Lathe";
        public override uint? NetID => ContentNetIDs.LATHE;

        public bool CanProduce(LatheRecipePrototype recipe, int quantity = 1)
        {
            if (!Owner.TryGetComponent(out SharedMaterialStorageComponent storage)
            ||  !Owner.TryGetComponent(out SharedLatheDatabaseComponent database)) return false;

            if (!database.Contains(recipe)) return false;

            foreach (var (material, amount) in recipe.RequiredMaterials)
            {
                if (storage[material] <= (amount * quantity)) return false;
            }

            return true;
        }

        public bool CanProduce(string ID, int quantity = 1)
        {
            return PrototypeManager.TryIndex(ID, out LatheRecipePrototype recipe) && CanProduce(recipe, quantity);
        }

        /// <summary>
        ///     Sent to the server to sync material storage and the recipe queue.
        /// </summary>
        [Serializable, NetSerializable]
        public class LatheSyncRequestMessage : BoundUserInterfaceMessage
        {
            public LatheSyncRequestMessage()
            {
            }
        }



        /// <summary>
        ///     Sent to the server to sync the lathe's technology database with the research server.
        /// </summary>
        [Serializable, NetSerializable]
        public class LatheServerSyncMessage : BoundUserInterfaceMessage
        {
            public LatheServerSyncMessage()
            {
            }
        }

        /// <summary>
        ///     Sent to the server to open the ResearchClient UI.
        /// </summary>
        [Serializable, NetSerializable]
        public class LatheServerSelectionMessage : BoundUserInterfaceMessage
        {
            public LatheServerSelectionMessage()
            {
            }
        }

        /// <summary>
        ///     Sent to the client when the lathe is producing a recipe.
        /// </summary>
        [Serializable, NetSerializable]
        public class LatheProducingRecipeMessage : BoundUserInterfaceMessage
        {
            public readonly string ID;
            public LatheProducingRecipeMessage(string id)
            {
                ID = id;
            }
        }

        /// <summary>
        ///     Sent to the client when the lathe stopped/finished producing a recipe.
        /// </summary>
        [Serializable, NetSerializable]
        public class LatheStoppedProducingRecipeMessage : BoundUserInterfaceMessage
        {
            public LatheStoppedProducingRecipeMessage()
            {
            }
        }

        /// <summary>
        ///     Sent to the client to let it know about the recipe queue.
        /// </summary>
        [Serializable, NetSerializable]
        public class LatheFullQueueMessage : BoundUserInterfaceMessage
        {
            public readonly Queue<string> Recipes;
            public LatheFullQueueMessage(Queue<string> recipes)
            {
                Recipes = recipes;
            }
        }

        /// <summary>
        ///     Sent to the server when a client queues a new recipe.
        /// </summary>
        [Serializable, NetSerializable]
        public class LatheQueueRecipeMessage : BoundUserInterfaceMessage
        {
            public readonly string ID;
            public readonly int Quantity;
            public LatheQueueRecipeMessage(string id, int quantity)
            {
                ID = id;
                Quantity = quantity;
            }
        }

        [NetSerializable, Serializable]
        public enum LatheUiKey
        {
            Key,
        }
    }
}
