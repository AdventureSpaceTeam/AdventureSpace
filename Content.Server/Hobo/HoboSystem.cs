using System.Numerics;
using Content.Shared.Eye;
using Content.Shared.Hobo.Components;

namespace Content.Server.Hobo;

public sealed partial class HoboSystem : EntitySystem
{
    [Dependency] private readonly SharedEyeSystem _eye = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HoboComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(EntityUid uid, HoboComponent component, ComponentStartup args)
    {
        //ghost vision
        if (TryComp(uid, out EyeComponent? eye))
        {
            _eye.SetVisibilityMask(uid, eye.VisibilityMask | (int) (VisibilityFlags.Ghost), eye);
        }
    }
}
