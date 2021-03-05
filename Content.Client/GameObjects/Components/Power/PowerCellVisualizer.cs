using System.Diagnostics;
using Content.Shared.GameObjects.Components.Power;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Client.GameObjects.Components.Power
{
    [UsedImplicitly]
    public class PowerCellVisualizer : AppearanceVisualizer
    {
        [DataField("prefix")]
        private string _prefix;

        public override void InitializeEntity(IEntity entity)
        {
            base.InitializeEntity(entity);

            var sprite = entity.GetComponent<ISpriteComponent>();

            sprite.LayerMapSet(Layers.Charge, sprite.AddLayerState($"{_prefix}_100"));
            sprite.LayerSetShader(Layers.Charge, "unshaded");
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var sprite = component.Owner.GetComponent<ISpriteComponent>();
            if (component.TryGetData(PowerCellVisuals.ChargeLevel, out byte level))
            {
                var adjustedLevel = level * 25;
                sprite.LayerSetState(Layers.Charge, $"{_prefix}_{adjustedLevel}");
            }
        }

        private enum Layers : byte
        {
            Charge
        }
    }
}
