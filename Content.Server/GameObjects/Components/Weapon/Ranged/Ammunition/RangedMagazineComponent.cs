using System;
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Weapon.Ranged.Barrels;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Audio;
using Content.Shared.GameObjects.Components.Weapons.Ranged.Barrels;
using Content.Shared.Interfaces;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Weapon.Ranged.Ammunition
{
    [RegisterComponent]
    public class RangedMagazineComponent : Component, IMapInit, IInteractUsing, IUse
    {
        public override string Name => "RangedMagazine";

        private Stack<IEntity> _spawnedAmmo = new Stack<IEntity>();
        private Container _ammoContainer;

        public int ShotsLeft => _spawnedAmmo.Count + _unspawnedCount;
        public int Capacity => _capacity;
        private int _capacity;
        
        public MagazineType MagazineType => _magazineType;
        private MagazineType _magazineType;
        public BallisticCaliber Caliber => _caliber;
        private BallisticCaliber _caliber;

        private AppearanceComponent _appearanceComponent;

        // If there's anything already in the magazine
        private string _fillPrototype;
        // By default the magazine won't spawn the entity until needed so we need to keep track of how many left we can spawn
        // Generally you probablt don't want to use this
        private int _unspawnedCount;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _magazineType, "magazineType", MagazineType.Unspecified);
            serializer.DataField(ref _caliber, "caliber", BallisticCaliber.Unspecified);
            serializer.DataField(ref _fillPrototype, "fillPrototype", null);
            serializer.DataField(ref _capacity, "capacity", 20);
        }

        void IMapInit.MapInit()
        {
            if (_fillPrototype != null)
            {
                _unspawnedCount += Capacity;
            }
            UpdateAppearance();
        }

        public override void Initialize()
        {
            base.Initialize();
            _ammoContainer = ContainerManagerComponent.Ensure<Container>($"{Name}-magazine", Owner, out var existing);

            if (existing)
            {
                if (_ammoContainer.ContainedEntities.Count > Capacity)
                {
                    throw new InvalidOperationException("Initialized capacity of magazine higher than its actual capacity");
                }

                foreach (var entity in _ammoContainer.ContainedEntities)
                {
                    _spawnedAmmo.Push(entity);
                    _unspawnedCount--;
                }
            }

            if (Owner.TryGetComponent(out AppearanceComponent appearanceComponent))
            {
                _appearanceComponent = appearanceComponent;
            }
            
            _appearanceComponent?.SetData(MagazineBarrelVisuals.MagLoaded, true);
        }

        private void UpdateAppearance()
        {
            _appearanceComponent?.SetData(AmmoVisuals.AmmoCount, ShotsLeft);
            _appearanceComponent?.SetData(AmmoVisuals.AmmoMax, Capacity);
        }
        
        public bool TryInsertAmmo(IEntity user, IEntity ammo)
        {
            if (!ammo.TryGetComponent(out AmmoComponent ammoComponent))
            {
                return false;
            }

            if (ammoComponent.Caliber != _caliber)
            {
                Owner.PopupMessage(user, Loc.GetString("Wrong caliber"));
                return false;
            }

            if (ShotsLeft >= Capacity)
            {
                Owner.PopupMessage(user, Loc.GetString("Magazine is full"));
                return false;
            }

            _ammoContainer.Insert(ammo);
            _spawnedAmmo.Push(ammo);
            UpdateAppearance();
            return true;
        }

        public IEntity TakeAmmo()
        {
            IEntity ammo = null;
            // If anything's spawned use that first, otherwise use the fill prototype as a fallback (if we have spawn count left)
            if (_spawnedAmmo.TryPop(out var entity))
            {
                ammo = entity;
                _ammoContainer.Remove(entity);
            }
            else if (_unspawnedCount > 0)
            {
                _unspawnedCount--;
                ammo = Owner.EntityManager.SpawnEntity(_fillPrototype, Owner.Transform.GridPosition);
            }
            
            UpdateAppearance();
            return ammo;
        }

        bool IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            return TryInsertAmmo(eventArgs.User, eventArgs.Using);
        }

        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent(out HandsComponent handsComponent))
            {
                return false;
            }

            var ammo = TakeAmmo();
            if (ammo == null)
            {
                return false;
            }

            var itemComponent = ammo.GetComponent<ItemComponent>();
            if (!handsComponent.CanPutInHand(itemComponent))
            {
                ammo.Transform.GridPosition = eventArgs.User.Transform.GridPosition;
                ServerRangedBarrelComponent.EjectCasing(ammo);
            }
            else
            {
                handsComponent.PutInHand(itemComponent);
            }

            return true;
        }
    }
}
