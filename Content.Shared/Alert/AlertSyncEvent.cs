using Robust.Shared.GameObjects;

namespace Content.Shared.Alert;

/// <summary>
///     Raised when the AlertSystem needs alert sources to recalculate their alert states and set them.
/// </summary>
public class AlertSyncEvent : EntityEventArgs
{
    public EntityUid Euid { get; }

    public AlertSyncEvent(EntityUid euid)
    {
        Euid = euid;
    }
}
