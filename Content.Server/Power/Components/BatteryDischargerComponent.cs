﻿using Content.Server.Power.NodeGroups;
using Robust.Shared.GameObjects;

namespace Content.Server.Power.Components
{
    [RegisterComponent]
    public class BatteryDischargerComponent : BasePowerNetComponent
    {
        public override string Name => "BatteryDischarger";

        protected override void AddSelfToNet(IPowerNet net)
        {
            net.AddDischarger(this);
        }

        protected override void RemoveSelfFromNet(IPowerNet net)
        {
            net.RemoveDischarger(this);
        }
    }
}
