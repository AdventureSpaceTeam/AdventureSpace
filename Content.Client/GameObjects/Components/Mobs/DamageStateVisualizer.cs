#nullable enable
using System.Collections.Generic;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Mobs;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Client.GameObjects.Components.Mobs
{
    [UsedImplicitly]
    public sealed class DamageStateVisualizer : AppearanceVisualizer, ISerializationHooks
    {
        private DamageState _data = DamageState.Alive;
        private Dictionary<DamageState, string> _stateMap = new();
        private int? _originalDrawDepth;

        [DataField("normal")]
        private string? normal;
        [DataField("crit")]
        private string? crit;
        [DataField("dead")]
        private string? dead;

        void ISerializationHooks.BeforeSerialization()
        {
            _stateMap.TryGetValue(DamageState.Alive, out normal);
            _stateMap.TryGetValue(DamageState.Critical, out crit);
            _stateMap.TryGetValue(DamageState.Dead, out dead);
        }

        void ISerializationHooks.AfterDeserialization()
        {
            if (normal != null)
            {
                _stateMap.Add(DamageState.Alive, normal);
            }

            if (crit != null)
            {
                _stateMap.Add(DamageState.Critical, crit);
            }

            if (dead != null)
            {
                _stateMap.Add(DamageState.Dead, dead);
            }
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);
            var sprite = component.Owner.GetComponent<ISpriteComponent>();
            if (!component.TryGetData(DamageStateVisuals.State, out DamageState data))
            {
                return;
            }

            if (_data == data)
            {
                return;
            }

            _data = data;

            if (_stateMap.TryGetValue(_data, out var state))
            {
                sprite.LayerSetState(DamageStateVisualLayers.Base, state);
            }

            // So they don't draw over mobs anymore
            if (_data == DamageState.Dead)
            {
                _originalDrawDepth = sprite.DrawDepth;
                sprite.DrawDepth = (int) DrawDepth.FloorObjects;
            }
            else if (_originalDrawDepth != null)
            {
                sprite.DrawDepth = _originalDrawDepth.Value;
                _originalDrawDepth = null;
            }
        }
    }

    public enum DamageStateVisualLayers : byte
    {
        Base
    }
}
