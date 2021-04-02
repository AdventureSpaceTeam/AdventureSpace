#nullable enable
using Content.Shared.GameObjects.Components.Storage;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components.Items
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedItemComponent))]
    public class ItemComponent : SharedItemComponent
    {
        public override bool TryPutInHand(IEntity user)
        {
            return false;
        }

        protected override void OnEquippedPrefixChange()
        {
            if (!Owner.TryGetContainer(out var container))
                return;

            if (container.Owner.TryGetComponent(out HandsComponent? hands))
                hands.RefreshInHands();
        }
    }
}
