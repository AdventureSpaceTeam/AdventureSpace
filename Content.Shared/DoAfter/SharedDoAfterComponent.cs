using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.DoAfter
{
    [NetworkedComponent()]
    public abstract class SharedDoAfterComponent : Component
    {
        public override string Name => "DoAfter";
    }

    [Serializable, NetSerializable]
    public sealed class DoAfterComponentState : ComponentState
    {
        public List<ClientDoAfter> DoAfters { get; }

        public DoAfterComponentState(List<ClientDoAfter> doAfters)
        {
            DoAfters = doAfters;
        }
    }

    [Serializable, NetSerializable]
#pragma warning disable 618
    public sealed class CancelledDoAfterMessage : ComponentMessage
#pragma warning restore 618
    {
        public byte ID { get; }

        public CancelledDoAfterMessage(byte id)
        {
            ID = id;
        }
    }

    /// <summary>
    ///     We send a trimmed-down version of the DoAfter for the client for it to use.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class ClientDoAfter
    {
        // To see what these do look at DoAfter and DoAfterEventArgs
        public byte ID { get; }

        public TimeSpan StartTime { get; }

        public EntityCoordinates UserGrid { get; }

        public EntityCoordinates TargetGrid { get; }

        public EntityUid? Target { get; }

        public float Delay { get; }

        // TODO: The other ones need predicting
        public bool BreakOnUserMove { get; }

        public bool BreakOnTargetMove { get; }

        public float MovementThreshold { get; }

        public ClientDoAfter(byte id, EntityCoordinates userGrid, EntityCoordinates targetGrid, TimeSpan startTime,
            float delay, bool breakOnUserMove, bool breakOnTargetMove, float movementThreshold, EntityUid? target = null)
        {
            ID = id;
            UserGrid = userGrid;
            TargetGrid = targetGrid;
            StartTime = startTime;
            Delay = delay;
            BreakOnUserMove = breakOnUserMove;
            BreakOnTargetMove = breakOnTargetMove;
            MovementThreshold = movementThreshold;
            Target = target;
        }
    }
}
