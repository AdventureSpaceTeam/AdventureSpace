namespace Content.Server.Body.Events;

[ByRefEvent]
public record struct OnEntityBreathGas(string? Reagent, float Amount);
