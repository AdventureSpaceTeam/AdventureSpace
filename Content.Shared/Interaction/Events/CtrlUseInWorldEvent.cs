using Robust.Shared.Map;

namespace Content.Shared.Interaction.Events;

public sealed class CtrlUseInWorldEvent : HandledEntityEventArgs
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

    public CtrlUseInWorldEvent(EntityUid user, EntityUid target, EntityCoordinates clickLocation)
    {
        User = user;
        Target = target;
        ClickLocation = clickLocation;
    }
}
