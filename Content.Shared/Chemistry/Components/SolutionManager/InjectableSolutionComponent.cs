﻿using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Chemistry.Components.SolutionManager
{
    /// <summary>
    ///     Denotes a solution which can be added with syringes.
    /// </summary>
    [RegisterComponent]
    public class InjectableSolutionComponent : Component
    {
        public override string Name => "InjectableSolution";

        /// <summary>
        /// Solution name which can be added with syringes.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("solution")]
        public string Solution { get; set; } = "default";
    }
}
