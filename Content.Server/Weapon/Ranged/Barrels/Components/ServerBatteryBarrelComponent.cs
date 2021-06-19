using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Server.Battery.Components;
using Content.Server.Hands.Components;
using Content.Server.Items;
using Content.Server.Projectiles.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Damage;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.NetIDs;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Barrels.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Players;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Weapon.Ranged.Barrels.Components
{
    [RegisterComponent]
    public sealed class ServerBatteryBarrelComponent : ServerRangedBarrelComponent
    {
        public override string Name => "BatteryBarrel";
        public override uint? NetID => ContentNetIDs.BATTERY_BARREL;

        // The minimum change we need before we can fire
        [DataField("lowerChargeLimit")]
        [ViewVariables] private float _lowerChargeLimit = 10;
        [DataField("fireCost")]
        [ViewVariables] private int _baseFireCost = 300;
        // What gets fired
        [DataField("ammoPrototype")]
        [ViewVariables] private string? _ammoPrototype;

        [ViewVariables] public IEntity? PowerCellEntity => _powerCellContainer.ContainedEntity;
        public BatteryComponent? PowerCell => _powerCellContainer.ContainedEntity?.GetComponentOrNull<BatteryComponent>();
        private ContainerSlot _powerCellContainer = default!;
        private ContainerSlot _ammoContainer = default!;
        [DataField("powerCellPrototype")]
        private string? _powerCellPrototype = default;
        [DataField("powerCellRemovable")]
        [ViewVariables] private bool _powerCellRemovable = default;

        public override int ShotsLeft
        {
            get
            {
                var powerCell = _powerCellContainer.ContainedEntity;

                if (powerCell == null)
                {
                    return 0;
                }

                return (int) Math.Ceiling(powerCell.GetComponent<BatteryComponent>().CurrentCharge / _baseFireCost);
            }
        }

        public override int Capacity
        {
            get
            {
                var powerCell = _powerCellContainer.ContainedEntity;

                if (powerCell == null)
                {
                    return 0;
                }

                return (int) Math.Ceiling((float) (powerCell.GetComponent<BatteryComponent>().MaxCharge / _baseFireCost));
            }
        }

        private AppearanceComponent? _appearanceComponent;

        // Sounds
        [DataField("soundPowerCellInsert")]
        private string? _soundPowerCellInsert = default;
        [DataField("soundPowerCellEject")]
        private string? _soundPowerCellEject = default;

        public override ComponentState GetComponentState(ICommonSession player)
        {
            (int, int)? count = (ShotsLeft, Capacity);

            return new BatteryBarrelComponentState(
                FireRateSelector,
                count);
        }

        public override void Initialize()
        {
            base.Initialize();
            _powerCellContainer = ContainerHelpers.EnsureContainer<ContainerSlot>(Owner, $"{Name}-powercell-container", out var existing);
            if (!existing && _powerCellPrototype != null)
            {
                var powerCellEntity = Owner.EntityManager.SpawnEntity(_powerCellPrototype, Owner.Transform.Coordinates);
                _powerCellContainer.Insert(powerCellEntity);
            }

            if (_ammoPrototype != null)
            {
                _ammoContainer = ContainerHelpers.EnsureContainer<ContainerSlot>(Owner, $"{Name}-ammo-container");
            }

            if (Owner.TryGetComponent(out AppearanceComponent? appearanceComponent))
            {
                _appearanceComponent = appearanceComponent;
            }
            Dirty();
        }

        protected override void Startup()
        {
            UpdateAppearance();
        }

        public void UpdateAppearance()
        {
            _appearanceComponent?.SetData(MagazineBarrelVisuals.MagLoaded, _powerCellContainer.ContainedEntity != null);
            _appearanceComponent?.SetData(AmmoVisuals.AmmoCount, ShotsLeft);
            _appearanceComponent?.SetData(AmmoVisuals.AmmoMax, Capacity);
            Dirty();
        }

        public override IEntity PeekAmmo()
        {
            // Spawn a dummy entity because it's easier to work with I guess
            // This will get re-used for the projectile
            var ammo = _ammoContainer.ContainedEntity;
            if (ammo == null)
            {
                ammo = Owner.EntityManager.SpawnEntity(_ammoPrototype, Owner.Transform.Coordinates);
                _ammoContainer.Insert(ammo);
            }

            return ammo;
        }

        public override IEntity? TakeProjectile(EntityCoordinates spawnAt)
        {
            var powerCellEntity = _powerCellContainer.ContainedEntity;

            if (powerCellEntity == null)
            {
                return null;
            }

            var capacitor = powerCellEntity.GetComponent<BatteryComponent>();
            if (capacitor.CurrentCharge < _lowerChargeLimit)
            {
                return null;
            }

            // Can fire confirmed
            // Multiply the entity's damage / whatever by the percentage of charge the shot has.
            IEntity entity;
            var chargeChange = Math.Min(capacitor.CurrentCharge, _baseFireCost);
            if (capacitor.UseCharge(chargeChange) < _lowerChargeLimit)
            {
                // Handling of funny exploding cells.
                return null;
            }
            var energyRatio = chargeChange / _baseFireCost;

            if (_ammoContainer.ContainedEntity != null)
            {
                entity = _ammoContainer.ContainedEntity;
                _ammoContainer.Remove(entity);
                entity.Transform.Coordinates = spawnAt;
            }
            else
            {
                entity = Owner.EntityManager.SpawnEntity(_ammoPrototype, spawnAt);
            }

            if (entity.TryGetComponent(out ProjectileComponent? projectileComponent))
            {
                if (energyRatio < 1.0)
                {
                    var newDamages = new Dictionary<DamageType, int>(projectileComponent.Damages.Count);
                    foreach (var (damageType, damage) in projectileComponent.Damages)
                    {
                        newDamages.Add(damageType, (int) (damage * energyRatio));
                    }

                    projectileComponent.Damages = newDamages;
                }
            } else if (entity.TryGetComponent(out HitscanComponent? hitscanComponent))
            {
                hitscanComponent.Damage *= energyRatio;
                hitscanComponent.ColorModifier = energyRatio;
            }
            else
            {
                throw new InvalidOperationException("Ammo doesn't have hitscan or projectile?");
            }

            Dirty();
            UpdateAppearance();
            return entity;
        }

        public bool TryInsertPowerCell(IEntity entity)
        {
            if (_powerCellContainer.ContainedEntity != null)
            {
                return false;
            }

            if (!entity.HasComponent<BatteryComponent>())
            {
                return false;
            }

            if (_soundPowerCellInsert != null)
            {
                SoundSystem.Play(Filter.Pvs(Owner), _soundPowerCellInsert, Owner.Transform.Coordinates, AudioParams.Default.WithVolume(-2));
            }

            _powerCellContainer.Insert(entity);

            Dirty();
            UpdateAppearance();
            return true;
        }

        public override bool UseEntity(UseEntityEventArgs eventArgs)
        {
            if (!_powerCellRemovable)
            {
                return false;
            }

            if (PowerCellEntity == null)
            {
                return false;
            }

            return TryEjectCell(eventArgs.User);
        }

        private bool TryEjectCell(IEntity user)
        {
            if (PowerCell == null || !_powerCellRemovable)
            {
                return false;
            }

            if (!user.TryGetComponent(out HandsComponent? hands))
            {
                return false;
            }

            var cell = PowerCell;
            if (!_powerCellContainer.Remove(cell.Owner))
            {
                return false;
            }

            Dirty();
            UpdateAppearance();

            if (!hands.PutInHand(cell.Owner.GetComponent<ItemComponent>()))
            {
                cell.Owner.Transform.Coordinates = user.Transform.Coordinates;
            }

            if (_soundPowerCellEject != null)
            {
                SoundSystem.Play(Filter.Pvs(Owner), _soundPowerCellEject, Owner.Transform.Coordinates, AudioParams.Default.WithVolume(-2));
            }
            return true;
        }

        public override async Task<bool> InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (!eventArgs.Using.HasComponent<BatteryComponent>())
            {
                return false;
            }

            return TryInsertPowerCell(eventArgs.Using);
        }

        [Verb]
        public sealed class EjectCellVerb : Verb<ServerBatteryBarrelComponent>
        {
            protected override void GetData(IEntity user, ServerBatteryBarrelComponent component, VerbData data)
            {
                if (!EntitySystem.Get<ActionBlockerSystem>().CanInteract(user) || !component._powerCellRemovable)
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                if (component.PowerCell == null)
                {
                    data.Text = Loc.GetString("No cell");
                    data.Visibility = VerbVisibility.Disabled;
                }
                else
                {
                    data.Text = Loc.GetString("Eject cell");
                    data.IconTexture = "/Textures/Interface/VerbIcons/eject.svg.192dpi.png";
                }
            }

            protected override void Activate(IEntity user, ServerBatteryBarrelComponent component)
            {
                component.TryEjectCell(user);
            }
        }
    }
}
