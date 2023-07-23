﻿using Content.Shared.Maps;
using Content.Shared.Tag;
using JetBrains.Annotations;
using Robust.Shared.Map;

namespace Content.Shared.Construction.Conditions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed class NoWindowsInTile : IConstructionCondition
    {
        public bool Condition(EntityUid user, EntityCoordinates location, Direction direction)
        {
            var entManager = IoCManager.Resolve<IEntityManager>();
            var tagQuery = entManager.GetEntityQuery<TagComponent>();
            var sysMan = entManager.EntitySysManager;
            var tagSystem = sysMan.GetEntitySystem<TagSystem>();

            foreach (var entity in location.GetEntitiesInTile(LookupFlags.Static))
            {
                if (tagSystem.HasTag(entity, "Window", tagQuery))
                    return false;
            }

            return true;
        }

        public ConstructionGuideEntry GenerateGuideEntry()
        {
            return new ConstructionGuideEntry
            {
                Localization = "construction-step-condition-no-windows-in-tile"
            };
        }
    }
}
