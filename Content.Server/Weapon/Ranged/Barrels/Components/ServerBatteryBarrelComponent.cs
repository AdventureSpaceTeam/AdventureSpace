using System;
using System.Threading.Tasks;
using Content.Server.Hands.Components;
using Content.Server.Items;
using Content.Server.Power.Components;
using Content.Server.Projectiles.Components;
using Content.Shared.Interaction;
using Content.Shared.Sound;
using Content.Shared.Weapons.Ranged.Barrels.Components;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Weapon.Ranged.Barrels.Components
{
    [RegisterComponent]
    [NetworkedComponent()]
    public sealed class ServerBatteryBarrelComponent : ServerRangedBarrelComponent
    {
        [Dependency] private readonly IEntityManager _entities = default!;

        public override string Name => "BatteryBarrel";

        // The minimum change we need before we can fire
        [DataField("lowerChargeLimit")]
        [ViewVariables] private float _lowerChargeLimit = 10;
        [DataField("fireCost")]
        [ViewVariables] private int _baseFireCost = 300;
        // What gets fired
        [DataField("ammoPrototype")]
        [ViewVariables] private string? _ammoPrototype;

        [ViewVariables] public EntityUid? PowerCellEntity => _powerCellContainer.ContainedEntity;
        public BatteryComponent? PowerCell => _powerCellContainer.ContainedEntity == null
            ? null
            : _entities.GetComponentOrNull<BatteryComponent>(_powerCellContainer.ContainedEntity.Value);

        private ContainerSlot _powerCellContainer = default!;
        private ContainerSlot _ammoContainer = default!;
        [DataField("powerCellPrototype")]
        private string? _powerCellPrototype;
        [DataField("powerCellRemovable")]
        [ViewVariables] public bool PowerCellRemovable;

        public override int ShotsLeft
        {
            get
            {
                if (_powerCellContainer.ContainedEntity is not {Valid: true} powerCell)
                {
                    return 0;
                }

                return (int) Math.Ceiling(_entities.GetComponent<BatteryComponent>(powerCell).CurrentCharge / _baseFireCost);
            }
        }

        public override int Capacity
        {
            get
            {
                if (_powerCellContainer.ContainedEntity is not {Valid: true} powerCell)
                {
                    return 0;
                }

                return (int) Math.Ceiling(_entities.GetComponent<BatteryComponent>(powerCell).MaxCharge / _baseFireCost);
            }
        }

        private AppearanceComponent? _appearanceComponent;

        // Sounds
        [DataField("soundPowerCellInsert", required: true)]
        private SoundSpecifier _soundPowerCellInsert = default!;
        [DataField("soundPowerCellEject", required: true)]
        private SoundSpecifier _soundPowerCellEject = default!;

        public override ComponentState GetComponentState()
        {
            (int, int)? count = (ShotsLeft, Capacity);

            return new BatteryBarrelComponentState(
                FireRateSelector,
                count);
        }

        protected override void Initialize()
        {
            base.Initialize();
            _powerCellContainer = Owner.EnsureContainer<ContainerSlot>($"{Name}-powercell-container", out var existing);
            if (!existing && _powerCellPrototype != null)
            {
                var powerCellEntity = _entities.SpawnEntity(_powerCellPrototype, _entities.GetComponent<TransformComponent>(Owner).Coordinates);
                _powerCellContainer.Insert(powerCellEntity);
            }

            if (_ammoPrototype != null)
            {
                _ammoContainer = Owner.EnsureContainer<ContainerSlot>($"{Name}-ammo-container");
            }

            if (_entities.TryGetComponent(Owner, out AppearanceComponent? appearanceComponent))
            {
                _appearanceComponent = appearanceComponent;
            }
            Dirty();
        }

        protected override void Startup()
        {
            base.Startup();
            UpdateAppearance();
        }

        public void UpdateAppearance()
        {
            _appearanceComponent?.SetData(MagazineBarrelVisuals.MagLoaded, _powerCellContainer.ContainedEntity != null);
            _appearanceComponent?.SetData(AmmoVisuals.AmmoCount, ShotsLeft);
            _appearanceComponent?.SetData(AmmoVisuals.AmmoMax, Capacity);
            Dirty();
        }

        public override EntityUid PeekAmmo()
        {
            // Spawn a dummy entity because it's easier to work with I guess
            // This will get re-used for the projectile
            var ammo = _ammoContainer.ContainedEntity;
            if (ammo == null)
            {
                ammo = _entities.SpawnEntity(_ammoPrototype, _entities.GetComponent<TransformComponent>(Owner).Coordinates);
                _ammoContainer.Insert(ammo.Value);
            }

            return ammo.Value;
        }

        public override EntityUid TakeProjectile(EntityCoordinates spawnAt)
        {
            var powerCellEntity = _powerCellContainer.ContainedEntity;

            if (powerCellEntity == null)
            {
                return default;
            }

            var capacitor = _entities.GetComponent<BatteryComponent>(powerCellEntity.Value);
            if (capacitor.CurrentCharge < _lowerChargeLimit)
            {
                return default;
            }

            // Can fire confirmed
            // Multiply the entity's damage / whatever by the percentage of charge the shot has.
            EntityUid? entity;
            var chargeChange = Math.Min(capacitor.CurrentCharge, _baseFireCost);
            if (capacitor.UseCharge(chargeChange) < _lowerChargeLimit)
            {
                // Handling of funny exploding cells.
                return default;
            }
            var energyRatio = chargeChange / _baseFireCost;

            if (_ammoContainer.ContainedEntity != null)
            {
                entity = _ammoContainer.ContainedEntity;
                _ammoContainer.Remove(entity.Value);
                _entities.GetComponent<TransformComponent>(entity.Value).Coordinates = spawnAt;
            }
            else
            {
                entity = _entities.SpawnEntity(_ammoPrototype, spawnAt);
            }

            if (_entities.TryGetComponent(entity.Value, out ProjectileComponent? projectileComponent))
            {
                if (energyRatio < 1.0)
                {
                    projectileComponent.Damage *= energyRatio;
                }
            } else if (_entities.TryGetComponent(entity.Value, out HitscanComponent? hitscanComponent))
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
            return entity.Value;
        }

        public bool TryInsertPowerCell(EntityUid entity)
        {
            if (_powerCellContainer.ContainedEntity != null)
            {
                return false;
            }

            if (!_entities.HasComponent<BatteryComponent>(entity))
            {
                return false;
            }

            SoundSystem.Play(Filter.Pvs(Owner), _soundPowerCellInsert.GetSound(), Owner, AudioParams.Default.WithVolume(-2));

            _powerCellContainer.Insert(entity);

            Dirty();
            UpdateAppearance();
            return true;
        }

        public override bool UseEntity(UseEntityEventArgs eventArgs)
        {
            if (!PowerCellRemovable)
            {
                return false;
            }

            return PowerCellEntity != default && TryEjectCell(eventArgs.User);
        }

        public bool TryEjectCell(EntityUid user)
        {
            if (PowerCell == null || !PowerCellRemovable)
            {
                return false;
            }

            if (!_entities.TryGetComponent(user, out HandsComponent? hands))
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

            if (!hands.PutInHand(_entities.GetComponent<ItemComponent>(cell.Owner)))
            {
                _entities.GetComponent<TransformComponent>(cell.Owner).Coordinates = _entities.GetComponent<TransformComponent>(user).Coordinates;
            }

            SoundSystem.Play(Filter.Pvs(Owner), _soundPowerCellEject.GetSound(), Owner, AudioParams.Default.WithVolume(-2));
            return true;
        }

        public override async Task<bool> InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (!_entities.HasComponent<BatteryComponent>(eventArgs.Using))
            {
                return false;
            }

            return TryInsertPowerCell(eventArgs.Using);
        }
    }
}
