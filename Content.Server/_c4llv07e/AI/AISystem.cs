using Content.Shared.Actions;
using Content.Shared.Eye;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind.Components;
using Content.Shared.Popups;
using Content.Shared.Roles.Jobs;
using Content.Shared.Roles;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Content.Shared._c4llv07e.AI;

namespace Content.Server._c4llv07e.AI;

public sealed class AISystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly VisibilitySystem _visibility = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<AIComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<AIComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<AIComponent, AIToggleVisibilityActionEvent>(OnToggleVisibility);
    }

    private void SetupHands(Entity<AIComponent> ent)
    {
        var hands = EnsureComp<HandsComponent>(ent);
        _hands.RemoveHands(ent, hands);
        int handIndex = 0;
        foreach (var itemProto in ent.Comp.ItemsInHands)
        {
            EntityUid item;
            item = Spawn(itemProto);
            var handId = $"{ent.Owner}-hand-item{handIndex}";
            handIndex += 1;
            _hands.AddHand(ent, handId, HandLocation.Middle, hands);
            _hands.DoPickup(ent, hands.Hands[handId], item, hands);
            EnsureComp<UnremoveableComponent>(item);
            EnsureComp<AIControlledComponent>(item);
        }
    }

    private void SetupVisibility(Entity<AIComponent, VisibilityComponent?> ent)
    {
        UpdateVisibility(ent);
    }

    private void OnMindAdded(Entity<AIComponent> ent, ref MindAddedMessage args)
    {
        if (!TryComp<JobComponent>(args.Mind.Owner, out var job))
            return;
        job.Prototype = new ProtoId<JobPrototype>("AI");
    }

    private void UpdateVisibility(Entity<AIComponent, VisibilityComponent?> ent)
    {
        var entity = new Entity<VisibilityComponent?>(ent.Owner, ent.Comp2);
        if (TryComp<EyeComponent>(ent, out var eye))
            _eye.SetVisibilityMask(ent, eye.VisibilityMask | (int) VisibilityFlags.AI, eye);
        if (ent.Comp1.IsVisible)
        {
            _visibility.AddLayer(entity, (int) VisibilityFlags.Normal, false);
            _visibility.RemoveLayer(entity, (int) VisibilityFlags.AI, false);
        }
        else
        {
            _visibility.RemoveLayer(entity, (int) VisibilityFlags.Normal, false);
            _visibility.AddLayer(entity, (int) VisibilityFlags.AI, false);
        }
        _visibility.RefreshVisibility(entity, ent.Comp2);
        _appearance.SetData(ent, AIVisuals.Visibility, ent.Comp1.IsVisible);
    }

    private void OnStartup(Entity<AIComponent> ent, ref ComponentStartup args)
    {
        var visibility = EnsureComp<VisibilityComponent>(ent.Owner);
        _actions.AddAction(ent, "ActionToggleLighting");
        _actions.AddAction(ent, "ActionAIToggleVisibility");
        SetupHands(ent);
        SetupVisibility((ent.Owner, ent.Comp, visibility));
    }

    private void OnToggleVisibility(Entity<AIComponent> ent, ref AIToggleVisibilityActionEvent args)
    {
        ent.Comp.IsVisible = !ent.Comp.IsVisible;
        var message = "Голопроекторы выключены";
        if (ent.Comp.IsVisible)
            message = "Голопроекторы включены";
        _popup.PopupEntity(message, ent, ent, PopupType.Medium);
        UpdateVisibility(ent);
    }
}
