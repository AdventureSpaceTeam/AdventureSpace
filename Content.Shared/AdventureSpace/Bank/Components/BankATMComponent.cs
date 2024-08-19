using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.AdventureSpace.Bank.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class BankATMComponent : Component
{
    public static string CashSlotSlotId = "bank-ATM-cashSlot";

    [DataField]
    public SoundSpecifier ErrorSound = new SoundPathSpecifier("/Audio/Effects/Cargo/buzz_sigh.ogg");

    [DataField]
    public SoundSpecifier ConfirmSound = new SoundPathSpecifier("/Audio/Effects/Cargo/ping.ogg");
}
