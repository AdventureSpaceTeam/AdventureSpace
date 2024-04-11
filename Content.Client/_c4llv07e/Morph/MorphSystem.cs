using Content.Shared.Verbs;
using Content.Shared._c4llv07e.Morph;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Shared.Utility;
using Robust.Shared.Serialization.TypeSerializers.Implementations;

namespace Content.Client._c4llv07e.Morph;

public sealed class SharedMorphSystem : EntitySystem
{
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MorphComponent, AppearanceChangeEvent>(OnAppearanceChange);
        SubscribeLocalEvent<MorphComponent, GetVerbsEvent<InnateVerb>>(AddMorphVerb);
    }
    private void OnAppearanceChange(Entity<MorphComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;
        if (!_appearance.TryGetData<ResPath>(ent.Owner, MorphVisuals.ResPath, out var path, args.Component))
            return;
        if (!_appearance.TryGetData<string>(ent.Owner, MorphVisuals.StateId, out var state, args.Component))
            return;
        var spriteSpecifier = new SpriteSpecifier.Rsi(path, state);

        // Really bad way to do this. But it is the only one that doesn't
        // require reflection which makes it the best.
        var i = 0;
        foreach (var layer in args.Sprite.AllLayers)
        {
            args.Sprite.LayerSetSprite(i, spriteSpecifier);
            i += 1;
        }
    }
    private void AddMorphVerb(Entity<MorphComponent> ent, ref GetVerbsEvent<InnateVerb> args)
    {
        var user = args.User;
        var target = args.Target;
        if (!TryComp(target, out SpriteComponent? sprite) || !sprite.Visible)
            return;
        if (sprite.BaseRSI == null)
            return;
        var rsiPath = sprite.BaseRSI.Path;
        var rsiState = sprite.LayerGetState(0);
        if (rsiState == null)
            return;
        if (rsiState.Name == null)
            return;
        InnateVerb verb = new()
        {
            Act = () =>
            {
                var ev = new MorphEvent(GetNetEntity(user), rsiPath, rsiState.Name);
                RaiseNetworkEvent(ev);
            },
            Text = "Замоскироваться под предмет",
            Icon = new SpriteSpecifier.Rsi(rsiPath, rsiState.Name),
            Priority = 2
        };
        args.Verbs.Add(verb);
    }
}
