using System.Diagnostics.CodeAnalysis;
using Content.Shared.Audio;
using Content.Shared.Hands.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Physics.Components;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;

namespace Content.Shared.Weapons.Reflect;

/// <summary>
/// This handles reflecting projectiles and hitscan shots.
/// </summary>
public abstract class SharedReflectSystem : EntitySystem
{
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HandsComponent, ProjectileReflectAttemptEvent>(OnHandReflectProjectile);
        SubscribeLocalEvent<HandsComponent, HitScanReflectAttemptEvent>(OnHandsReflectHitscan);

        SubscribeLocalEvent<ReflectComponent, ProjectileCollideEvent>(OnReflectCollide);
        SubscribeLocalEvent<ReflectComponent, HitScanReflectAttemptEvent>(OnReflectHitscan);
    }

    private void OnReflectCollide(EntityUid uid, ReflectComponent component, ref ProjectileCollideEvent args)
    {
        if (args.Cancelled)
        {
            return;
        }

        if (TryReflectProjectile(uid, args.OtherEntity, reflect: component))
            args.Cancelled = true;
    }

    private void OnHandReflectProjectile(EntityUid uid, HandsComponent hands, ref ProjectileReflectAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (hands.ActiveHandEntity != null && TryReflectProjectile(hands.ActiveHandEntity.Value, args.ProjUid))
            args.Cancelled = true;
    }

    private bool TryReflectProjectile(EntityUid reflector, EntityUid projectile, ProjectileComponent? projectileComp = null, ReflectComponent? reflect = null)
    {
        if (!Resolve(reflector, ref reflect, false) ||
            !reflect.Enabled ||
            !TryComp<ReflectiveComponent>(projectile, out var reflective) ||
            (reflect.Reflects & reflective.Reflective) == 0x0 ||
            !_random.Prob(reflect.ReflectProb) ||
            !TryComp<PhysicsComponent>(projectile, out var physics))
        {
            return false;
        }

        var rotation = _random.NextAngle(-reflect.Spread / 2, reflect.Spread / 2).Opposite();
        var existingVelocity = _physics.GetMapLinearVelocity(projectile, component: physics);
        var relativeVelocity = existingVelocity - _physics.GetMapLinearVelocity(reflector);
        var newVelocity = rotation.RotateVec(relativeVelocity);

        // Have the velocity in world terms above so need to convert it back to local.
        var difference = newVelocity - existingVelocity;

        _physics.SetLinearVelocity(projectile, physics.LinearVelocity + difference, body: physics);

        var locRot = Transform(projectile).LocalRotation;
        var newRot = rotation.RotateVec(locRot.ToVec());
        _transform.SetLocalRotation(projectile, newRot.ToAngle());

        if (_netManager.IsServer)
        {
            _popup.PopupEntity(Loc.GetString("reflect-shot"), reflector);
            _audio.PlayPvs(reflect.SoundOnReflect, reflector, AudioHelpers.WithVariation(0.05f, _random));
        }

        if (Resolve(projectile, ref projectileComp, false))
        {
            projectileComp.Shooter = reflector;
            projectileComp.Weapon = reflector;
            Dirty(projectileComp);
        }

        return true;
    }

    private void OnHandsReflectHitscan(EntityUid uid, HandsComponent hands, ref HitScanReflectAttemptEvent args)
    {
        if (args.Reflected || hands.ActiveHandEntity == null)
            return;

        if (TryReflectHitscan(hands.ActiveHandEntity.Value, args.Direction, out var dir))
        {
            args.Direction = dir.Value;
            args.Reflected = true;
        }
    }

    private void OnReflectHitscan(EntityUid uid, ReflectComponent component, ref HitScanReflectAttemptEvent args)
    {
        if (args.Reflected ||
            (component.Reflects & args.Reflective) == 0x0)
        {
            return;
        }

        if (TryReflectHitscan(uid, args.Direction, out var dir))
        {
            args.Direction = dir.Value;
            args.Reflected = true;
        }
    }

    private bool TryReflectHitscan(EntityUid reflector, Vector2 direction,
        [NotNullWhen(true)] out Vector2? newDirection)
    {
        if (!TryComp<ReflectComponent>(reflector, out var reflect) ||
            !reflect.Enabled ||
            !_random.Prob(reflect.ReflectProb))
        {
            newDirection = null;
            return false;
        }

        if (_netManager.IsServer)
        {
            _popup.PopupEntity(Loc.GetString("reflect-shot"), reflector);
            _audio.PlayPvs(reflect.SoundOnReflect, reflector, AudioHelpers.WithVariation(0.05f, _random));
        }

        var spread = _random.NextAngle(-reflect.Spread / 2, reflect.Spread / 2);
        newDirection = -spread.RotateVec(direction);
        return true;
    }
}
