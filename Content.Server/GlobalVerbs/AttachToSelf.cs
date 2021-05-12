﻿using Content.Shared.GameObjects.Verbs;
using Robust.Server.Console;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.GlobalVerbs
{
    [GlobalVerb]
    public class AttachToSelf : GlobalVerb
    {
        public override void GetData(IEntity user, IEntity target, VerbData data)
        {
            data.Visibility = VerbVisibility.Invisible;

            if (user == target)
            {
                return;
            }

            if (!user.TryGetComponent(out ActorComponent? actor))
            {
                return;
            }

            var groupController = IoCManager.Resolve<IConGroupController>();
            if (!groupController.CanCommand(actor.PlayerSession, "attachtoself"))
            {
                return;
            }

            data.Visibility = VerbVisibility.Visible;
            data.Text = Loc.GetString("Attach to self");
            data.CategoryData = VerbCategories.Debug;
        }

        public override void Activate(IEntity user, IEntity target)
        {
            if (!user.TryGetComponent(out ActorComponent? actor))
            {
                return;
            }

            var groupController = IoCManager.Resolve<IConGroupController>();
            if (!groupController.CanCommand(actor.PlayerSession, "attachtoself"))
            {
                return;
            }

            target.Transform.AttachParent(user);
        }
    }
}
