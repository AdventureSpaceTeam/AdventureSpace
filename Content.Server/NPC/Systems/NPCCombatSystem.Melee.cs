using Content.Server.CombatMode;
using Content.Server.NPC.Components;
using Content.Shared.MobState;
using Content.Shared.MobState.Components;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;

namespace Content.Server.NPC.Systems;

public sealed partial class NPCCombatSystem
{
    private const float TargetMeleeLostRange = 14f;

    private void InitializeMelee()
    {
        SubscribeLocalEvent<NPCMeleeCombatComponent, ComponentStartup>(OnMeleeStartup);
        SubscribeLocalEvent<NPCMeleeCombatComponent, ComponentShutdown>(OnMeleeShutdown);
    }

    private void OnMeleeShutdown(EntityUid uid, NPCMeleeCombatComponent component, ComponentShutdown args)
    {
        if (TryComp<CombatModeComponent>(uid, out var combatMode))
        {
            combatMode.IsInCombatMode = false;
        }

        _steering.Unregister(component.Owner);
    }

    private void OnMeleeStartup(EntityUid uid, NPCMeleeCombatComponent component, ComponentStartup args)
    {
        if (TryComp<CombatModeComponent>(uid, out var combatMode))
        {
            combatMode.IsInCombatMode = true;
        }

        // TODO: Cleanup later, just looking for parity for now.
        component.Weapon = uid;
    }

    private void UpdateMelee(float frameTime)
    {
        var combatQuery = GetEntityQuery<CombatModeComponent>();
        var xformQuery = GetEntityQuery<TransformComponent>();
        var physicsQuery = GetEntityQuery<PhysicsComponent>();
        var curTime = _timing.CurTime;

        foreach (var (comp, _) in EntityQuery<NPCMeleeCombatComponent, ActiveNPCComponent>())
        {
            if (!combatQuery.TryGetComponent(comp.Owner, out var combat) || !combat.IsInCombatMode)
            {
                RemComp<NPCMeleeCombatComponent>(comp.Owner);
                continue;
            }

            Attack(comp, curTime, physicsQuery, xformQuery);
        }
    }

    private void Attack(NPCMeleeCombatComponent component, TimeSpan curTime, EntityQuery<PhysicsComponent> physicsQuery, EntityQuery<TransformComponent> xformQuery)
    {
        component.Status = CombatStatus.Normal;

        if (!TryComp<MeleeWeaponComponent>(component.Weapon, out var weapon))
        {
            component.Status = CombatStatus.NoWeapon;
            return;
        }

        if (!xformQuery.TryGetComponent(component.Owner, out var xform) ||
            !xformQuery.TryGetComponent(component.Target, out var targetXform))
        {
            component.Status = CombatStatus.TargetUnreachable;
            return;
        }

        if (!xform.Coordinates.TryDistance(EntityManager, targetXform.Coordinates, out var distance))
        {
            component.Status = CombatStatus.TargetUnreachable;
            return;
        }

        if (distance > TargetMeleeLostRange)
        {
            component.Status = CombatStatus.TargetUnreachable;
            return;
        }

        if (TryComp<NPCSteeringComponent>(component.Owner, out var steering) &&
            steering.Status == SteeringStatus.NoPath)
        {
            component.Status = CombatStatus.TargetUnreachable;
            return;
        }

        if (distance > weapon.Range)
        {
            component.Status = CombatStatus.TargetOutOfRange;
            return;
        }

        steering = EnsureComp<NPCSteeringComponent>(component.Owner);
        steering.Range = MathF.Max(0.2f, weapon.Range - 0.4f);

        // Gets unregistered on component shutdown.
        _steering.TryRegister(component.Owner, new EntityCoordinates(component.Target, Vector2.Zero), steering);

        if (weapon.NextAttack > curTime)
            return;

        if (_random.Prob(component.MissChance) &&
            physicsQuery.TryGetComponent(component.Target, out var targetPhysics) &&
            targetPhysics.LinearVelocity.LengthSquared != 0f)
        {
            _melee.AttemptLightAttackMiss(component.Owner, weapon, targetXform.Coordinates.Offset(_random.NextVector2(0.5f)));
        }
        else
        {
            _melee.AttemptLightAttack(component.Owner, weapon, component.Target);
        }
    }
}
