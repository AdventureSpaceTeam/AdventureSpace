using System;
using System.Collections.Generic;
using Content.Server.AI.Utility.Actions;
using Content.Server.AI.Utility.Actions.Combat.Melee;
using Content.Server.AI.Utility.Considerations;
using Content.Server.AI.Utility.Considerations.Combat.Melee;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Content.Server.AI.WorldState.States.Inventory;
using Content.Server.GameObjects.Components.Weapon.Melee;
using Robust.Shared.IoC;

namespace Content.Server.AI.Utility.ExpandableActions.Combat.Melee
{
    public sealed class EquipMeleeExp : ExpandableUtilityAction
    {
        public override float Bonus => UtilityAction.CombatPrepBonus;

        protected override IEnumerable<Func<float>> GetCommonConsiderations(Blackboard context)
        {
            var considerationsManager = IoCManager.Resolve<ConsiderationsManager>();

            return new[]
            {
                considerationsManager.Get<MeleeWeaponEquippedCon>()
                    .InverseBoolCurve(context),
            };
        }

        public override IEnumerable<UtilityAction> GetActions(Blackboard context)
        {
            var owner = context.GetState<SelfState>().GetValue();

            foreach (var entity in context.GetState<EnumerableInventoryState>().GetValue())
            {
                if (!entity.HasComponent<MeleeWeaponComponent>())
                {
                    continue;
                }
                
                yield return new EquipMelee(owner, entity, Bonus);
            }
        }
    }
}
