using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.Components.Observer;
using Content.Server.Players;
using Content.Shared.GameObjects.Verbs;
using Robust.Server.Console;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.GlobalVerbs
{
    [GlobalVerb]
    public class ControlMobVerb : GlobalVerb
    {
        public override bool RequireInteractionRange => false;
        public override bool BlockedByContainers => false;

        public override void GetData(IEntity user, IEntity target, VerbData data)
        {
            data.Visibility = VerbVisibility.Invisible;

            var groupController = IoCManager.Resolve<IConGroupController>();
            if (user == target)
            {
                return;
            }

            if (user.TryGetComponent<IActorComponent>(out var player))
            {
                if (!user.HasComponent<MindComponent>() || !target.HasComponent<MindComponent>())
                {
                    return;
                }

                if (groupController.CanCommand(player.playerSession, "controlmob"))
                {
                    data.Visibility = VerbVisibility.Visible;
                    data.Text = Loc.GetString("Control Mob");
                    data.CategoryData = VerbCategories.Debug;
                }
            }
        }

        public override void Activate(IEntity user, IEntity target)
        {
            var groupController = IoCManager.Resolve<IConGroupController>();

            var player = user.GetComponent<IActorComponent>().playerSession;
            if (!groupController.CanCommand(player, "controlmob"))
            {
                return;
            }

            var userMind = player.ContentData().Mind;

            var targetMind = target.GetComponent<MindComponent>();
            var oldEntity = userMind.CurrentEntity;

            targetMind.Mind?.TransferTo(null);
            userMind.TransferTo(target);

            if (oldEntity.HasComponent<GhostComponent>())
                oldEntity.Delete();
        }
    }
}
