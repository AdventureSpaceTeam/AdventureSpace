using Content.Shared.Containers.ItemSlots;
using Robust.Shared.GameStates;

namespace Content.Shared.AdventureSpace.DarkForces.Ratvar.Righteous.Roles;

[RegisterComponent, NetworkedComponent]
public sealed partial class RatvarMarauderShellComponent : Component
{
    [DataField("soulVesselSlot", required: true)]
    public ItemSlot SoulVesselSlot = new();

    public readonly string SoulVesselSlotId = "SoulVessel";
}
