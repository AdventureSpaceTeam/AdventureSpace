﻿#nullable enable
using System.Collections.Generic;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Prototypes
{
    [Prototype("dataset")]
    public class DatasetPrototype : IPrototype
    {
        [ViewVariables]
        [field: DataField("id", required: true)]
        public string ID { get; } = default!;

        [field: DataField("values")] public IReadOnlyList<string> Values { get; } = new List<string>();
    }
}
