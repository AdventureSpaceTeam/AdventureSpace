﻿using Content.Server.GameObjects;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.Components.Nutrition;
using Content.Shared.GameObjects;
using Robust.Server.Console;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.GlobalVerbs
{
    /// <summary>
    ///     Completely removes all damage from the DamageableComponent (heals the mob).
    /// </summary>
    [GlobalVerb]
    class RejuvenateVerb : GlobalVerb
    {
        public override bool RequireInteractionRange => false;

        public override void GetData(IEntity user, IEntity target, VerbData data)
        {
            data.Text = "Rejuvenate";
            data.CategoryData = VerbCategories.Debug;
            data.Visibility = VerbVisibility.Invisible;

            var groupController = IoCManager.Resolve<IConGroupController>();

            if (user.TryGetComponent<IActorComponent>(out var player))
            {
                if (!target.HasComponent<DamageableComponent>() && !target.HasComponent<HungerComponent>() &&
                    !target.HasComponent<ThirstComponent>())
                {
                    return;
                }

                if (groupController.CanCommand(player.playerSession, "rejuvenate"))
                {
                    data.Visibility = VerbVisibility.Visible;
                }
            }
        }

        public override void Activate(IEntity user, IEntity target)
        {
            var groupController = IoCManager.Resolve<IConGroupController>();
            if (user.TryGetComponent<IActorComponent>(out var player))
            {
                if (groupController.CanCommand(player.playerSession, "rejuvenate"))
                    PerformRejuvenate(target);
            }
        }
        public static void PerformRejuvenate(IEntity target)
        {
            if (target.TryGetComponent(out DamageableComponent damage))
            {
                damage.HealAllDamage();
            }
            if (target.TryGetComponent(out HungerComponent hunger))
            {
                hunger.ResetFood();
            }
            if (target.TryGetComponent(out ThirstComponent thirst))
            {
                thirst.ResetThirst();
            }
            if (target.TryGetComponent(out StunnableComponent stun))
            {
                stun.ResetStuns();
            }
        }
    }
}
