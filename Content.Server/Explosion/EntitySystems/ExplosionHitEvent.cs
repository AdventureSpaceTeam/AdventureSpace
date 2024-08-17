using Content.Shared.Damage;

namespace Content.Server.Explosion.EntitySystems;

[ByRefEvent]
public record struct ExplosionHitEvent(DamageSpecifier Damage);
