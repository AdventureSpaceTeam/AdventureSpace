namespace Content.Shared.AdventureSpace.DarkForces.Ratvar.Righteous.Abilities;

public interface IRatvarAbilityRelay
{
    TimeSpan UseTime { get; set; }
}

public sealed partial class RatvarAbilityWrapper<TEvent> : EntityEventArgs
{
    public TEvent Args;

    public RatvarAbilityWrapper(TEvent args)
    {
        Args = args;
    }
}
