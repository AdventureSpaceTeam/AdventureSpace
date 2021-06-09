﻿using Content.Shared.Cabinet;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.Cabinet
{
    [UsedImplicitly]
    public class ItemCabinetVisualizer : AppearanceVisualizer
    {
        // TODO proper layering
        [DataField("fullState", required: true)]
        private string _fullState = default!;

        [DataField("emptyState", required: true)]
        private string _emptyState = default!;

        [DataField("closedState", required: true)]
        private string _closedState = default!;

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (component.Owner.TryGetComponent<SpriteComponent>(out var sprite)
                && component.TryGetData(ItemCabinetVisuals.IsOpen, out bool isOpen))
            {
                if (isOpen)
                {
                    if (component.TryGetData(ItemCabinetVisuals.ContainsItem, out bool contains))
                    {
                        if (contains)
                        {
                            sprite.LayerSetState(0, _fullState);
                        }
                        else
                        {
                            sprite.LayerSetState(0, _emptyState);
                        }

                    }
                }
                else
                {
                    sprite.LayerSetState(0, _closedState);
                }
            }
        }
    }
}
