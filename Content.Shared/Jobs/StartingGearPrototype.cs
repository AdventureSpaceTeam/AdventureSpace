using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using YamlDotNet.RepresentationModel;
using static Content.Shared.GameObjects.Components.Inventory.EquipmentSlotDefines;

namespace Content.Shared.Jobs
{
    [Prototype("startingGear")]
    public class StartingGearPrototype : IPrototype, IIndexedPrototype
    {
        private string _id;
        private Dictionary<Slots, string> _equipment;

        [ViewVariables] public string ID => _id;

        [ViewVariables] public IReadOnlyDictionary<Slots, string> Equipment => _equipment;

        public void LoadFrom(YamlMappingNode mapping)
        {
            var serializer = YamlObjectSerializer.NewReader(mapping);

            serializer.DataField(ref _id, "id", string.Empty);

            var equipment = serializer.ReadDataField<Dictionary<string, string>>("equipment");

            _equipment = equipment.ToDictionary(slotStr =>
            {
                var (key, _) = slotStr;
                if (!Enum.TryParse(key, true, out Slots slot))
                {
                    throw new Exception($"{key} is an invalid equipment slot.");
                }

                return slot;
            }, type => type.Value);
        }
    }
}
