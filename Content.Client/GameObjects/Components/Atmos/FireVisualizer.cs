﻿using Content.Shared.GameObjects.Components.Atmos;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Client.GameObjects.Components.Atmos
{
    [UsedImplicitly]
    public class FireVisualizer : AppearanceVisualizer
    {
        [DataField("fireStackAlternateState")]
        private int _fireStackAlternateState = 3;
        [DataField("normalState")]
        private string _normalState;
        [DataField("alternateState")]
        private string _alternateState;
        [DataField("sprite")]
        private string _sprite;

        public override void InitializeEntity(IEntity entity)
        {
            base.InitializeEntity(entity);

            var sprite = entity.GetComponent<ISpriteComponent>();

            sprite.LayerMapReserveBlank(FireVisualLayers.Fire);
            sprite.LayerSetVisible(FireVisualLayers.Fire, false);
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (component.TryGetData(FireVisuals.OnFire, out bool onFire))
            {
                var fireStacks = 0f;

                if (component.TryGetData(FireVisuals.FireStacks, out float stacks))
                    fireStacks = stacks;

                SetOnFire(component, onFire, fireStacks);
            }
        }

        private void SetOnFire(AppearanceComponent component, bool onFire, float fireStacks)
        {
            var sprite = component.Owner.GetComponent<ISpriteComponent>();

            sprite.LayerSetRSI(FireVisualLayers.Fire, _sprite);
            sprite.LayerSetVisible(FireVisualLayers.Fire, onFire);

            if(fireStacks > _fireStackAlternateState && !string.IsNullOrEmpty(_alternateState))
                sprite.LayerSetState(FireVisualLayers.Fire, _alternateState);
            else
                sprite.LayerSetState(FireVisualLayers.Fire, _normalState);
        }
    }

    public enum FireVisualLayers : byte
    {
        Fire
    }
}
