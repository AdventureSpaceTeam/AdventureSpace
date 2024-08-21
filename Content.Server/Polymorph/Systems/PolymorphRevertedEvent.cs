namespace Content.Server.Polymorph.Systems;

public sealed class PolymorphRevertedEvent : EntityEventArgs
{
    public EntityUid Original;
    public EntityUid Polymorph;

    public PolymorphRevertedEvent(EntityUid original, EntityUid polymorph)
    {
        Original = original;
        Polymorph = polymorph;
    }
}
