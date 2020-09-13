﻿using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using static Content.Shared.GameObjects.Components.Power.AME.SharedAMEShieldComponent;

namespace Content.Client.GameObjects.Components.Power.AME
{
    public class AMEVisualizer : AppearanceVisualizer
    {
        public override void InitializeEntity(IEntity entity)
        {
            base.InitializeEntity(entity);
            var sprite = entity.GetComponent<ISpriteComponent>();
            sprite.LayerMapSet(Layers.Core, sprite.AddLayerState("core"));
            sprite.LayerSetVisible(Layers.Core, false);
            sprite.LayerMapSet(Layers.CoreState, sprite.AddLayerState("core_weak"));
            sprite.LayerSetVisible(Layers.CoreState, false);
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);
            var sprite = component.Owner.GetComponent<ISpriteComponent>();
            if (component.TryGetData<string>(AMEShieldVisuals.Core, out var core))
            {
                if (core == "isCore")
                {
                    sprite.LayerSetState(Layers.Core, "core");
                    sprite.LayerSetVisible(Layers.Core, true);
                }
                else
                {
                    sprite.LayerSetVisible(Layers.Core, false);
                }
            }

            if (component.TryGetData<string>(AMEShieldVisuals.CoreState, out var coreState))
                switch (coreState)
                {
                    case "weak":
                        sprite.LayerSetState(Layers.CoreState, "core_weak");
                        sprite.LayerSetVisible(Layers.CoreState, true);
                        break;
                    case "strong":
                        sprite.LayerSetState(Layers.CoreState, "core_strong");
                        sprite.LayerSetVisible(Layers.CoreState, true);
                        break;
                    case "off":
                        sprite.LayerSetVisible(Layers.CoreState, false);
                        break;
                }
        }
    }

    enum Layers
    {
        Core,
        CoreState,
    }
}
