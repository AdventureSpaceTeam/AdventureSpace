using Content.Client.Ghost;
using Content.Shared.Hobo.Components;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client.Hobo;

public sealed partial class HoboSystem : EntitySystem
{
    [Dependency] private readonly GhostSystem _ghost = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HoboComponent, LocalPlayerAttachedEvent>(OnPlayerAttach);
    }

    private void OnPlayerAttach(EntityUid uid, HoboComponent component, LocalPlayerAttachedEvent localPlayerAttachedEvent)
    {
        _ghost.SetGhostVisibility(true);
    }
}
