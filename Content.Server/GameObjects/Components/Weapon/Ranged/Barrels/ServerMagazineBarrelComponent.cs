using System;
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Weapon.Ranged.Ammunition;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Weapons.Ranged;
using Content.Shared.GameObjects.Components.Weapons.Ranged.Barrels;
using Content.Shared.Interfaces;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Weapon.Ranged.Barrels
{
    [RegisterComponent]
    public sealed class ServerMagazineBarrelComponent : ServerRangedBarrelComponent
    {
        public override string Name => "MagazineBarrel";
        public override uint? NetID => ContentNetIDs.MAGAZINE_BARREL;

        private ContainerSlot _chamberContainer;
        [ViewVariables] public bool HasMagazine => _magazineContainer.ContainedEntity != null;
        private ContainerSlot _magazineContainer;

        [ViewVariables] public MagazineType MagazineTypes => _magazineTypes;
        private MagazineType _magazineTypes;
        [ViewVariables] public BallisticCaliber Caliber => _caliber;
        private BallisticCaliber _caliber;

        public override int ShotsLeft
        {
            get
            {
                var count = 0;
                if (_chamberContainer.ContainedEntity != null)
                {
                    count++;
                }

                var magazine = _magazineContainer.ContainedEntity;
                if (magazine != null)
                {
                    count += magazine.GetComponent<RangedMagazineComponent>().ShotsLeft;
                }

                return count;
            }
        }

        public override int Capacity
        {
            get
            {
                // Chamber
                var count = 1;
                var magazine = _magazineContainer.ContainedEntity;
                if (magazine != null)
                {
                    count += magazine.GetComponent<RangedMagazineComponent>().Capacity;
                }

                return count;
            }
        }

        public bool BoltOpen { get; private set; } = true;
        private bool _autoEjectMag;
        // If the bolt needs to be open before we can insert / remove the mag (i.e. for LMGs)
        public bool MagNeedsOpenBolt => _magNeedsOpenBolt;
        private bool _magNeedsOpenBolt;

        private AppearanceComponent _appearanceComponent;

        // Sounds
        private string _soundBoltOpen;
        private string _soundBoltClosed;
        private string _soundRack;
        private string _soundMagInsert;
        private string _soundMagEject;
        private string _soundAutoEject;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            if (serializer.Reading)
            {
                var magTypes = serializer.ReadDataField("magazineTypes", new List<MagazineType>());
                foreach (var mag in magTypes)
                {
                    _magazineTypes |= mag;
                }
            }
            serializer.DataField(ref _caliber, "caliber", BallisticCaliber.Unspecified);
            serializer.DataField(ref _autoEjectMag, "autoEjectMag", false);
            serializer.DataField(ref _magNeedsOpenBolt, "magNeedsOpenBolt", false);
            serializer.DataField(ref _soundBoltOpen, "soundBoltOpen", null);
            serializer.DataField(ref _soundBoltClosed, "soundBoltClosed", null);
            serializer.DataField(ref _soundRack, "soundRack", null);
            serializer.DataField(ref _soundMagInsert, "soundMagInsert", null);
            serializer.DataField(ref _soundMagEject, "soundMagEject", null);
            serializer.DataField(ref _soundAutoEject, "soundAutoEject", "/Audio/Guns/EmptyAlarm/smg_empty_alarm.ogg");
        }

        public override ComponentState GetComponentState()
        {
            (int, int)? count = null;
            var magazine = _magazineContainer.ContainedEntity;
            if (magazine != null && magazine.TryGetComponent(out RangedMagazineComponent rangedMagazineComponent))
            {
                count = (rangedMagazineComponent.ShotsLeft, rangedMagazineComponent.Capacity);
            }

            return new MagazineBarrelComponentState(
                _chamberContainer.ContainedEntity != null,
                FireRateSelector,
                count,
                SoundGunshot);
        }

        public override void Initialize()
        {
            base.Initialize();

            if (Owner.TryGetComponent(out AppearanceComponent appearanceComponent))
            {
                _appearanceComponent = appearanceComponent;
            }

            _chamberContainer = ContainerManagerComponent.Ensure<ContainerSlot>($"{Name}-chamber", Owner);
            _magazineContainer = ContainerManagerComponent.Ensure<ContainerSlot>($"{Name}-magazine", Owner);
        }

        public void ToggleBolt()
        {
            // For magazines only when we normally set BoltOpen we'll defer the UpdateAppearance until everything is done
            // Whereas this will just call it straight up.
            BoltOpen = !BoltOpen;
            var soundSystem = EntitySystem.Get<AudioSystem>();
            if (BoltOpen)
            {
                if (_soundBoltOpen != null)
                {
                    soundSystem.PlayAtCoords(_soundBoltOpen, Owner.Transform.GridPosition, AudioParams.Default.WithVolume(-5));
                }
            }
            else
            {
                if (_soundBoltClosed != null)
                {
                    soundSystem.PlayAtCoords(_soundBoltClosed, Owner.Transform.GridPosition, AudioParams.Default.WithVolume(-5));
                }
            }
            Dirty();
            UpdateAppearance();
        }

        public override IEntity PeekAmmo()
        {
            return BoltOpen ? null : _chamberContainer.ContainedEntity;
        }

        public override IEntity TakeProjectile()
        {
            if (BoltOpen)
            {
                return null;
            }
            var entity = _chamberContainer.ContainedEntity;

            Cycle();
            return entity?.GetComponent<AmmoComponent>().TakeBullet();
        }

        private void Cycle(bool manual = false)
        {
            if (BoltOpen)
            {
                return;
            }

            var chamberEntity = _chamberContainer.ContainedEntity;
            if (chamberEntity != null)
            {
                _chamberContainer.Remove(chamberEntity);
                var ammoComponent = chamberEntity.GetComponent<AmmoComponent>();
                if (!ammoComponent.Caseless)
                {
                    EjectCasing(chamberEntity);
                }
            }

            // Try and pull a round from the magazine to replace the chamber if possible
            var magazine = _magazineContainer.ContainedEntity;
            var nextRound = magazine?.GetComponent<RangedMagazineComponent>().TakeAmmo();

            if (nextRound != null)
            {
                // If you're really into gunporn you could put a sound here
                _chamberContainer.Insert(nextRound);
            }

            var soundSystem = EntitySystem.Get<AudioSystem>();

            if (_autoEjectMag && magazine != null && magazine.GetComponent<RangedMagazineComponent>().ShotsLeft == 0)
            {
                if (_soundAutoEject != null)
                {
                    soundSystem.PlayAtCoords(_soundAutoEject, Owner.Transform.GridPosition, AudioParams.Default.WithVolume(-2));
                }

                _magazineContainer.Remove(magazine);
                SendNetworkMessage(new MagazineAutoEjectMessage());
            }

            if (nextRound == null && !BoltOpen)
            {
                if (_soundBoltOpen != null)
                {
                    soundSystem.PlayAtCoords(_soundBoltOpen, Owner.Transform.GridPosition, AudioParams.Default.WithVolume(-5));
                }

                if (ContainerHelpers.TryGetContainer(Owner, out var container))
                {
                    Owner.PopupMessage(container.Owner, Loc.GetString("Bolt open"));
                }
                BoltOpen = true;
                Dirty();
                UpdateAppearance();
                return;
            }

            if (manual)
            {
                if (_soundRack != null)
                {
                    soundSystem.PlayAtCoords(_soundRack, Owner.Transform.GridPosition, AudioParams.Default.WithVolume(-2));
                }
            }

            Dirty();
            UpdateAppearance();
        }

        private void UpdateAppearance()
        {
            _appearanceComponent?.SetData(BarrelBoltVisuals.BoltOpen, BoltOpen);
            _appearanceComponent?.SetData(MagazineBarrelVisuals.MagLoaded, _magazineContainer.ContainedEntity != null);
            _appearanceComponent?.SetData(AmmoVisuals.AmmoCount, ShotsLeft);
            _appearanceComponent?.SetData(AmmoVisuals.AmmoMax, Capacity);
        }

        public override bool UseEntity(UseEntityEventArgs eventArgs)
        {
            // Behavior:
            // If bolt open just close it
            // If bolt closed then cycle
            //     If we cycle then get next round
            //         If no more round then open bolt

            if (BoltOpen)
            {
                if (_soundBoltClosed != null)
                {
                    EntitySystem.Get<AudioSystem>().PlayAtCoords(_soundBoltClosed, Owner.Transform.GridPosition, AudioParams.Default.WithVolume(-5));
                }
                Owner.PopupMessage(eventArgs.User, Loc.GetString("Bolt closed"));
                BoltOpen = false;
                Dirty();
                UpdateAppearance();
                return true;
            }

            // Could play a rack-slide specific sound here if you're so inclined (if the chamber is empty but rounds are available)

            Cycle(true);
            return true;
        }

        public void RemoveMagazine(IEntity user)
        {
            var mag = _magazineContainer.ContainedEntity;

            if (mag == null)
            {
                return;
            }

            if (MagNeedsOpenBolt && !BoltOpen)
            {
                Owner.PopupMessage(user, Loc.GetString("Bolt needs to be open"));
                return;
            }

            _magazineContainer.Remove(mag);
            if (_soundMagEject != null)
            {
                EntitySystem.Get<AudioSystem>().PlayAtCoords(_soundMagEject, Owner.Transform.GridPosition, AudioParams.Default.WithVolume(-2));
            }

            if (user.TryGetComponent(out HandsComponent handsComponent))
            {
                handsComponent.PutInHandOrDrop(mag.GetComponent<ItemComponent>());
            }

            Dirty();
            UpdateAppearance();
        }

        public override bool InteractUsing(InteractUsingEventArgs eventArgs)
        {
            // Insert magazine
            if (eventArgs.Using.TryGetComponent(out RangedMagazineComponent magazineComponent))
            {
                if ((MagazineTypes & magazineComponent.MagazineType) == 0)
                {
                    Owner.PopupMessage(eventArgs.User, Loc.GetString("Wrong magazine type"));
                    return false;
                }

                if (magazineComponent.Caliber != _caliber)
                {
                    Owner.PopupMessage(eventArgs.User, Loc.GetString("Wrong caliber"));
                    return false;
                }

                if (_magNeedsOpenBolt && !BoltOpen)
                {
                    Owner.PopupMessage(eventArgs.User, Loc.GetString("Need to open bolt first"));
                    return false;
                }

                if (_magazineContainer.ContainedEntity == null)
                {
                    if (_soundMagInsert != null)
                    {
                        EntitySystem.Get<AudioSystem>().PlayAtCoords(_soundMagInsert, Owner.Transform.GridPosition, AudioParams.Default.WithVolume(-2));
                    }
                    Owner.PopupMessage(eventArgs.User, Loc.GetString("Magazine inserted"));
                    _magazineContainer.Insert(eventArgs.Using);
                    Dirty();
                    UpdateAppearance();
                    return true;
                }

                Owner.PopupMessage(eventArgs.User, Loc.GetString("Already holding a magazine"));
                return false;
            }

            // Insert 1 ammo
            if (eventArgs.Using.TryGetComponent(out AmmoComponent ammoComponent))
            {
                if (!BoltOpen)
                {
                    Owner.PopupMessage(eventArgs.User, Loc.GetString("Cannot insert ammo while bolt is closed"));
                    return false;
                }

                if (ammoComponent.Caliber != _caliber)
                {
                    Owner.PopupMessage(eventArgs.User, Loc.GetString("Wrong caliber"));
                    return false;
                }

                if (_chamberContainer.ContainedEntity == null)
                {
                    Owner.PopupMessage(eventArgs.User, Loc.GetString("Ammo inserted"));
                    _chamberContainer.Insert(eventArgs.Using);
                    Dirty();
                    UpdateAppearance();
                    return true;
                }

                Owner.PopupMessage(eventArgs.User, Loc.GetString("Chamber full"));
                return false;
            }

            return false;
        }

        [Verb]
        private sealed class EjectMagazineVerb : Verb<ServerMagazineBarrelComponent>
        {
            protected override void GetData(IEntity user, ServerMagazineBarrelComponent component, VerbData data)
            {
                if (!ActionBlockerSystem.CanInteract(user))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.Text = Loc.GetString("Eject magazine");
                if (component.MagNeedsOpenBolt)
                {
                    data.Visibility = component.HasMagazine && component.BoltOpen
                        ? VerbVisibility.Visible
                        : VerbVisibility.Disabled;
                    return;
                }

                data.Visibility = component.HasMagazine ? VerbVisibility.Visible : VerbVisibility.Disabled;
            }

            protected override void Activate(IEntity user, ServerMagazineBarrelComponent component)
            {
                component.RemoveMagazine(user);
            }
        }

        [Verb]
        private sealed class OpenBoltVerb : Verb<ServerMagazineBarrelComponent>
        {
            protected override void GetData(IEntity user, ServerMagazineBarrelComponent component, VerbData data)
            {
                if (!ActionBlockerSystem.CanInteract(user))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.Text = Loc.GetString("Open bolt");
                data.Visibility = component.BoltOpen ? VerbVisibility.Disabled : VerbVisibility.Visible;
            }

            protected override void Activate(IEntity user, ServerMagazineBarrelComponent component)
            {
                component.ToggleBolt();
            }
        }

        [Verb]
        private sealed class CloseBoltVerb : Verb<ServerMagazineBarrelComponent>
        {
            protected override void GetData(IEntity user, ServerMagazineBarrelComponent component, VerbData data)
            {
                if (!ActionBlockerSystem.CanInteract(user))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.Text = Loc.GetString("Close bolt");
                data.Visibility = component.BoltOpen ? VerbVisibility.Visible : VerbVisibility.Disabled;
            }

            protected override void Activate(IEntity user, ServerMagazineBarrelComponent component)
            {
                component.ToggleBolt();
            }
        }
    }

    [Flags]
    public enum MagazineType
    {

        Unspecified = 0,
        LPistol = 1 << 0, // Placeholder?
        Pistol = 1 << 1,
        HCPistol = 1 << 2,
        Smg = 1 << 3,
        SmgTopMounted = 1 << 4,
        Rifle = 1 << 5,
        IH = 1 << 6, // Placeholder?
        Box = 1 << 7,
        Pan = 1 << 8,
        Dart = 1 << 9, // Placeholder
    }
}
