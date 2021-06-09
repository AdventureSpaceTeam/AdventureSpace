﻿#nullable enable
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Content.Shared.Construction.Steps
{
    [DataDefinition]
    public class PrototypeConstructionGraphStep : ArbitraryInsertConstructionGraphStep
    {
        [DataField("prototype")] public string Prototype { get; } = string.Empty;

        public override bool EntityValid(IEntity entity)
        {
            return entity.Prototype?.ID == Prototype;
        }

        public override void DoExamine(FormattedMessage message, bool inDetailsRange)
        {
            message.AddMarkup(string.IsNullOrEmpty(Name)
                ? Robust.Shared.Localization.Loc.GetString(
                    "construction-insert-prototype-no-name",
                    ("prototypeName", Prototype) // Terrible.
                )
                : Robust.Shared.Localization.Loc.GetString(
                    "construction-insert-prototype",
                    ("entityName", Name)
                ));
        }
    }
}
