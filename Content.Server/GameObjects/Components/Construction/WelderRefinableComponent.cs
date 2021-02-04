﻿#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Interactable;
using Content.Server.GameObjects.Components.Stack;
using Content.Shared.GameObjects.Components.Interactable;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Construction
{
    /// <summary>
    /// Used for something that can be refined by welder.
    /// For example, glass shard can be refined to glass sheet.
    /// </summary>
    [RegisterComponent]
    public class WelderRefinableComponent : Component, IInteractUsing
    {
        [ViewVariables]
        private HashSet<string>? _refineResult = default;
        [ViewVariables]
        private float _refineTime;

        private bool _beingWelded;

        public override string Name => "WelderRefinable";

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _refineResult, "refineResult", new HashSet<string> { "GlassStack" });
            serializer.DataField(ref _refineTime, "refineTime", 2f);
        }

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            // check if object is welder
            if (!eventArgs.Using.TryGetComponent(out ToolComponent? tool))
                return false;

            // check if someone is already welding object
            if (_beingWelded)
                return false;
            _beingWelded = true;

            if (!await tool.UseTool(eventArgs.User, Owner, _refineTime, ToolQuality.Welding))
            {
                // failed to veld - abort refine
                _beingWelded = false;
                return false;
            }

            // get last owner coordinates and delete it
            var resultPosition = Owner.Transform.Coordinates;
            Owner.Delete();

            // spawn each result afrer refine
            foreach (var result in _refineResult!)
            {
                var droppedEnt = Owner.EntityManager.SpawnEntity(result, resultPosition);

                if (droppedEnt.TryGetComponent<StackComponent>(out var stackComp))
                    stackComp.Count = 1;
            }

            return true;
        }
    }
}
