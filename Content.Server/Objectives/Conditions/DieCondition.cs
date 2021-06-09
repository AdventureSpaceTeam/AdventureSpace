﻿#nullable enable
using Content.Server.Objectives.Interfaces;
using JetBrains.Annotations;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Content.Server.Objectives.Conditions
{
    [UsedImplicitly]
    [DataDefinition]
    public class DieCondition : IObjectiveCondition
    {
        private Mind.Mind? _mind;

        public IObjectiveCondition GetAssigned(Mind.Mind mind)
        {
            return new DieCondition {_mind = mind};
        }

        public string Title => Loc.GetString("Die a glorious death");

        public string Description => Loc.GetString("Die.");

        public SpriteSpecifier Icon => new SpriteSpecifier.Rsi(new ResourcePath("Mobs/Ghosts/ghost_human.rsi"), "icon");

        public float Progress => (_mind?.CharacterDeadIC ?? true) ? 1f : 0f;

        public float Difficulty => 1f;

        public bool Equals(IObjectiveCondition? other)
        {
            return other is DieCondition condition && Equals(_mind, condition._mind);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((DieCondition) obj);
        }

        public override int GetHashCode()
        {
            return (_mind != null ? _mind.GetHashCode() : 0);
        }
    }
}
