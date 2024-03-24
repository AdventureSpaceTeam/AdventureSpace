using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Hands.Components;
using Content.Shared._c4llv07e.AI;

namespace Content.Shared._c4llv07e.AI;

public sealed class SharedAISystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<AIComponent, InteractionAttemptEvent>(OnInteract);
    }

    public void OnInteract(Entity<AIComponent> ent, ref InteractionAttemptEvent args)
    {
        if (!HasComp<AIControlledComponent>(args.Target) && args.Target != null)
            args.Cancel();
    }
}
