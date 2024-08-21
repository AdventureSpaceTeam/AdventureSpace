namespace Content.Shared.AdventureSpace.Vampire.Attempt;

public sealed class VampireChiropteanScreechAttemptEvent : CancellableEntityEventArgs
{
    public readonly EntityUid Target;
    public readonly EntityUid User;
    public readonly bool FullPower;

    public VampireChiropteanScreechAttemptEvent(EntityUid target, EntityUid user, bool fullPower)
    {
        Target = target;
        User = user;
        FullPower = fullPower;
    }
}
