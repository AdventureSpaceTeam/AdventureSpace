namespace Content.Shared.AdventureSpace.Vampire.Attempt;

public sealed partial class VampireHypnosisAttemptEvent : CancellableEntityEventArgs
{
    public readonly EntityUid Target;
    public readonly EntityUid User;
    public readonly bool FullPower;

    public VampireHypnosisAttemptEvent(EntityUid target, EntityUid user, bool fullPower)
    {
        Target = target;
        User = user;
        FullPower = fullPower;
    }
}
