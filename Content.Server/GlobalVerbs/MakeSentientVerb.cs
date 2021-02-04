using Content.Server.Commands;
using Content.Server.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.Verbs;
using Robust.Server.Console;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.Console;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.GlobalVerbs
{
    [GlobalVerb]
    public class MakeSentientVerb : GlobalVerb
    {
        public override bool RequireInteractionRange => false;
        public override bool BlockedByContainers => false;

        public override void GetData(IEntity user, IEntity target, VerbData data)
        {
            data.Visibility = VerbVisibility.Invisible;

            var groupController = IoCManager.Resolve<IConGroupController>();

            if (user == target || target.HasComponent<MindComponent>())
                return;

            var player = user.GetComponent<IActorComponent>().playerSession;
            if (groupController.CanCommand(player, "makesentient"))
            {
                data.Visibility = VerbVisibility.Visible;
                data.Text = Loc.GetString("Make Sentient");
                data.CategoryData = VerbCategories.Debug;
            }
        }

        public override void Activate(IEntity user, IEntity target)
        {
            var groupController = IoCManager.Resolve<IConGroupController>();

            var player = user.GetComponent<IActorComponent>().playerSession;
            if (!groupController.CanCommand(player, "makesentient"))
                return;

            var host = IoCManager.Resolve<IServerConsoleHost>();
            var cmd = new MakeSentientCommand();
            var uidStr = target.Uid.ToString();
            cmd.Execute(new ConsoleShell(host, player), $"{cmd.Command} {uidStr}",
                new[] {uidStr});
        }
    }
}
