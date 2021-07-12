using Content.Shared.Atmos.Piping;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Content.Client.Atmos.Visualizers
{
    [UsedImplicitly]
    public class PipeColorVisualizer : AppearanceVisualizer
    {
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (!component.Owner.TryGetComponent(out SpriteComponent? sprite))
                return;

            if (component.TryGetData(PipeColorVisuals.Color, out Color color))
            {
                sprite.LayerSetColor(Layers.Pipe, color);
            }
        }

        public enum Layers : byte
        {
            Pipe,
        }
    }
}
