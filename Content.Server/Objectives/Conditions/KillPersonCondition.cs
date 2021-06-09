﻿#nullable enable
using Content.Server.Objectives.Interfaces;
using Robust.Shared.Localization;
using Robust.Shared.Utility;

namespace Content.Server.Objectives.Conditions
{
    public abstract class KillPersonCondition : IObjectiveCondition
    {
        protected Mind.Mind? Target;
        public abstract IObjectiveCondition GetAssigned(Mind.Mind mind);

        public string Title => Loc.GetString("Kill {0}", Target?.OwnedEntity?.Name ?? "");

        public string Description => Loc.GetString("Do it however you like, just make sure they don't last the shift.");

        public SpriteSpecifier Icon => new SpriteSpecifier.Rsi(new ResourcePath("Objects/Weapons/Guns/Pistols/mk58_wood.rsi"), "icon");

        public float Progress => (Target?.CharacterDeadIC ?? true) ? 1f : 0f;

        public float Difficulty => 2f;

        public bool Equals(IObjectiveCondition? other)
        {
            return other is KillPersonCondition kpc && Equals(Target, kpc.Target);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((KillPersonCondition) obj);
        }

        public override int GetHashCode()
        {
            return Target?.GetHashCode() ?? 0;
        }
    }
}
