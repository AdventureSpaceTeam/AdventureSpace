using Content.Shared.FixedPoint;

namespace Content.Server.Medical;

[ByRefEvent]
public record struct EntityHealedEvent(FixedPoint2 Healed);
