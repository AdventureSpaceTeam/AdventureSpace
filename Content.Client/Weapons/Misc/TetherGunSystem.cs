using Content.Shared.Weapons.Misc;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Client.Weapons.Misc;

public sealed class TetherGunSystem : SharedTetherGunSystem
{
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TetheredComponent, ComponentStartup>(OnTetheredStartup);
        SubscribeLocalEvent<TetheredComponent, ComponentShutdown>(OnTetheredShutdown);
        _overlay.AddOverlay(new TetherGunOverlay(EntityManager));
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlay.RemoveOverlay<TetherGunOverlay>();
    }

    protected override bool CanTether(EntityUid uid, TetherGunComponent component, EntityUid target, EntityUid? user)
    {
        // Need powercells predicted sadly :<
        return false;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        var player = _player.LocalPlayer?.ControlledEntity;

        if (player == null ||
            !TryGetTetherGun(player.Value, out var gunUid, out var gun) ||
            gun.TetherEntity == null)
        {
            return;
        }

        var mousePos = _input.MouseScreenPosition;
        var mouseWorldPos = _eyeManager.ScreenToMap(mousePos);

        if (mouseWorldPos.MapId == MapId.Nullspace)
            return;

        EntityCoordinates coords;

        if (_mapManager.TryFindGridAt(mouseWorldPos, out var grid))
        {
            coords = EntityCoordinates.FromMap(grid.Owner, mouseWorldPos, TransformSystem);
        }
        else
        {
            coords = EntityCoordinates.FromMap(_mapManager.GetMapEntityId(mouseWorldPos.MapId), mouseWorldPos, TransformSystem);
        }

        const float BufferDistance = 0.1f;

        if (TryComp<TransformComponent>(gun.TetherEntity, out var tetherXform) &&
            tetherXform.Coordinates.TryDistance(EntityManager, TransformSystem, coords, out var distance) &&
            distance < BufferDistance)
        {
            return;
        }

        RaisePredictiveEvent(new RequestTetherMoveEvent()
        {
            Coordinates = coords
        });
    }

    private void OnTetheredStartup(EntityUid uid, TetheredComponent component, ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        sprite.Color = Color.Orange;
    }

    private void OnTetheredShutdown(EntityUid uid, TetheredComponent component, ComponentShutdown args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        sprite.Color = Color.White;
    }
}
