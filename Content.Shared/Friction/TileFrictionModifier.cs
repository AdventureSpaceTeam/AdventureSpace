#nullable enable
using System;
using Content.Shared.NetIDs;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Friction
{
    [RegisterComponent]
    public class SharedTileFrictionModifier : Component
    {
        public override string Name => "TileFrictionModifier";

        /// <summary>
        ///     Multiply the tilefriction cvar by this to get the body's actual tilefriction.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float Modifier
        {
            get => _modifier;
            set
            {
                if (MathHelper.CloseTo(_modifier, value)) return;

                _modifier = value;
            }
        }

        [DataField("modifier")]
        private float _modifier = 1.0f;

        public override ComponentState GetComponentState(ICommonSession session)
        {
            return new TileFrictionComponentState(_modifier);
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);
            if (curState is not TileFrictionComponentState tileState) return;
            _modifier = tileState.Modifier;
        }

        [NetSerializable, Serializable]
        protected class TileFrictionComponentState : ComponentState
        {
            public float Modifier;

            public TileFrictionComponentState(float modifier) : base(ContentNetIDs.TILE_FRICTION)
            {
                Modifier = modifier;
            }
        }
    }
}
