﻿#nullable enable
using Content.Shared.GameObjects.Components.ActionBlocking;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components.ActionBlocking
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedHandcuffComponent))]
    public class HandcuffComponent : SharedHandcuffComponent
    {
        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            if (curState is not HandcuffedComponentState state)
            {
                return;
            }

            if (state.IconState == string.Empty)
            {
                return;
            }

            if (Owner.TryGetComponent<SpriteComponent>(out var sprite))
            {
                sprite.LayerSetState(0, new RSI.StateId(state.IconState)); // TODO: safety check to see if RSI contains the state?
            }
        }
    }
}
