using Content.Shared.Actions;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Hands.Components;
using Content.Shared._c4llv07e.AI;

namespace Content.Server._c4llv07e.AI;

public sealed class AISystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<AIComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<AIComponent, ComponentShutdown>(OnShutdown);
    }

    public void OnStartup(Entity<AIComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<HandsComponent>(ent.Owner, out var hands))
            return;
        _hands.RemoveHands(ent.Owner, hands);

        int handIndex = 0;
        foreach (var itemProto in ent.Comp.ItemsInHands)
        {
            EntityUid item;
            item = Spawn(itemProto);
            var handId = $"{ent.Owner}-hand-item{handIndex}";
            handIndex += 1;
            _hands.AddHand(ent.Owner, handId, HandLocation.Middle, hands);
            _hands.DoPickup(ent.Owner, hands.Hands[handId], item, hands);
            EnsureComp<UnremoveableComponent>(item);
            EnsureComp<AIControlledComponent>(item);
        }
        _actions.AddAction(ent, "ActionToggleLighting");
    }

    public void OnShutdown(Entity<AIComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<HandsComponent>(ent.Owner, out var hands))
            return;
        _hands.RemoveHands(ent.Owner, hands);
    }
}
