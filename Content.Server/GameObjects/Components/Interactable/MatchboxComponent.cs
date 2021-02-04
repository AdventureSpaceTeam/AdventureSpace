﻿using System.Threading.Tasks;
using Content.Shared.GameObjects.Components;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Interactable
{
    // TODO make changes in icons when different threshold reached
    // e.g. different icons for 10% 50% 100%
    [RegisterComponent]
    public class MatchboxComponent : Component, IInteractUsing
    {
        public override string Name => "Matchbox";

        int IInteractUsing.Priority => 1;

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (eventArgs.Using.TryGetComponent<MatchstickComponent>(out var matchstick)
                && matchstick.CurrentState == SharedBurningStates.Unlit)
            {
                matchstick.Ignite(eventArgs.User);
                return true;
            }

            return false;
        }
    }
}
