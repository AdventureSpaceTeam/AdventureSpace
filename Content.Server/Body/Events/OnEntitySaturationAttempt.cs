namespace Content.Server.Body.Events;

[ByRefEvent]
public record struct OnEntitySaturationAttempt(bool HasSaturation);

[ByRefEvent]
public record struct CanProcessEntitySaturation(bool IgnoreAttempt = true);
