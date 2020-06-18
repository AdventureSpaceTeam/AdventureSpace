using Content.Server.AI.Utility.AiLogic;
using Content.Server.AI.WorldState;
using Content.Server.GameObjects.Components.Movement;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.AI.Utility
{
    public static class UtilityAiHelpers
    {
        public static Blackboard GetBlackboard(IEntity entity)
        {
            if (!entity.TryGetComponent(out AiControllerComponent aiControllerComponent))
            {
                return null;
            }

            if (aiControllerComponent.Processor is UtilityAi utilityAi)
            {
                return utilityAi.Blackboard;
            }

            return null;
        }
    }
}