using Robust.Shared.Player;

namespace Content.Server._c4llv07e.Bridges;

//TODO BY UR
public interface IAntagBridge
{
    void ForceMakeCultist(ICommonSession session);

    void ForceMakeCultistLeader(ICommonSession session);

    void ForceMakeRatvarRighteous(ICommonSession session);

    void ForceMakeRatvarRighteous(EntityUid uid);

    void ForceMakeVampire(ICommonSession session);
}

public sealed class StubAntagBridge : IAntagBridge
{
    public void ForceMakeCultist(ICommonSession session) { }

    public void ForceMakeCultistLeader(ICommonSession session) { }

    public void ForceMakeRatvarRighteous(ICommonSession session) { }

    public void ForceMakeRatvarRighteous(EntityUid uid) { }

    public void ForceMakeVampire(ICommonSession session) { }
}
