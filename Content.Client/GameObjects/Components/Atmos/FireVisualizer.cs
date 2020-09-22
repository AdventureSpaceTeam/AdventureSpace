﻿using System;
using Content.Shared.GameObjects.Components.Atmos;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Client.GameObjects.Components.Atmos
{
    public class FireVisualizer : AppearanceVisualizer
    {
        private int _fireStackAlternateState = 3;
        private string _normalState;
        private string _alternateState;

        public override void LoadData(YamlMappingNode node)
        {
            base.LoadData(node);

            if (node.TryGetNode("fireStackAlternateState", out var fireStack))
            {
                _fireStackAlternateState = fireStack.AsInt();
            }

            if (node.TryGetNode("normalState", out var normalState))
            {
                _normalState = normalState.AsString();
            }

            if (node.TryGetNode("alternateState", out var alternateState))
            {
                _alternateState = alternateState.AsString();
            }
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

            sprite.LayerSetVisible(FireVisualLayers.Fire, onFire);

            if(fireStacks > _fireStackAlternateState && !string.IsNullOrEmpty(_alternateState))
                sprite.LayerSetState(FireVisualLayers.Fire, _alternateState);
            else
                sprite.LayerSetState(FireVisualLayers.Fire, _normalState);
        }
    }

    public enum FireVisualLayers
    {
        Fire
    }
}
