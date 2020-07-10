using System;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States.Utility;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.AI.Utility.Considerations.State
{
    /// <summary>
    /// Simple NullCheck on a StoredState
    /// </summary>
    public sealed class StoredStateEntityIsNullCon : Consideration
    {
        public StoredStateEntityIsNullCon Set(Type type, Blackboard context)
        {
            // Ideally we'd just use a variable but then if we were iterating through multiple AI at once it'd be
            // Stuffed so we need to store it on the AI's context.
            context.GetState<StoredStateIsNullState>().SetValue(type);
            return this;
        }
        
        protected override float GetScore(Blackboard context)
        {
            var stateData = context.GetState<StoredStateIsNullState>().GetValue();
            context.GetStoredState(stateData, out StoredStateData<IEntity> state);
            return state.GetValue() == null ? 1.0f : 0.0f;
        }
    }
}