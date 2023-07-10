using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Client.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;

namespace Content.Client.Movement.Systems;

public sealed class JetpackSystem : SharedJetpackSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly ClothingSystem _clothing = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<JetpackComponent, AppearanceChangeEvent>(OnJetpackAppearance);
    }

    protected override bool CanEnable(JetpackComponent component)
    {
        // No predicted atmos so you'd have to do a lot of funny to get this working.
        return false;
    }

    private void OnJetpackAppearance(EntityUid uid, JetpackComponent component, ref AppearanceChangeEvent args)
    {
        _appearance.TryGetData<bool>(uid, JetpackVisuals.Enabled, out var enabled, args.Component);

        var state = "icon" + (enabled ? "-on" : "");
        args.Sprite?.LayerSetState(0, state);

        if (TryComp<ClothingComponent>(uid, out var clothing))
            _clothing.SetEquippedPrefix(uid, enabled ? "on" : null, clothing);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted) return;

        foreach (var comp in EntityQuery<ActiveJetpackComponent>())
        {
            if (_timing.CurTime < comp.TargetTime)
                continue;

            comp.TargetTime = _timing.CurTime + TimeSpan.FromSeconds(comp.EffectCooldown);

            CreateParticles(comp.Owner);
        }
    }

    private void CreateParticles(EntityUid uid)
    {
        // Don't show particles unless the user is moving.
        if (Container.TryGetContainingContainer(uid, out var container) &&
            TryComp<PhysicsComponent>(container.Owner, out var body) &&
            body.LinearVelocity.LengthSquared() < 1f)
            return;

        var uidXform = Transform(uid);
        var coordinates = uidXform.Coordinates;
        var gridUid = coordinates.GetGridUid(EntityManager);

        if (_mapManager.TryGetGrid(gridUid, out var grid))
        {
            coordinates = new EntityCoordinates(grid.Owner, grid.WorldToLocal(coordinates.ToMapPos(EntityManager)));
        }
        else if (uidXform.MapUid != null)
        {
            coordinates = new EntityCoordinates(uidXform.MapUid.Value, uidXform.WorldPosition);
        }
        else
        {
            return;
        }

        var ent = Spawn("JetpackEffect", coordinates);
        var xform = Transform(ent);
        xform.Coordinates = coordinates;
    }
}
