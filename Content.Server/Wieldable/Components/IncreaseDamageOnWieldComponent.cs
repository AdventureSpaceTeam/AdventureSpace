﻿using Content.Shared.Damage;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Wieldable.Components
{
    [RegisterComponent, Friend(typeof(WieldableSystem))]
    public class IncreaseDamageOnWieldComponent : Component
    {
        public override string Name { get; } = "IncreaseDamageOnWield";

        [DataField("modifiers", required: true)]
        public DamageModifierSet Modifiers = default!;
    }
}
