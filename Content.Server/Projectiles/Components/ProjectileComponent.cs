using System.Collections.Generic;
using Content.Shared.Damage;
using Content.Shared.Projectiles;
using Content.Shared.Sound;
using Robust.Shared.GameObjects;
using Robust.Shared.Players;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Projectiles.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedProjectileComponent))]
    public class ProjectileComponent : SharedProjectileComponent
    {
        // TODO PROTOTYPE Replace this datafield variable with prototype references, once they are supported.
        // This also requires changing the dictionary type and modifying ProjectileSystem.cs, which uses it.
        // While thats being done, also replace "damages" -> "damageTypes" For consistency.
        [DataField("damages")]
        [ViewVariables(VVAccess.ReadWrite)]
        public Dictionary<string, int> Damages { get; set; } = new();

        [DataField("deleteOnCollide")]
        public bool DeleteOnCollide { get; } = true;

        // Get that juicy FPS hit sound
        [DataField("soundHit", required: true)] public SoundSpecifier? SoundHit = default!;
        [DataField("soundHitSpecies")] public SoundSpecifier? SoundHitSpecies = null;

        public bool DamagedEntity;

        public float TimeLeft { get; set; } = 10;

        /// <summary>
        /// Function that makes the collision of this object ignore a specific entity so we don't collide with ourselves
        /// </summary>
        /// <param name="shooter"></param>
        public void IgnoreEntity(IEntity shooter)
        {
            Shooter = shooter.Uid;
            Dirty();
        }

        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new ProjectileComponentState(Shooter, IgnoreShooter);
        }
    }
}
