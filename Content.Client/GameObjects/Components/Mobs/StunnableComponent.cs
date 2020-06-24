using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.Components.Movement;
using Robust.Shared.GameObjects;

#nullable enable

namespace Content.Client.GameObjects.Components.Mobs
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedStunnableComponent))]
    public class StunnableComponent : SharedStunnableComponent
    {
        private bool _stunned;
        private bool _knockedDown;
        private bool _slowedDown;

        public override bool Stunned => _stunned;
        public override bool KnockedDown => _knockedDown;
        public override bool SlowedDown => _slowedDown;

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (!(curState is StunnableComponentState state))
            {
                return;
            }

            _stunned = state.Stunned;
            _knockedDown = state.KnockedDown;
            _slowedDown = state.SlowedDown;

            WalkModifierOverride = state.WalkModifierOverride;
            RunModifierOverride = state.RunModifierOverride;

            if (Owner.TryGetComponent(out MovementSpeedModifierComponent movement))
            {
                movement.RefreshMovementSpeedModifiers();
            }
        }
    }
}
