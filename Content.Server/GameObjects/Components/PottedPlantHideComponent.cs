using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces.GameObjects;
using Content.Shared.Audio;
using Content.Shared.Interfaces;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Localization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components
{
    [RegisterComponent]
    public class PottedPlantHideComponent : Component, IInteractUsing, IInteractHand
    {
        private const int MaxItemSize = (int) ReferenceSizes.Pocket;

        public override string Name => "PottedPlantHide";

        [ViewVariables] private ContainerSlot _itemContainer;

        public override void Initialize()
        {
            base.Initialize();

            _itemContainer =
                ContainerManagerComponent.Ensure<ContainerSlot>("flashlight_cell_container", Owner, out _);
        }

        bool IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (_itemContainer.ContainedEntity != null)
            {
                Rustle();

                Owner.PopupMessage(eventArgs.User, Loc.GetString("There's already something in here?!"));
                return false;
            }

            var size = eventArgs.Using.GetComponent<ItemComponent>().ObjectSize;

            // TODO: use proper text macro system for this.

            if (size > MaxItemSize)
            {
                Owner.PopupMessage(eventArgs.User,
                    Loc.GetString("The {0} is too big to fit in the plant!", eventArgs.Using.Name));
                return false;
            }

            var handsComponent = eventArgs.User.GetComponent<IHandsComponent>();

            if (!handsComponent.Drop(eventArgs.Using, _itemContainer))
            {
                return false;
            }

            Owner.PopupMessage(eventArgs.User, Loc.GetString("You hide the {0} in the plant.", eventArgs.Using.Name));
            Rustle();
            return true;
        }

        bool IInteractHand.InteractHand(InteractHandEventArgs eventArgs)
        {
            Rustle();

            if (_itemContainer.ContainedEntity == null)
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("You root around in the roots."));
                return true;
            }

            Owner.PopupMessage(eventArgs.User, Loc.GetString("There was something in there!"));
            if (eventArgs.User.TryGetComponent(out HandsComponent hands))
            {
                hands.PutInHandOrDrop(_itemContainer.ContainedEntity.GetComponent<ItemComponent>());
            }
            else if (_itemContainer.Remove(_itemContainer.ContainedEntity))
            {
                _itemContainer.ContainedEntity.Transform.GridPosition = Owner.Transform.GridPosition;
            }

            return true;
        }

        private void Rustle()
        {
            EntitySystem.Get<AudioSystem>()
                .PlayFromEntity("/Audio/effects/plant_rustle.ogg", Owner, AudioHelpers.WithVariation(0.25f));
        }
    }
}
