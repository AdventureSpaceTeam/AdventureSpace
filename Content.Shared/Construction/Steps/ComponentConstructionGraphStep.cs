﻿using Content.Shared.Examine;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility.Markup;

namespace Content.Shared.Construction.Steps
{
    [DataDefinition]
    public class ComponentConstructionGraphStep : ArbitraryInsertConstructionGraphStep
    {
        [DataField("component")] public string Component { get; } = string.Empty;

        public override bool EntityValid(EntityUid uid, IEntityManager entityManager)
        {
            foreach (var component in entityManager.GetComponents(uid))
            {
                if (component.Name == Component)
                    return true;
            }

            return false;
        }

        public override void DoExamine(ExaminedEvent examinedEvent)
        {
            examinedEvent.Message.AddMessage(
                    Basic.RenderMarkup(string.IsNullOrEmpty(Name)
                        ? Loc.GetString(
                            "construction-insert-entity-with-component",
                            ("componentName", Component))// Terrible.
                        : Loc.GetString(
                            "construction-insert-exact-entity",
                            ("entityName", Name))
                    ));
        }
    }
}
