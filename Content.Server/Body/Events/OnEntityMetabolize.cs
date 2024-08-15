using Content.Server.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;

namespace Content.Server.Body.Events;

[ByRefEvent]
public record struct OnEntityMetabolize(MetabolismGroupEntry Entry, FixedPoint2 MostToRemove);

[ByRefEvent]
public record struct OnEntityMetabolizeAfterReagent(MetabolismGroupEntry Entry, float Scale);

[ByRefEvent]
public record struct OnEntityAfterMetabolize(FixedPoint2 MostToRemove, Entity<SolutionComponent> Solution);
