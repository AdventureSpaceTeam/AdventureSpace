using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Shared._c4llv07e.Morph;

public sealed class SharedMorphSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<MorphEvent>(HandleMorph);
    }

    private void HandleMorph(MorphEvent args)
    {
        var netEnt = GetEntity(args.EntityUid);
        _appearance.SetData(netEnt, MorphVisuals.ResPath, args.Path);
        _appearance.SetData(netEnt, MorphVisuals.StateId, args.State);
    }
}
