using Content.Client.Ghost;
using Content.Shared._c4llv07e.VisibleGhosts;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client._c4llv07e.VisibleGhosts;

public sealed partial class VisibleGhostsSystem : EntitySystem
{
    [Dependency] private readonly GhostSystem _ghost = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<VisibleGhostsComponent, LocalPlayerAttachedEvent>(OnPlayerAttach);
    }

    private void OnPlayerAttach(EntityUid uid, VisibleGhostsComponent component, LocalPlayerAttachedEvent localPlayerAttachedEvent)
    {
        _ghost.SetGhostVisibility(true);
    }
}
