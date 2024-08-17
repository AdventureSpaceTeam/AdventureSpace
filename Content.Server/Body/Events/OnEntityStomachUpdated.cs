using Content.Shared.Chemistry.Reagent;

namespace Content.Server.Body.Events;

[ByRefEvent]
public record struct OnEntityStomachUpdated(ReagentQuantity Quantity);
