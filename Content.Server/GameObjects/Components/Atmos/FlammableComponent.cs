﻿using System;
using System.Collections.Generic;
using Content.Server.Atmos;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.Components.Temperature;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Alert;
using Content.Shared.Atmos;
using Content.Shared.Chemistry;
using Content.Shared.Damage;
using Content.Shared.GameObjects.Components.Atmos;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Components.Timers;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.Timers;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Atmos
{
    [RegisterComponent]
    public class FlammableComponent : SharedFlammableComponent, ICollideBehavior, IFireAct, IReagentReaction
    {
        private bool _resisting = false;
        private readonly List<EntityUid> _collided = new();

        [ViewVariables(VVAccess.ReadWrite)]
        public bool OnFire { get; private set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public float FireStacks { get; private set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public bool FireSpread { get; private set; } = false;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool CanResistFire { get; private set; } = false;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(this, x => x.FireSpread, "fireSpread", false);
            serializer.DataField(this, x => x.CanResistFire, "canResistFire", false);
        }

        public void Ignite()
        {
            if (FireStacks > 0 && !OnFire)
            {
                OnFire = true;

            }

            UpdateAppearance();
        }

        public void Extinguish()
        {
            if (!OnFire) return;
            OnFire = false;
            FireStacks = 0;

            _collided.Clear();

            UpdateAppearance();
        }

        public void AdjustFireStacks(float relativeFireStacks)
        {
            FireStacks = MathF.Max(MathF.Min(-10f, FireStacks + relativeFireStacks), 20f);
            if (OnFire && FireStacks <= 0)
                Extinguish();

            UpdateAppearance();
        }

        public void Update(TileAtmosphere tile)
        {
            // Slowly dry ourselves off if wet.
            if (FireStacks < 0)
            {
                FireStacks = MathF.Min(0, FireStacks + 1);
            }

            Owner.TryGetComponent(out ServerAlertsComponent status);

            if (!OnFire)
            {
                status?.ClearAlert(AlertType.Fire);
                return;
            }

            status?.ShowAlert(AlertType.Fire);

            if (FireStacks > 0)
            {
                if(Owner.TryGetComponent(out TemperatureComponent temp))
                {
                    temp.ReceiveHeat(200 * FireStacks);
                }

                if (Owner.TryGetComponent(out IDamageableComponent damageable))
                {
                    // TODO ATMOS Fire resistance from armor
                    var damage = Math.Min((int) (FireStacks * 2.5f), 10);
                    damageable.ChangeDamage(DamageClass.Burn, damage, false);
                }

                AdjustFireStacks(-0.1f * (_resisting ? 10f : 1f));
            }
            else
            {
                Extinguish();
                return;
            }

            // If we're in an oxygenless environment, put the fire out.
            if (tile?.Air?.GetMoles(Gas.Oxygen) < 1f)
            {
                Extinguish();
                return;
            }

            tile.HotspotExpose(700, 50, true);

            foreach (var uid in _collided.ToArray())
            {
                if (!uid.IsValid() || !Owner.EntityManager.EntityExists(uid))
                {
                    _collided.Remove(uid);
                    continue;
                }

                var entity = Owner.EntityManager.GetEntity(uid);
                var physics = Owner.GetComponent<IPhysicsComponent>();
                var otherPhysics = entity.GetComponent<IPhysicsComponent>();

                if (!physics.WorldAABB.Intersects(otherPhysics.WorldAABB))
                {
                    _collided.Remove(uid);
                }
            }
        }

        public void CollideWith(IEntity collidedWith)
        {
            if (!collidedWith.TryGetComponent(out FlammableComponent otherFlammable))
                return;

            if (!FireSpread || !otherFlammable.FireSpread)
                return;

            if (OnFire)
            {
                if (otherFlammable.OnFire)
                {
                    var fireSplit = (FireStacks + otherFlammable.FireStacks) / 2;
                    FireStacks = fireSplit;
                    otherFlammable.FireStacks = fireSplit;
                }
                else
                {
                    FireStacks /= 2;
                    otherFlammable.FireStacks += FireStacks;
                    otherFlammable.Ignite();
                }
            } else if (otherFlammable.OnFire)
            {
                otherFlammable.FireStacks /= 2;
                FireStacks += otherFlammable.FireStacks;
                Ignite();
            }
        }

        private void UpdateAppearance()
        {
            if (Owner.Deleted || !Owner.TryGetComponent(out AppearanceComponent appearanceComponent)) return;
            appearanceComponent.SetData(FireVisuals.OnFire, OnFire);
            appearanceComponent.SetData(FireVisuals.FireStacks, FireStacks);
        }

        public void FireAct(float temperature, float volume)
        {
            AdjustFireStacks(3);
            Ignite();
        }

        // This needs some improvements...
        public void Resist()
        {
            if (!OnFire || !ActionBlockerSystem.CanInteract(Owner) || _resisting || !Owner.TryGetComponent(out StunnableComponent stunnable)) return;

            _resisting = true;

            Owner.PopupMessage(Loc.GetString("You stop, drop, and roll!"));
            stunnable.Paralyze(2f);

            Owner.SpawnTimer(2000, () =>
            {
                _resisting = false;
                FireStacks -= 2f;
                UpdateAppearance();
            });
        }

        public ReagentUnit ReagentReactTouch(ReagentPrototype reagent, ReagentUnit volume)
        {
            switch (reagent.ID)
            {
                case "chem.H2O":
                    Extinguish();
                    AdjustFireStacks(-1.5f);
                    return ReagentUnit.Zero;

                case "chem.WeldingFuel":
                case "chem.Thermite":
                case "chem.Phoron":
                case "chem.Ethanol":
                    AdjustFireStacks(volume.Float() / 10f);
                    return volume;

                default:
                    return ReagentUnit.Zero;
            }
        }
    }
}
