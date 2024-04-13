using Content.Shared.Actions;
using Content.Shared.Eye;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind.Components;
using Content.Shared.Roles.Jobs;
using Content.Shared.Roles;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Content.Shared._c4llv07e.AI;

namespace Content.Server._c4llv07e.AI;

public sealed class AISystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly VisibilitySystem _visibility = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<AIComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<AIComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<AIComponent, MindAddedMessage>(OnMindAdded);
    }

    private void OnStartup(Entity<AIComponent> ent, ref ComponentStartup args)
    {
        var hands = EnsureComp<HandsComponent>(ent.Owner);
        var visibility = EnsureComp<VisibilityComponent>(ent.Owner);
        _actions.AddAction(ent, "ActionToggleLighting");
        SetupHands((ent.Owner, ent.Comp, hands));
        SetupVisibility((ent.Owner, visibility));
    }

    private void SetupHands(Entity<AIComponent, HandsComponent> ent)
    {
        _hands.RemoveHands(ent.Owner, ent.Comp2);

        int handIndex = 0;
        foreach (var itemProto in ent.Comp1.ItemsInHands)
        {
            EntityUid item;
            item = Spawn(itemProto);
            var handId = $"{ent.Owner}-hand-item{handIndex}";
            handIndex += 1;
            _hands.AddHand(ent.Owner, handId, HandLocation.Middle, ent.Comp2);
            _hands.DoPickup(ent.Owner, ent.Comp2.Hands[handId], item, ent.Comp2);
            EnsureComp<UnremoveableComponent>(item);
            EnsureComp<AIControlledComponent>(item);
        }
    }

    private void SetupVisibility(Entity<VisibilityComponent?> ent)
    {
        _visibility.AddLayer(ent, (int) VisibilityFlags.Ghost, false);
        _visibility.AddLayer(ent, (int) VisibilityFlags.AI, false);
        _visibility.RemoveLayer(ent, (int) VisibilityFlags.Normal, true);
        _visibility.RefreshVisibility(ent.Owner, ent.Comp);
        if (!TryComp<EyeComponent>(ent, out var eye))
            return;
        _eye.SetVisibilityMask(ent.Owner, eye.VisibilityMask & ~(int) VisibilityFlags.Ghost | (int) VisibilityFlags.AI);
    }

    private void OnShutdown(Entity<AIComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<HandsComponent>(ent.Owner, out var hands))
            return;
        _hands.RemoveHands(ent.Owner, hands);
    }

    private void OnMindAdded(Entity<AIComponent> ent, ref MindAddedMessage args)
    {
        if (!TryComp<JobComponent>(args.Mind.Owner, out var job))
            return;
        job.Prototype = new ProtoId<JobPrototype>("AI");
    }
}
