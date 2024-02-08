using Robust.Shared.GameStates;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Cards;

namespace Content.Shared.Cards;

public sealed class SharedFlipCardSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FlipCardComponent, ComponentGetState>(GetCompState);
        SubscribeLocalEvent<FlipCardComponent, ComponentHandleState>(HandleCompState);
        SubscribeLocalEvent<FlipCardComponent, ActivateInWorldEvent>(OnActivate);
    }

    private void GetCompState(Entity<FlipCardComponent> ent, ref ComponentGetState args)
    {
        args.State = new FlipCardComponentState
        {
            Flipped = ent.Comp.Flipped,
        };
    }

    private void HandleCompState(Entity<FlipCardComponent> ent, ref ComponentHandleState args)
    {
        if (args.Current is not FlipCardComponentState state)
            return;

        ent.Comp.Flipped = state.Flipped;
    }

    private void OnActivate(EntityUid uid, FlipCardComponent comp, ActivateInWorldEvent args)
    {
        comp.Flipped = !comp.Flipped;
        Dirty(uid, comp);
        if (TryComp<AppearanceComponent>(uid, out var appearance))
        {
            _appearanceSystem.SetData(uid, CardsVisual.Visual, comp.Flipped ? CardsVisual.Flipped : CardsVisual.Normal, appearance);
        }
    }
}
