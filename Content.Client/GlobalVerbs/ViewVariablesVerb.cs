﻿using Content.Shared.GameObjects.Verbs;
using Robust.Client.Console;
using Robust.Client.ViewVariables;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.GlobalVerbs
{
    /// <summary>
    /// Global verb that opens a view variables window for the entity in question.
    /// </summary>
    [GlobalVerb]
    class ViewVariablesVerb : GlobalVerb
    {
        public override bool RequireInteractionRange => false;
        public override bool BlockedByContainers => false;

        public override void GetData(IEntity user, IEntity target, VerbData data)
        {
            var groupController = IoCManager.Resolve<IClientConGroupController>();
            if (!groupController.CanViewVar())
            {
                data.Visibility = VerbVisibility.Invisible;
                return;
            }

            data.Text = "View Variables";
            data.CategoryData = VerbCategories.Debug;
        }

        public override void Activate(IEntity user, IEntity target)
        {
            var vvm = IoCManager.Resolve<IViewVariablesManager>();
            vvm.OpenVV(target);
        }
    }
}
