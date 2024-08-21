using Content.Shared.Destructible;
using Content.Shared.Mobs;
using Content.Shared.AdventureSpace.Roles.StationAI.Components;

namespace Content.Shared.AdventureSpace.Roles.StationAI;

public abstract class SharedStationAISystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationAICarrierComponent, MoveEvent>(OnCarrierMoved);
        SubscribeLocalEvent<StationAICarrierComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<StationAICarrierComponent, DestructionEventArgs>(OnBeforeAICarrierDestroyed);
    }

    private void OnMobStateChanged(Entity<StationAICarrierComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        if (GetCarrierGhost(ent) is not { } aiGhostEnt)
            return;

        DropCamera(aiGhostEnt);
    }

    private void OnBeforeAICarrierDestroyed(Entity<StationAICarrierComponent> ent, ref DestructionEventArgs args)
    {
        if (GetCarrierGhost(ent) is not { } aiGhostEnt)
            return;

        DropCamera(aiGhostEnt);
    }

    private void OnCarrierMoved(Entity<StationAICarrierComponent> ent, ref MoveEvent args)
    {
        if (GetCarrierGhost(ent) is not { } aiGhostEnt)
            return;

        _transformSystem.SetCoordinates(aiGhostEnt, Transform(ent).Coordinates);
    }

    private Entity<StationAIGhostComponent>? GetCarrierGhost(Entity<StationAICarrierComponent> ent)
    {
        if (ent.Comp.AIGhostEntity is not { } aiGhost)
            return null;

        var aiGhostEnt = GetEntity(aiGhost);
        if (!TryComp<StationAIGhostComponent>(aiGhostEnt, out var aiGhostComponent))
            return null;

        return (aiGhostEnt, aiGhostComponent);
    }

    protected void AttachAICamera(Entity<StationAIGhostComponent> ent, EntityUid target)
    {
        var airCarrier = EnsureComp<StationAICarrierComponent>(target);
        airCarrier.AIGhostEntity = GetNetEntity(ent);

        ent.Comp.ActiveCamera = target;
        _transformSystem.SetCoordinates(ent, Transform(target).Coordinates);
        Dirty(target, airCarrier);
    }

    protected virtual void DropCamera(Entity<StationAIGhostComponent> ent)
    {
        ClearAICarrier(ent);
        BackGhostToCore(ent);
    }

    protected void BackGhostToCore(Entity<StationAIGhostComponent> ent)
    {
        if (ent.Comp.CoreUid == EntityUid.Invalid)
            return;

        _transformSystem.SetCoordinates(ent, Transform(ent.Comp.CoreUid).Coordinates);
        _transformSystem.SetParent(ent, ent.Comp.CoreUid);
    }

    private void ClearAICarrier(Entity<StationAIGhostComponent> ent)
    {
        var camera = ent.Comp.ActiveCamera;
        if (camera == EntityUid.Invalid)
            return;

        if (ent.Comp.ActiveCamera == EntityUid.Invalid)
            return;

        if (!TryComp<StationAICarrierComponent>(camera, out var aiCarrier))
            return;

        aiCarrier.AIGhostEntity = null;
        ent.Comp.ActiveCamera = EntityUid.Invalid;

        RemComp<StationAICarrierComponent>(camera);
    }

    protected void OnSharedCameraDeactivated(Entity<StationAICarrierComponent> ent)
    {
        if (GetCarrierGhost(ent) is not { } aiGhostEnt)
            return;

        DropCamera(aiGhostEnt);
    }
}
