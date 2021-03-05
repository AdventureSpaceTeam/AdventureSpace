﻿#nullable enable
using System.Runtime.CompilerServices;
using Content.Shared.GameObjects.Components.Morgue;
using Robust.Client.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Client.GameObjects.Components.Storage
{
    public sealed class MorgueVisualizer : AppearanceVisualizer
    {
        [DataField("state_open")]
        private string _stateOpen = "";
        [DataField("state_closed")]
        private string _stateClosed = "";

        [DataField("light_contents")]
        private string _lightContents = "";
        [DataField("light_mob")]
        private string _lightMob = "";
        [DataField("light_soul")]
        private string _lightSoul = "";

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (!component.Owner.TryGetComponent(out ISpriteComponent? sprite)) return;

            if (component.TryGetData(MorgueVisuals.Open, out bool open))
            {
                sprite.LayerSetState(MorgueVisualLayers.Base, open ? _stateOpen : _stateClosed);
            }
            else
            {
                sprite.LayerSetState(MorgueVisualLayers.Base, _stateClosed);
            }

            var lightState = "";
            if (component.TryGetData(MorgueVisuals.HasContents, out bool hasContents) && hasContents) lightState = _lightContents;
            if (component.TryGetData(MorgueVisuals.HasMob,      out bool hasMob)      && hasMob)      lightState = _lightMob;
            if (component.TryGetData(MorgueVisuals.HasSoul,     out bool hasSoul)     && hasSoul)     lightState = _lightSoul;

            if (!string.IsNullOrEmpty(lightState))
            {
                sprite.LayerSetState(MorgueVisualLayers.Light, lightState);
                sprite.LayerSetVisible(MorgueVisualLayers.Light, true);
            }
            else
            {
                sprite.LayerSetVisible(MorgueVisualLayers.Light, false);
            }
        }
    }

    public enum MorgueVisualLayers : byte
    {
        Base,
        Light,
    }
}
