#nullable enable
using System;
using Content.Shared.GameObjects.Components.Atmos;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Serialization;
using YamlDotNet.RepresentationModel;

namespace Content.Client.GameObjects.Components.Atmos
{
    [UsedImplicitly]
    public class PipeConnectorVisualizer : AppearanceVisualizer
    {
        private string _baseState = string.Empty;

        private RSI? _connectorRsi;

        public override void LoadData(YamlMappingNode node)
        {
            base.LoadData(node);
            var serializer = YamlObjectSerializer.NewReader(node);

            serializer.DataField(ref _baseState, "baseState", "pipeConnector");

            var rsiString = SharedSpriteComponent.TextureRoot / serializer.ReadDataField("rsi", "Constructible/Atmos/pipe.rsi");
            var resourceCache = IoCManager.Resolve<IResourceCache>();
            if (resourceCache.TryGetResource(rsiString, out RSIResource? rsi))
                _connectorRsi = rsi.RSI;
            else
                Logger.Error($"{nameof(PipeVisualizer)} could not load to load RSI {rsiString}.");
        }

        public override void InitializeEntity(IEntity entity)
        {
            base.InitializeEntity(entity);

            if (!entity.TryGetComponent<ISpriteComponent>(out var sprite))
                return;

            if (_connectorRsi == null)
                return;

            foreach (Layer layerKey in Enum.GetValues(typeof(Layer)))
            {
                sprite.LayerMapReserveBlank(layerKey);
                var layer = sprite.LayerMapGet(layerKey);
                sprite.LayerSetRSI(layer, _connectorRsi);
                var layerState = _baseState + ((PipeDirection) layerKey).ToString();
                sprite.LayerSetState(layer, layerState);
            }
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (!component.Owner.TryGetComponent<ISpriteComponent>(out var sprite))
                return;

            if (!component.TryGetData(PipeVisuals.VisualState, out PipeVisualState state))
                return;

            foreach (Layer layerKey in Enum.GetValues(typeof(Layer)))
            {
                var dir = (PipeDirection) layerKey;
                var layerVisible = state.ConnectedDirections.HasDirection(dir);

                var layer = sprite.LayerMapGet(layerKey);
                sprite.LayerSetVisible(layer, layerVisible);
            }
        }

        private enum Layer : byte
        {
            NorthConnection = PipeDirection.North,
            SouthConnection = PipeDirection.South,
            EastConnection = PipeDirection.East,
            WestConnection = PipeDirection.West,
        }
    }
}
