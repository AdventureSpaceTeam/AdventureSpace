using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Server.Weapon.Ranged.Ammunition.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.NetIDs;
using Content.Shared.Notification.Managers;
using Content.Shared.Sound;
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
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.Weapon.Ranged.Barrels.Components
{
    /// <summary>
    /// Shotguns mostly
    /// </summary>
    [RegisterComponent]
    public sealed class BoltActionBarrelComponent : ServerRangedBarrelComponent, IMapInit, IExamine
    {
        // Originally I had this logic shared with PumpBarrel and used a couple of variables to control things
        // but it felt a lot messier to play around with, especially when adding verbs

        public override string Name => "BoltActionBarrel";
        public override uint? NetID => ContentNetIDs.BOLTACTION_BARREL;

        public override int ShotsLeft
        {
            get
            {
                var chamberCount = _chamberContainer.ContainedEntity != null ? 1 : 0;
                return chamberCount + _spawnedAmmo.Count + _unspawnedCount;
            }
        }
        public override int Capacity => _capacity;
        [DataField("capacity")]
        private int _capacity = 6;

        private ContainerSlot _chamberContainer = default!;
        private Stack<IEntity> _spawnedAmmo = default!;
        private Container _ammoContainer = default!;

        [ViewVariables]
        [DataField("caliber")]
        private BallisticCaliber _caliber = BallisticCaliber.Unspecified;

        [ViewVariables]
        [DataField("fillPrototype")]
        private string? _fillPrototype;
        [ViewVariables]
        private int _unspawnedCount;

        public bool BoltOpen
        {
            get => _boltOpen;
            set
            {
                if (_boltOpen == value)
                {
                    return;
                }

                if (value)
                {
                    TryEjectChamber();
                    if (_soundBoltOpen.TryGetSound(out var soundBoltOpen))
                    {
                        SoundSystem.Play(Filter.Pvs(Owner), soundBoltOpen, Owner.Transform.Coordinates, AudioParams.Default.WithVolume(-2));
                    }
                }
                else
                {
                    TryFeedChamber();
                    if (_soundBoltClosed.TryGetSound(out var soundBoltClosed))
                    {
                        SoundSystem.Play(Filter.Pvs(Owner), soundBoltClosed, Owner.Transform.Coordinates, AudioParams.Default.WithVolume(-2));
                    }
                }

                _boltOpen = value;
                UpdateAppearance();
                Dirty();
            }
        }
        private bool _boltOpen;
        [DataField("autoCycle")]
        private bool _autoCycle;

        private AppearanceComponent? _appearanceComponent;

        // Sounds
        [DataField("soundCycle")]
        private SoundSpecifier _soundCycle = new SoundPathSpecifier( "/Audio/Weapons/Guns/Cock/sf_rifle_cock.ogg");
        [DataField("soundBoltOpen")]
        private SoundSpecifier _soundBoltOpen = new SoundPathSpecifier("/Audio/Weapons/Guns/Bolt/rifle_bolt_open.ogg");
        [DataField("soundBoltClosed")]
        private SoundSpecifier _soundBoltClosed = new SoundPathSpecifier("/Audio/Weapons/Guns/Bolt/rifle_bolt_closed.ogg");
        [DataField("soundInsert")]
        private SoundSpecifier _soundInsert = new SoundPathSpecifier("/Audio/Weapons/Guns/MagIn/bullet_insert.ogg");

        void IMapInit.MapInit()
        {
            if (_fillPrototype != null)
            {
                _unspawnedCount += Capacity;
                if (_unspawnedCount > 0)
                {
                    _unspawnedCount--;
                    var chamberEntity = Owner.EntityManager.SpawnEntity(_fillPrototype, Owner.Transform.Coordinates);
                    _chamberContainer.Insert(chamberEntity);
                }
            }
            UpdateAppearance();
        }

        public override ComponentState GetComponentState(ICommonSession player)
        {
            (int, int)? count = (ShotsLeft, Capacity);
            var chamberedExists = _chamberContainer.ContainedEntity != null;
            // (Is one chambered?, is the bullet spend)
            var chamber = (chamberedExists, false);

            if (chamberedExists && _chamberContainer.ContainedEntity!.TryGetComponent<AmmoComponent>(out var ammo))
            {
                chamber.Item2 = ammo.Spent;
            }

            return new BoltActionBarrelComponentState(
                chamber,
                FireRateSelector,
                count,
                SoundGunshot.GetSound());
        }

        protected override void Initialize()
        {
            // TODO: Add existing ammo support on revolvers
            base.Initialize();
            _spawnedAmmo = new Stack<IEntity>(_capacity - 1);
            _ammoContainer = ContainerHelpers.EnsureContainer<Container>(Owner, $"{Name}-ammo-container", out var existing);

            if (existing)
            {
                foreach (var entity in _ammoContainer.ContainedEntities)
                {
                    _spawnedAmmo.Push(entity);
                    _unspawnedCount--;
                }
            }

            _chamberContainer = ContainerHelpers.EnsureContainer<ContainerSlot>(Owner, $"{Name}-chamber-container");

            if (Owner.TryGetComponent(out AppearanceComponent? appearanceComponent))
            {
                _appearanceComponent = appearanceComponent;
            }

            _appearanceComponent?.SetData(MagazineBarrelVisuals.MagLoaded, true);
            Dirty();
            UpdateAppearance();
        }

        private void UpdateAppearance()
        {
            _appearanceComponent?.SetData(BarrelBoltVisuals.BoltOpen, BoltOpen);
            _appearanceComponent?.SetData(AmmoVisuals.AmmoCount, ShotsLeft);
            _appearanceComponent?.SetData(AmmoVisuals.AmmoMax, Capacity);
        }

        public override IEntity? PeekAmmo()
        {
            return _chamberContainer.ContainedEntity;
        }

        public override IEntity? TakeProjectile(EntityCoordinates spawnAt)
        {
            var chamberEntity = _chamberContainer.ContainedEntity;
            if (_autoCycle)
            {
                Cycle();
            }
            else
            {
                Dirty();
            }

            return chamberEntity?.GetComponentOrNull<AmmoComponent>()?.TakeBullet(spawnAt);
        }

        protected override bool WeaponCanFire()
        {
            if (!base.WeaponCanFire())
            {
                return false;
            }

            return !BoltOpen && _chamberContainer.ContainedEntity != null;
        }

        private void Cycle(bool manual = false)
        {
            TryEjectChamber();
            TryFeedChamber();

            if (_chamberContainer.ContainedEntity == null && manual)
            {
                BoltOpen = true;
                if (Owner.TryGetContainer(out var container))
                {
                    Owner.PopupMessage(container.Owner, Loc.GetString("bolt-action-barrel-component-bolt-opened"));
                }
                return;
            }
            else
            {
                if (_soundCycle.TryGetSound(out var soundCycle))
                {
                    SoundSystem.Play(Filter.Pvs(Owner), soundCycle, Owner.Transform.Coordinates, AudioParams.Default.WithVolume(-2));
                }
            }

            Dirty();
            UpdateAppearance();
        }

        public bool TryInsertBullet(IEntity user, IEntity ammo)
        {
            if (!ammo.TryGetComponent(out AmmoComponent? ammoComponent))
            {
                return false;
            }

            if (ammoComponent.Caliber != _caliber)
            {
                Owner.PopupMessage(user, Loc.GetString("bolt-action-barrel-component-try-insert-bullet-wrong-caliber"));
                return false;
            }

            if (!BoltOpen)
            {
                Owner.PopupMessage(user, Loc.GetString("bolt-action-barrel-component-try-insert-bullet-bolt-closed"));
                return false;
            }

            if (_chamberContainer.ContainedEntity == null)
            {
                _chamberContainer.Insert(ammo);
                if (_soundInsert.TryGetSound(out var soundInsert))
                {
                    SoundSystem.Play(Filter.Pvs(Owner), soundInsert, Owner.Transform.Coordinates, AudioParams.Default.WithVolume(-2));
                }
                Dirty();
                UpdateAppearance();
                return true;
            }

            if (_ammoContainer.ContainedEntities.Count < Capacity - 1)
            {
                _ammoContainer.Insert(ammo);
                _spawnedAmmo.Push(ammo);
                if (_soundInsert.TryGetSound(out var soundInsert))
                {
                    SoundSystem.Play(Filter.Pvs(Owner), soundInsert, Owner.Transform.Coordinates, AudioParams.Default.WithVolume(-2));
                }
                Dirty();
                UpdateAppearance();
                return true;
            }

            Owner.PopupMessage(user, Loc.GetString("bolt-action-barrel-component-try-insert-bullet-no-room"));

            return false;
        }

        public override bool UseEntity(UseEntityEventArgs eventArgs)
        {
            if (BoltOpen)
            {
                BoltOpen = false;
                Owner.PopupMessage(eventArgs.User, Loc.GetString("bolt-action-barrel-component-bolt-closed"));
                return true;
            }

            Cycle(true);

            return true;
        }

        public override async Task<bool> InteractUsing(InteractUsingEventArgs eventArgs)
        {
            return TryInsertBullet(eventArgs.User, eventArgs.Using);
        }

        private bool TryEjectChamber()
        {
            var chamberedEntity = _chamberContainer.ContainedEntity;
            if (chamberedEntity != null)
            {
                if (!_chamberContainer.Remove(chamberedEntity))
                {
                    return false;
                }
                if (!chamberedEntity.GetComponent<AmmoComponent>().Caseless)
                {
                    EjectCasing(chamberedEntity);
                }
                return true;
            }
            return false;
        }

        private bool TryFeedChamber()
        {
            if (_chamberContainer.ContainedEntity != null)
            {
                return false;
            }
            if (_spawnedAmmo.TryPop(out var next))
            {
                _ammoContainer.Remove(next);
                _chamberContainer.Insert(next);
                return true;
            }
            else if (_unspawnedCount > 0)
            {
                _unspawnedCount--;
                var ammoEntity = Owner.EntityManager.SpawnEntity(_fillPrototype, Owner.Transform.Coordinates);
                _chamberContainer.Insert(ammoEntity);
                return true;
            }
            return false;
        }

        public override void Examine(FormattedMessage message, bool inDetailsRange)
        {
            base.Examine(message, inDetailsRange);

            message.AddMarkup("\n" + Loc.GetString("bolt-action-barrel-component-on-examine", ("caliber",_caliber)));
        }

        [Verb]
        private sealed class OpenBoltVerb : Verb<BoltActionBarrelComponent>
        {
            protected override void GetData(IEntity user, BoltActionBarrelComponent component, VerbData data)
            {
                if (!EntitySystem.Get<ActionBlockerSystem>().CanInteract(user))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.Text = Loc.GetString("open-bolt-verb-get-data-text");
                data.Visibility = component.BoltOpen ? VerbVisibility.Invisible : VerbVisibility.Visible;
            }

            protected override void Activate(IEntity user, BoltActionBarrelComponent component)
            {
                component.BoltOpen = true;
            }
        }

        [Verb]
        private sealed class CloseBoltVerb : Verb<BoltActionBarrelComponent>
        {
            protected override void GetData(IEntity user, BoltActionBarrelComponent component, VerbData data)
            {
                if (!EntitySystem.Get<ActionBlockerSystem>().CanInteract(user))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.Text = Loc.GetString("close-bolt-verb-get-data-text");
                data.Visibility = component.BoltOpen ? VerbVisibility.Visible : VerbVisibility.Invisible;
            }

            protected override void Activate(IEntity user, BoltActionBarrelComponent component)
            {
                component.BoltOpen = false;
            }
        }
    }
}
