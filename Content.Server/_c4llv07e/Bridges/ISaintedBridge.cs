namespace Content.Server._c4llv07e.Bridges;

//TODO BY UR
public interface ISaintedBridge
{
    bool TryMakeSainted(EntityUid user, EntityUid uid);
}

public sealed class StubSaintedBridge : ISaintedBridge
{
    public bool TryMakeSainted(EntityUid user, EntityUid uid) => false;
}
