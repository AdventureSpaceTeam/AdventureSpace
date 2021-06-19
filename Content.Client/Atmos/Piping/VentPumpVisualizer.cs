using Content.Shared.Atmos.Visuals;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Atmos.Piping
{
    [UsedImplicitly]
    public class VentPumpVisualizer : AppearanceVisualizer
    {
        private string _offState = "vent_off";
        private string _inState = "vent_in";
        private string _outState = "vent_out";
        private string _weldedState = "vent_welded";

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (!component.Owner.TryGetComponent(out ISpriteComponent? sprite))
                return;

            if (!component.TryGetData(VentPumpVisuals.State, out VentPumpState state))
                return;

            switch (state)
            {
                case VentPumpState.Off:
                    sprite.LayerSetState(VentVisualLayers.Vent, _offState);
                    break;
                case VentPumpState.In:
                    sprite.LayerSetState(VentVisualLayers.Vent, _inState);
                    break;
                case VentPumpState.Out:
                    sprite.LayerSetState(VentVisualLayers.Vent, _outState);
                    break;
                case VentPumpState.Welded:
                    sprite.LayerSetState(VentVisualLayers.Vent, _weldedState);
                    break;
            }
        }
    }

    public enum VentVisualLayers
    {
        Vent,
    }
}
