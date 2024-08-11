using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._c4llv07e.DonateUplink;

[RegisterComponent, NetworkedComponent]
public sealed partial class DonateUplinkComponent : Component
{
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool Opened = false;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public string BackplateText = "Спасибо за донат!";
}

[Serializable, NetSerializable]
public sealed class DonateUplinkComponentState : ComponentState
{
    public bool Open { get; init; }
}


[Serializable, NetSerializable]
public sealed partial class DonateUplinkScrewingFinishedEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public enum DonateUplinkVisualLayers : byte
{
    Base,
    Opened,
}
