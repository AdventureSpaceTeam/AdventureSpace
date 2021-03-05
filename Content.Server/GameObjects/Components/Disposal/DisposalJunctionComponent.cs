﻿using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Disposal
{
    [RegisterComponent]
    [ComponentReference(typeof(IDisposalTubeComponent))]
    public class DisposalJunctionComponent : DisposalTubeComponent
    {
        [Dependency] private readonly IRobustRandom _random = default!;

        /// <summary>
        ///     The angles to connect to.
        /// </summary>
        [ViewVariables]
        [DataField("degrees")]
        private List<Angle> _degrees;

        public override string Name => "DisposalJunction";

        protected override Direction[] ConnectableDirections()
        {
            var direction = Owner.Transform.LocalRotation;

            return _degrees.Select(degree => new Angle(degree.Theta + direction.Theta).GetDir()).ToArray();
        }

        public override Direction NextDirection(DisposalHolderComponent holder)
        {
            var next = Owner.Transform.LocalRotation.GetDir();
            var directions = ConnectableDirections().Skip(1).ToArray();

            if (holder.PreviousTube == null ||
                DirectionTo(holder.PreviousTube) == next)
            {
                return _random.Pick(directions);
            }

            return next;
        }
    }
}
