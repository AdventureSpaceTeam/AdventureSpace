﻿using System.Collections.Generic;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.Prototypes
{
    [Prototype("dataset")]
    public class DatasetPrototype : IPrototype, IIndexedPrototype
    {
        private string _id;
        public string ID => _id;

        private List<string> _values;
        public IReadOnlyList<string> Values => _values;

        public void LoadFrom(YamlMappingNode mapping)
        {
            var ser = YamlObjectSerializer.NewReader(mapping);

            ser.DataField(ref _id, "id", "");
            ser.DataField(ref _values, "values", new List<string>());
        }
    }
}
