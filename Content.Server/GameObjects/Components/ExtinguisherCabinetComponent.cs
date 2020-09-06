﻿using System.Threading.Tasks;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.Interfaces.GameObjects.Components.Items;
using Content.Shared.Audio;
using Content.Shared.GameObjects.Components;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components
{

    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class ExtinguisherCabinetComponent : Component, IInteractUsing, IInteractHand, IActivate
    {
        public override string Name => "ExtinguisherCabinet";

        private bool _opened = false;
        private string _doorSound;

        [ViewVariables] protected ContainerSlot ItemContainer;
        [ViewVariables] public string DoorSound => _doorSound;

        public override void Initialize()
        {
            base.Initialize();

            ItemContainer =
                ContainerManagerComponent.Ensure<ContainerSlot>("extinguisher_cabinet", Owner, out _);
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _doorSound, "doorSound", "/Audio/Machines/machine_switch.ogg");
        }

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (!_opened)
            {
                _opened = true;
                ClickLatchSound();
            }
            else
            {
                if (ItemContainer.ContainedEntity != null || !eventArgs.Using.HasComponent<FireExtinguisherComponent>())
                {
                    return false;
                }
                var handsComponent = eventArgs.User.GetComponent<IHandsComponent>();

                if (!handsComponent.Drop(eventArgs.Using, ItemContainer))
                {
                    return false;
                }
            }

            UpdateVisuals();

            return true;
        }

        bool IInteractHand.InteractHand(InteractHandEventArgs eventArgs)
        {
            if (_opened)
            {
                if (ItemContainer.ContainedEntity == null)
                {
                    _opened = false;
                    ClickLatchSound();
                }
                else if (eventArgs.User.TryGetComponent(out HandsComponent hands))
                {
                    Owner.PopupMessage(eventArgs.User,
                        Loc.GetString("You take {0:extinguisherName} from the {1:cabinetName}", ItemContainer.ContainedEntity.Name, Owner.Name));
                    hands.PutInHandOrDrop(ItemContainer.ContainedEntity.GetComponent<ItemComponent>());
                }
                else if (ItemContainer.Remove(ItemContainer.ContainedEntity))
                {
                    ItemContainer.ContainedEntity.Transform.Coordinates = Owner.Transform.Coordinates;
                }
            }
            else
            {
                _opened = true;
                ClickLatchSound();
            }

            UpdateVisuals();

            return true;
        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            _opened = !_opened;
            ClickLatchSound();
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (Owner.TryGetComponent(out AppearanceComponent appearance))
            {
                appearance.SetData(ExtinguisherCabinetVisuals.IsOpen, _opened);
                appearance.SetData(ExtinguisherCabinetVisuals.ContainsExtinguisher, ItemContainer.ContainedEntity != null);
            }
        }

        private void ClickLatchSound()
        {
            EntitySystem.Get<AudioSystem>() // Don't have original click, this sounds close
                .PlayFromEntity(DoorSound, Owner, AudioHelpers.WithVariation(0.15f));
        }
    }
}
