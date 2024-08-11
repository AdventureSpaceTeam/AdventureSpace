using System.Numerics;
using Content.Shared.Eye;
using Content.Shared._c4llv07e.VisibleGhosts;

namespace Content.Server._c4llv07e.VisibleGhosts;

public sealed partial class VisibleGhostsSystem : EntitySystem
{
    [Dependency] private readonly SharedEyeSystem _eye = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<VisibleGhostsComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(EntityUid uid, VisibleGhostsComponent component, ComponentStartup args)
    {
        //ghost vision
        if (TryComp(uid, out EyeComponent? eye))
        {
            _eye.SetVisibilityMask(uid, eye.VisibilityMask | (int) (VisibilityFlags.Ghost), eye);
        }
    }
}
