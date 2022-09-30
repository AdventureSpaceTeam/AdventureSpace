using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Melee.Events;

/// <summary>
/// Data for melee lunges from attacks.
/// </summary>
[Serializable, NetSerializable]
public sealed class MeleeLungeEvent : EntityEventArgs
{
    public EntityUid Entity;

    /// <summary>
    /// Width of the attack angle.
    /// </summary>
    public Angle Angle;

    /// <summary>
    /// The relative local position to the <see cref="Entity"/>
    /// </summary>
    public Vector2 LocalPos;

    /// <summary>
    /// Entity to spawn for the animation
    /// </summary>
    public string? Animation;

    public MeleeLungeEvent(EntityUid uid, Angle angle, Vector2 localPos, string? animation)
    {
        Entity = uid;
        Angle = angle;
        LocalPos = localPos;
        Animation = animation;
    }
}
