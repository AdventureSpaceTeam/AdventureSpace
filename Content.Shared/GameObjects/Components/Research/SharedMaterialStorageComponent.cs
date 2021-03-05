#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using Content.Shared.Materials;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components.Research
{
    public class SharedMaterialStorageComponent : Component, IEnumerable<KeyValuePair<string, int>>
    {
        public override string Name => "MaterialStorage";
        public sealed override uint? NetID => ContentNetIDs.MATERIAL_STORAGE;

        [ViewVariables]
        protected virtual Dictionary<string, int> Storage { get; set; } = new();

        public int this[string ID]
        {
            get
            {
                if (!Storage.ContainsKey(ID))
                    return 0;
                return Storage[ID];
            }
        }

        public int this[MaterialPrototype material]
        {
            get
            {
                var ID = material.ID;
                if (!Storage.ContainsKey(ID))
                    return 0;
                return Storage[ID];
            }
        }

        /// <summary>
        ///     The total volume of material stored currently.
        /// </summary>
        [ViewVariables] public int CurrentAmount
        {
            get
            {
                var value = 0;

                foreach (var amount in Storage.Values)
                {
                    value += amount;
                }

                return value;
            }
        }

        public IEnumerator<KeyValuePair<string, int>> GetEnumerator()
        {
            return Storage.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    [NetSerializable, Serializable]
    public class MaterialStorageState : ComponentState
    {
        public readonly Dictionary<string, int> Storage;
        public MaterialStorageState(Dictionary<string, int> storage) : base(ContentNetIDs.MATERIAL_STORAGE)
        {
            Storage = storage;
        }
    }
}
