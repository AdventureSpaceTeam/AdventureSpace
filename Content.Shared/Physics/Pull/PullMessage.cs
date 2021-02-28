﻿#nullable enable
using Robust.Shared.GameObjects;
using Robust.Shared.Physics;

namespace Content.Shared.Physics.Pull
{
    public class PullMessage : ComponentMessage
    {
        public readonly IPhysBody Puller;
        public readonly IPhysBody Pulled;

        protected PullMessage(IPhysBody puller, IPhysBody pulled)
        {
            Puller = puller;
            Pulled = pulled;
        }
    }
}
