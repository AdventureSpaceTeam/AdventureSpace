using Robust.Shared.Map;

namespace Content.Shared.Interaction.Events;

public sealed class AltUseInWorldEvent: HandledEntityEventArgs
{
    /// <summary>
    ///     Entity that triggered the interaction.
    /// </summary>
    public EntityUid User;

    /// <summary>
    ///     Entity that was interacted on.
    /// </summary>
    public EntityUid Target;

    public EntityCoordinates ClickLocation;

    public AltUseInWorldEvent(EntityUid user, EntityUid target, EntityCoordinates clickLocation)
    {
        User = user;
        Target = target;
        ClickLocation = clickLocation;
    }
}

