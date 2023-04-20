using Robust.Shared.Map;

namespace Content.Server.Shuttles.Events;

/// <summary>
/// Raised when a shuttle has moved to FTL space.
/// </summary>
[ByRefEvent]
public readonly record struct FTLStartedEvent(EntityUid Entity, EntityCoordinates TargetCoordinates, EntityUid? FromMapUid, Matrix3 FTLFrom, Angle FromRotation);
