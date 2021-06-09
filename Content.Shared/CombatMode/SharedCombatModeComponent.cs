#nullable enable
using System;
using Content.Shared.NetIDs;
using Content.Shared.Targeting;
using Robust.Shared.GameObjects;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Shared.CombatMode
{
    public abstract class SharedCombatModeComponent : Component
    {
        public sealed override uint? NetID => ContentNetIDs.COMBATMODE;
        public override string Name => "CombatMode";

        private bool _isInCombatMode;
        private TargetingZone _activeZone;

        [ViewVariables(VVAccess.ReadWrite)]
        public virtual bool IsInCombatMode
        {
            get => _isInCombatMode;
            set
            {
                if (_isInCombatMode == value) return;
                _isInCombatMode = value;
                Dirty();

                // Regenerate physics contacts -> Can probably just selectively check
                /* Still a bit jank so left disabled for now.
                if (Owner.TryGetComponent(out PhysicsComponent? physicsComponent))
                {
                    if (value)
                    {
                        physicsComponent.WakeBody();
                    }

                    physicsComponent.RegenerateContacts();
                }
                */
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public virtual TargetingZone ActiveZone
        {
            get => _activeZone;
            set
            {
                if (_activeZone == value) return;
                _activeZone = value;
                Dirty();
            }
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not CombatModeComponentState state)
                return;

            IsInCombatMode = state.IsInCombatMode;
            ActiveZone = state.TargetingZone;
        }


        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new CombatModeComponentState(IsInCombatMode, ActiveZone);
        }

        [Serializable, NetSerializable]
        protected sealed class CombatModeComponentState : ComponentState
        {
            public bool IsInCombatMode { get; }
            public TargetingZone TargetingZone { get; }

            public CombatModeComponentState(bool isInCombatMode, TargetingZone targetingZone)
                : base(ContentNetIDs.COMBATMODE)
            {
                IsInCombatMode = isInCombatMode;
                TargetingZone = targetingZone;
            }
        }
    }
}
