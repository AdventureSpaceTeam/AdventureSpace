using Content.Server.DeviceLinking.Systems;
using Content.Shared.DeviceLinking;
using Content.Shared.MachineLinking;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.DeviceLinking.Components;

/// <summary>
/// An edge detector that pulses high or low output ports when the input port gets a rising or falling edge respectively.
/// </summary>
[RegisterComponent]
[Access(typeof(EdgeDetectorSystem))]
public sealed class EdgeDetectorComponent : Component
{
    /// <summary>
    /// Name of the input port.
    /// </summary>
    [DataField("inputPort", customTypeSerializer: typeof(PrototypeIdSerializer<SourcePortPrototype>))]
    public string InputPort = "Input";

    /// <summary>
    /// Name of the rising edge output port.
    /// </summary>
    [DataField("outputHighPort", customTypeSerializer: typeof(PrototypeIdSerializer<TransmitterPortPrototype>))]
    public string OutputHighPort = "OutputHigh";

    /// <summary>
    /// Name of the falling edge output port.
    /// </summary>
    [DataField("outputLowPort", customTypeSerializer: typeof(PrototypeIdSerializer<TransmitterPortPrototype>))]
    public string OutputLowPort = "OutputLow";

    // Initial state
    [ViewVariables]
    public SignalState State = SignalState.Low;
}
