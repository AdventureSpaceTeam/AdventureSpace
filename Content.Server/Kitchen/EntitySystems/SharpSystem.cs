﻿using Content.Server.DoAfter;
using Content.Server.Kitchen.Components;
using Content.Server.Popups;
using Content.Shared.Body.Components;
using Content.Shared.Interaction;
using Content.Shared.MobState.Components;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Player;

namespace Content.Server.Kitchen.EntitySystems;

public sealed class SharpSystem : EntitySystem
{
    [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SharpComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<SharpButcherDoafterComplete>(OnDoafterComplete);
        SubscribeLocalEvent<SharpButcherDoafterCancelled>(OnDoafterCancelled);

        SubscribeLocalEvent<SharedButcherableComponent, GetVerbsEvent<InteractionVerb>>(OnGetInteractionVerbs);
    }

    private void OnAfterInteract(EntityUid uid, SharpComponent component, AfterInteractEvent args)
    {
        if (args.Target is null || !args.CanReach)
            return;

        TryStartButcherDoafter(uid, args.Target.Value, args.User);
    }

    private void TryStartButcherDoafter(EntityUid knife, EntityUid target, EntityUid user)
    {
        if (!TryComp<SharedButcherableComponent>(target, out var butcher))
            return;

        if (!TryComp<SharpComponent>(knife, out var sharp))
            return;

        if (butcher.Type != ButcheringType.Knife)
            return;

        if (TryComp<MobStateComponent>(target, out var mobState) && !mobState.IsDead())
            return;

        if (!sharp.Butchering.Add(target))
            return;

        var doAfter =
            new DoAfterEventArgs(user, sharp.ButcherDelayModifier * butcher.ButcherDelay, default, target)
            {
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                BreakOnDamage = true,
                BreakOnStun = true,
                NeedHand = true,
                BroadcastFinishedEvent = new SharpButcherDoafterComplete { User = user, Entity = target, Sharp = knife },
                BroadcastCancelledEvent = new SharpButcherDoafterCancelled { Entity = target, Sharp = knife }
            };

        _doAfterSystem.DoAfter(doAfter);
    }

    private void OnDoafterComplete(SharpButcherDoafterComplete ev)
    {
        if (!TryComp<SharedButcherableComponent>(ev.Entity, out var butcher))
            return;

        if (!TryComp<SharpComponent>(ev.Sharp, out var sharp))
            return;

        sharp.Butchering.Remove(ev.Entity);

        EntityUid popupEnt = default;
        for (int i = 0; i < butcher.Pieces; i++)
        {
            popupEnt = Spawn(butcher.SpawnedPrototype, Transform(ev.Entity).Coordinates);
        }

        _popupSystem.PopupEntity(Loc.GetString("butcherable-knife-butchered-success", ("target", ev.Entity), ("knife", ev.Sharp)),
            popupEnt, Filter.Entities(ev.User));

        if (TryComp<SharedBodyComponent>(ev.Entity, out var body))
        {
            body.Gib();
        }
        else
        {
            QueueDel(ev.Entity);
        }
    }

    private void OnDoafterCancelled(SharpButcherDoafterCancelled ev)
    {
        if (!TryComp<SharpComponent>(ev.Sharp, out var sharp))
            return;

        sharp.Butchering.Remove(ev.Entity);
    }

    private void OnGetInteractionVerbs(EntityUid uid, SharedButcherableComponent component, GetVerbsEvent<InteractionVerb> args)
    {
        if (component.Type != ButcheringType.Knife)
            return;

        bool disabled = false;
        string? message = null;

        if (TryComp<MobStateComponent>(uid, out var state) && !state.IsDead())
        {
            disabled = true;
            message = Loc.GetString("butcherable-mob-isnt-dead");
        }

        if (args.Using is null || !TryComp<SharpComponent>(args.Using, out var sharp))
        {
            disabled = true;
            message = Loc.GetString("butcherable-need-knife");
        }

        InteractionVerb verb = new()
        {
            Act = () =>
            {
                if (!disabled)
                    TryStartButcherDoafter(args.Using!.Value, args.Target, args.User);
            },
            Message = message,
            Disabled = disabled,
            IconTexture = "/Textures/Interface/VerbIcons/cutlery.svg.192dpi.png",
            Text = Loc.GetString("butcherable-verb-name"),
        };

        args.Verbs.Add(verb);
    }
}

public sealed class SharpButcherDoafterComplete : EntityEventArgs
{
    public EntityUid Entity;
    public EntityUid Sharp;
    public EntityUid User;
}

public sealed class SharpButcherDoafterCancelled : EntityEventArgs
{
    public EntityUid Entity;
    public EntityUid Sharp;
}
