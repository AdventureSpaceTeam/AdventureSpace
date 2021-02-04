﻿using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.EntitySystemMessages
{
    public static class SuspicionMessages
    {
        [Serializable, NetSerializable]
        public sealed class SetSuspicionEndTimerMessage : EntitySystemMessage
        {
            public TimeSpan? EndTime;
        }
    }
}
