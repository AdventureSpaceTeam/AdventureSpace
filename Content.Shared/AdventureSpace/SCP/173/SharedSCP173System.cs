using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Storage.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.AdventureSpace.SCP._173;

public abstract class SharedSCP173System : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SCP173FreezeComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<SCP173FreezeComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnGetState(EntityUid uid, SCP173FreezeComponent freezeComponent, ref ComponentGetState args)
    {
        args.State = new SCP173FreezeComponentState(freezeComponent.Enabled, freezeComponent.LookedAt);
    }

    private void OnHandleState(EntityUid uid, SCP173FreezeComponent freezeComponent, ref ComponentHandleState args)
    {
        if (args.Current is not SCP173FreezeComponentState state)
            return;
        if (freezeComponent.LookedAt != state.LookedAt)
        {
            var ev = new OnLookStateChangedEvent(state.LookedAt);
            RaiseLocalEvent(ev);
        }

        freezeComponent.Enabled = state.Enabled;
        freezeComponent.LookedAt = state.LookedAt;
    }

    protected bool CanAttack(EntityUid uid, EntityUid trg, SCP173FreezeComponent freezeComponent)
    {
        if (HasComp<InsideEntityStorageComponent>(uid))
            return true;

        return !freezeComponent.LookedAt && trg.IsValid() && trg != uid && HasComp<MobStateComponent>(trg) && !_mobState.IsDead(trg);
    }
}

public sealed class OnLookStateChangedEvent : EntityEventArgs
{
    public bool IsLookedAt;

    public OnLookStateChangedEvent(bool isLookedAt)
    {
        IsLookedAt = isLookedAt;
    }
}
