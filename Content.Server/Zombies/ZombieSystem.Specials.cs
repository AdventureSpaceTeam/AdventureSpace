using System.Linq;
using Content.Server.Polymorph.Systems;
using Content.Shared.Damage;
using Content.Shared.Doors.Components;
using Content.Shared.GameTicking;
using Content.Shared.Mobs;
using Content.Shared.Polymorph;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Prototypes;

namespace Content.Server.Zombies;

public sealed partial class ZombieSystem
{
    [Dependency] private readonly PolymorphSystem _polymorphSystem = default!;

    [ValidatePrototypeId<PolymorphPrototype>]
    private const string Tank = "ZombieTankPolymorph";

    [ValidatePrototypeId<PolymorphPrototype>]
    private const string Hunter = "ZombieHunterReSpritePolymorph";

    [ValidatePrototypeId<PolymorphPrototype>]
    private const string Smoker = "ZombieSmokerPolymorph";

    private int _tanksCountPerRound;
    private int _huntersCountPerRound;
    private int _smokersCountPerRound;

    private void InitZombieTank()
    {
        SubscribeLocalEvent<ZombieTankComponent, MeleeHitEvent>(OnZombieTankMelee);
        SubscribeLocalEvent<RoundEndedEvent>(OnRoundEnded);
    }

    private void OnRoundEnded(RoundEndedEvent ev)
    {
        _tanksCountPerRound = 0;
        _huntersCountPerRound = 0;
        _smokersCountPerRound = 0;
    }

    private void OnZombieTankMelee(EntityUid uid, ZombieTankComponent component, MeleeHitEvent args)
    {
        if (!args.HitEntities.Any())
            return;

        foreach (var entity in args.HitEntities)
        {
            if (args.User == entity || !HasComp<DoorComponent>(entity))
                continue;

            args.BonusDamage = args.BaseDamage * 2;
        }
    }

    private bool PolymorphToTank(EntityUid target)
    {
        if (_tanksCountPerRound == 1 || !_mind.TryGetMind(target, out _, out _))
            return false;

        PrepareSpecial(target);
        _polymorphSystem.PolymorphEntity(target, Tank);
        _tanksCountPerRound++;
        return true;
    }

    private bool PolymorphToHunter(EntityUid target)
    {
        if (_huntersCountPerRound == 2 || !_mind.TryGetMind(target, out _, out _))
            return false;

        PrepareSpecial(target);
        _polymorphSystem.PolymorphEntity(target, Hunter);
        _huntersCountPerRound++;

        return true;
    }

    private bool PolymorphToSmoker(EntityUid target)
    {
        if (_smokersCountPerRound == 2 || !_mind.TryGetMind(target, out _, out _))
            return false;

        PrepareSpecial(target);
        _polymorphSystem.PolymorphEntity(target, Smoker);
        _smokersCountPerRound++;

        return true;
    }

    private void PrepareSpecial(EntityUid target)
    {
        if (TryComp<DamageableComponent>(target, out var damageablecomp))
            _damageable.SetAllDamage(target, damageablecomp, 0);

        _mobState.ChangeMobState(target, MobState.Alive);
    }
}
