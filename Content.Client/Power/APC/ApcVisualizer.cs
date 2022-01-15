using Content.Shared.APC;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Content.Client.Power.APC
{
    public class ApcVisualizer : AppearanceVisualizer
    {
        public static readonly Color LackColor = Color.FromHex("#d1332e");
        public static readonly Color ChargingColor = Color.FromHex("#2e8ad1");
        public static readonly Color FullColor = Color.FromHex("#3db83b");

        [UsedImplicitly]
        public override void InitializeEntity(EntityUid entity)
        {
            base.InitializeEntity(entity);

            var sprite = IoCManager.Resolve<IEntityManager>().GetComponent<ISpriteComponent>(entity);

            sprite.LayerMapSet(Layers.ChargeState, sprite.AddLayerState("apco3-0"));
            sprite.LayerSetShader(Layers.ChargeState, "unshaded");

            sprite.LayerMapSet(Layers.Lock, sprite.AddLayerState("apcox-0"));
            sprite.LayerSetShader(Layers.Lock, "unshaded");

            sprite.LayerMapSet(Layers.Equipment, sprite.AddLayerState("apco0-3"));
            sprite.LayerSetShader(Layers.Equipment, "unshaded");

            sprite.LayerMapSet(Layers.Lighting, sprite.AddLayerState("apco1-3"));
            sprite.LayerSetShader(Layers.Lighting, "unshaded");

            sprite.LayerMapSet(Layers.Environment, sprite.AddLayerState("apco2-3"));
            sprite.LayerSetShader(Layers.Environment, "unshaded");
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var ent = IoCManager.Resolve<IEntityManager>();
            var sprite = ent.GetComponent<ISpriteComponent>(component.Owner);
            if (component.TryGetData<ApcChargeState>(ApcVisuals.ChargeState, out var state))
            {
                switch (state)
                {
                    case ApcChargeState.Lack:
                        sprite.LayerSetState(Layers.ChargeState, "apco3-0");
                        break;
                    case ApcChargeState.Charging:
                        sprite.LayerSetState(Layers.ChargeState, "apco3-1");
                        break;
                    case ApcChargeState.Full:
                        sprite.LayerSetState(Layers.ChargeState, "apco3-2");
                        break;
                }

                if (ent.TryGetComponent(component.Owner, out SharedPointLightComponent? light))
                {
                    light.Color = state switch
                    {
                        ApcChargeState.Lack => LackColor,
                        ApcChargeState.Charging => ChargingColor,
                        ApcChargeState.Full => FullColor,
                        _ => LackColor
                    };
                }
            }
            else
            {
                sprite.LayerSetState(Layers.ChargeState, "apco3-0");
            }
        }

        enum Layers : byte
        {
            ChargeState,
            Lock,
            Equipment,
            Lighting,
            Environment,
        }
    }
}
