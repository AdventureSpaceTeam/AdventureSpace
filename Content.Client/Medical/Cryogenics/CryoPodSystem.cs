﻿using Content.Shared.Destructible;
using Content.Shared.Emag.Systems;
using Content.Shared.Medical.Cryogenics;
using Content.Shared.Verbs;
using Robust.Client.GameObjects;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client.Medical.Cryogenics;

public sealed class CryoPodSystem: SharedCryoPodSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CryoPodComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<CryoPodComponent, GetVerbsEvent<AlternativeVerb>>(AddAlternativeVerbs);
        SubscribeLocalEvent<CryoPodComponent, GotEmaggedEvent>(OnEmagged);
        SubscribeLocalEvent<CryoPodComponent, CryoPodPryFinished>(OnCryoPodPryFinished);
        SubscribeLocalEvent<CryoPodComponent, CryoPodPryInterrupted>(OnCryoPodPryInterrupted);

        SubscribeLocalEvent<CryoPodComponent, AppearanceChangeEvent>(OnAppearanceChange);
        SubscribeLocalEvent<InsideCryoPodComponent, ComponentStartup>(OnCryoPodInsertion);
        SubscribeLocalEvent<InsideCryoPodComponent, ComponentRemove>(OnCryoPodRemoval);
    }

    private void OnCryoPodInsertion(EntityUid uid, InsideCryoPodComponent component, ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(uid, out var spriteComponent))
        {
            return;
        }

        component.PreviousOffset = spriteComponent.Offset;
        spriteComponent.Offset = new Vector2(0, 1);
    }

    private void OnCryoPodRemoval(EntityUid uid, InsideCryoPodComponent component, ComponentRemove args)
    {
        if (!TryComp<SpriteComponent>(uid, out var spriteComponent))
        {
            return;
        }

        spriteComponent.Offset = component.PreviousOffset;
    }

    private void OnAppearanceChange(EntityUid uid, SharedCryoPodComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
        {
            return;
        }

        if (!_appearance.TryGetData<bool>(uid, SharedCryoPodComponent.CryoPodVisuals.ContainsEntity, out var isOpen, args.Component)
            || !_appearance.TryGetData<bool>(uid, SharedCryoPodComponent.CryoPodVisuals.IsOn, out var isOn, args.Component))
        {
            return;
        }

        if (isOpen)
        {
            args.Sprite.LayerSetState(CryoPodVisualLayers.Base, "pod-open");
            args.Sprite.LayerSetVisible(CryoPodVisualLayers.Cover, false);
            args.Sprite.DrawDepth = (int) DrawDepth.Objects;
        }
        else
        {
            args.Sprite.DrawDepth = (int) DrawDepth.Mobs;
            args.Sprite.LayerSetState(CryoPodVisualLayers.Base, isOn ? "pod-on" : "pod-off");
            args.Sprite.LayerSetState(CryoPodVisualLayers.Cover, isOn ? "cover-on" : "cover-off");
            args.Sprite.LayerSetVisible(CryoPodVisualLayers.Cover, true);
        }
    }
}

public enum CryoPodVisualLayers : byte
{
    Base,
    Cover,
}
