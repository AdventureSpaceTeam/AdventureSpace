using Content.Client.GameObjects.Components.IconSmoothing;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;
using static Robust.Client.GameObjects.SpriteComponent;

namespace Content.Client.GameObjects.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(IconSmoothComponent))]
    public class ReinforcedWallComponent : IconSmoothComponent
    {
        public override string Name => "ReinforcedWall";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("reinforcedBase")]
        private string _reinforcedStateBase = default;

        protected override void Startup()
        {
            base.Startup();

            var state0 = $"{_reinforcedStateBase}0";
            Sprite.LayerMapSet(ReinforcedCornerLayers.SE, Sprite.AddLayerState(state0));
            Sprite.LayerSetDirOffset(ReinforcedCornerLayers.SE, DirectionOffset.None);
            Sprite.LayerMapSet(ReinforcedCornerLayers.NE, Sprite.AddLayerState(state0));
            Sprite.LayerSetDirOffset(ReinforcedCornerLayers.NE, DirectionOffset.CounterClockwise);
            Sprite.LayerMapSet(ReinforcedCornerLayers.NW, Sprite.AddLayerState(state0));
            Sprite.LayerSetDirOffset(ReinforcedCornerLayers.NW, DirectionOffset.Flip);
            Sprite.LayerMapSet(ReinforcedCornerLayers.SW, Sprite.AddLayerState(state0));
            Sprite.LayerSetDirOffset(ReinforcedCornerLayers.SW, DirectionOffset.Clockwise);
            Sprite.LayerMapSet(ReinforcedWallVisualLayers.Deconstruction, Sprite.AddBlankLayer());
        }

        internal override void CalculateNewSprite()
        {
            base.CalculateNewSprite();

            var (cornerNE, cornerNW, cornerSW, cornerSE) = CalculateCornerFill();

            Sprite.LayerSetState(ReinforcedCornerLayers.NE, $"{_reinforcedStateBase}{(int) cornerNE}");
            Sprite.LayerSetState(ReinforcedCornerLayers.SE, $"{_reinforcedStateBase}{(int) cornerSE}");
            Sprite.LayerSetState(ReinforcedCornerLayers.SW, $"{_reinforcedStateBase}{(int) cornerSW}");
            Sprite.LayerSetState(ReinforcedCornerLayers.NW, $"{_reinforcedStateBase}{(int) cornerNW}");
        }

        public enum ReinforcedCornerLayers : byte
        {
            SE,
            NE,
            NW,
            SW,
        }
    }
}
