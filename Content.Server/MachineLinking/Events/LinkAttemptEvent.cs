using Content.Server.MachineLinking.Components;
using Robust.Shared.GameObjects;

namespace Content.Server.MachineLinking.Events
{
    public sealed class LinkAttemptEvent : CancellableEntityEventArgs
    {
        public readonly SignalTransmitterComponent TransmitterComponent;
        public readonly string TransmitterPort;
        public readonly SignalReceiverComponent ReceiverComponent;
        public readonly string ReceiverPort;

        public LinkAttemptEvent(SignalTransmitterComponent transmitterComponent, string transmitterPort, SignalReceiverComponent receiverComponent, string receiverPort)
        {
            TransmitterComponent = transmitterComponent;
            this.TransmitterPort = transmitterPort;
            ReceiverComponent = receiverComponent;
            this.ReceiverPort = receiverPort;
        }
    }
}
