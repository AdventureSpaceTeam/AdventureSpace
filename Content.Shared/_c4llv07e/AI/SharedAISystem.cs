using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Storage.Components;
using Content.Shared._c4llv07e.AI;

namespace Content.Shared._c4llv07e.AI;

public sealed class SharedAISystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<AIComponent, InteractionAttemptEvent>(OnInteract);
        SubscribeLocalEvent<AIComponent, InsertIntoEntityStorageAttemptEvent>(OnEntityStorageInsertAttempt);
        SubscribeLocalEvent<AIComponent, ComponentShutdown>(OnShutdown);
    }

    public void OnInteract(Entity<AIComponent> ent, ref InteractionAttemptEvent args)
    {
        if (!HasComp<AIControlledComponent>(args.Target) && args.Target != null && args.Target != args.Uid)
            args.Cancelled = true;
    }

    private void OnEntityStorageInsertAttempt(Entity<AIComponent> ent, ref InsertIntoEntityStorageAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnShutdown(Entity<AIComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<HandsComponent>(ent, out var hands))
            return;
        _hands.RemoveHands(ent, hands);
    }
}
