using Content.Shared.Kudzu;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.Kudzu;

public class KudzuVisualizer : AppearanceVisualizer
{
    [DataField("layer")]
    private int Layer { get; } = 0;

    public override void OnChangeData(AppearanceComponent component)
    {
        base.OnChangeData(component);

        if (!component.Owner.TryGetComponent(out SpriteComponent? sprite))
        {
            return;
        }

        if (component.TryGetData(KudzuVisuals.Variant, out int var) && component.TryGetData(KudzuVisuals.GrowthLevel, out int level))
        {
            sprite.LayerMapReserveBlank(Layer);
            sprite.LayerSetState(0, $"kudzu_{level}{var}");
        }
    }
}
