﻿using System;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Chemistry.Components
{
    /// <summary>
    /// On colliding with an entity that has a bloodstream will dump its solution onto them.
    /// </summary>
    [RegisterComponent]
    internal sealed class SolutionInjectOnCollideComponent : Component
    {
        public override string Name => "SolutionInjectOnCollide";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("transferAmount")]
        public ReagentUnit TransferAmount { get; set; } = ReagentUnit.New(1);

        [ViewVariables(VVAccess.ReadWrite)]
        public float TransferEfficiency { get => _transferEfficiency; set => _transferEfficiency = Math.Clamp(value, 0, 1); }

        [DataField("transferEfficiency")]
        private float _transferEfficiency = 1f;
    }
}
