using System;
using System.Collections.Generic;
using Content.Shared.ActionBlocker;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Ghost
{
    [NetworkedComponent()]
    public class SharedGhostComponent : Component, IActionBlocker
    {
        public override string Name => "Ghost";

        [ViewVariables(VVAccess.ReadWrite)]
        public bool CanGhostInteract
        {
            get => _canGhostInteract;
            set
            {
                if (_canGhostInteract == value) return;
                _canGhostInteract = value;
                Dirty();
            }
        }

        [DataField("canInteract")]
        private bool _canGhostInteract;

        /// <summary>
        ///     Changed by <see cref="SharedGhostSystem.SetCanReturnToBody"/>
        /// </summary>
        // TODO MIRROR change this to use friend classes when thats merged
        [ViewVariables(VVAccess.ReadWrite)]
        public bool CanReturnToBody
        {
            get => _canReturnToBody;
            set
            {
                if (_canReturnToBody == value) return;
                _canReturnToBody = value;
                Dirty();
            }
        }

        [DataField("canReturnToBody")]
        private bool _canReturnToBody;

        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new GhostComponentState(CanReturnToBody, CanGhostInteract);
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not GhostComponentState state)
            {
                return;
            }

            CanReturnToBody = state.CanReturnToBody;
            CanGhostInteract = state.CanGhostInteract;
        }

        public bool CanInteract() => CanGhostInteract;
        public bool CanUse() => CanGhostInteract;
        public bool CanThrow() => CanGhostInteract;
        public bool CanDrop() => CanGhostInteract;
        public bool CanPickup() => CanGhostInteract;
        public bool CanEmote() => false;
        public bool CanAttack() => CanGhostInteract;
    }

    [Serializable, NetSerializable]
    public class GhostComponentState : ComponentState
    {
        public bool CanReturnToBody { get; }
        public bool CanGhostInteract { get; }

        public GhostComponentState(
            bool canReturnToBody,
            bool canGhostInteract)
        {
            CanReturnToBody = canReturnToBody;
            CanGhostInteract = canGhostInteract;
        }
    }
}


