using System.Diagnostics.CodeAnalysis;
using Content.Shared.Verbs;
using Content.Shared._c4llv07e.Morph;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Shared.Utility;
using Robust.Shared.Serialization.TypeSerializers.Implementations;

namespace Content.Client._c4llv07e.Morph;

public sealed class SharedMorphSystem : EntitySystem
{
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
            // I think less errors is always better :/
            // Just for clarification, Mob entity has two or more layers:
            // the actual entity sprites and a speech buble. The game
            // doesn't like when we touch speech's rsi so we check if the
            // layer is buble before we change it.
            // I can't think a better way to check it, ISpriteLayer is too
            // empty
            if (layer.Rsi?.Path.CanonPath != "/Textures/Effects/speech.rsi")
                args.Sprite.LayerSetSprite(i, spriteSpecifier);
            i += 1;
        }
    }
    private bool GetEntitySprite(EntityUid target, [NotNullWhen(true)] out ResPath? path, [NotNullWhen(true)] out string? state)
    {
        path = null;
        state = null;
        if (!TryComp(target, out SpriteComponent? sprite) || !sprite.Visible)
            return false;
        if (sprite.BaseRSI == null)
            return false;
        path = sprite.BaseRSI.Path;
        var rsiState = sprite.LayerGetState(0);
        if (rsiState == null)
            return false;
        if (rsiState.Name == null)
            return false;
        state = rsiState.Name;
        return true;
    }
    private void AddMorphVerb(Entity<MorphComponent> ent, ref GetVerbsEvent<InnateVerb> args)
    {
        var user = args.User;
        var target = args.Target;
        ResPath? path = new ResPath("/Textures/_c4llv07e/morph.rsi");
        string? state = "morph";
        if (target != user && !GetEntitySprite(target, out path, out state))
            return;

        InnateVerb verb = new()
        {
            Act = () =>
            {
                var ev = new MorphEvent(GetNetEntity(user), path.Value, state);
                RaiseNetworkEvent(ev);
            },
            Text = "Замоскироваться под предмет",
            Icon = new SpriteSpecifier.Rsi(path.Value, state),
            Priority = 2
        };
        args.Verbs.Add(verb);
    }
}
