﻿using Content.Shared.GameObjects.Components.Power;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Client.GameObjects.Components.Power
{
    public class SmesVisualizer : AppearanceVisualizer
    {
        public override void InitializeEntity(IEntity entity)
        {
            base.InitializeEntity(entity);

            var sprite = entity.GetComponent<ISpriteComponent>();

            sprite.LayerMapSet(Layers.Input, sprite.AddLayerState("smes-oc0"));
            sprite.LayerSetShader(Layers.Input, "unshaded");
            sprite.LayerMapSet(Layers.Charge, sprite.AddLayerState("smes-og1"));
            sprite.LayerSetShader(Layers.Charge, "unshaded");
            sprite.LayerSetVisible(Layers.Charge, false);
            sprite.LayerMapSet(Layers.Output, sprite.AddLayerState("smes-op0"));
            sprite.LayerSetShader(Layers.Output, "unshaded");
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var sprite = component.Owner.GetComponent<ISpriteComponent>();
            if (!component.TryGetData<int>(SmesVisuals.LastChargeLevel, out var level) || level == 0)
            {
                sprite.LayerSetVisible(Layers.Charge, false);
            }
            else
            {
                sprite.LayerSetVisible(Layers.Charge, true);
                sprite.LayerSetState(Layers.Charge, $"smes-og{level}");
            }

            if (component.TryGetData<ChargeState>(SmesVisuals.LastChargeState, out var state))
            {
                switch (state)
                {
                    case ChargeState.Still:
                        sprite.LayerSetState(Layers.Input, "smes-oc0");
                        sprite.LayerSetState(Layers.Output, "smes-op1");
                        break;
                    case ChargeState.Charging:
                        sprite.LayerSetState(Layers.Input, "smes-oc1");
                        sprite.LayerSetState(Layers.Output, "smes-op1");
                        break;
                    case ChargeState.Discharging:
                        sprite.LayerSetState(Layers.Input, "smes-oc0");
                        sprite.LayerSetState(Layers.Output, "smes-op2");
                        break;
                }
            }
            else
            {
                sprite.LayerSetState(Layers.Input, "smes-oc0");
                sprite.LayerSetState(Layers.Output, "smes-op1");
            }
        }

        enum Layers
        {
            Input,
            Charge,
            Output,
        }
    }
}
