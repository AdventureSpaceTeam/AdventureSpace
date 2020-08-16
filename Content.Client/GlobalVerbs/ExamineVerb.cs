using Content.Client.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Verbs;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Localization;

namespace Content.Client.GlobalVerbs
{
    [GlobalVerb]
    public class ExamineVerb : GlobalVerb
    {
        public override bool RequireInteractionRange => false;

        public override bool BlockedByContainers => false;

        public override void GetData(IEntity user, IEntity target, VerbData data)
        {
            data.Visibility = VerbVisibility.Visible;
            data.Text = Loc.GetString("Examine");
        }

        public override void Activate(IEntity user, IEntity target)
        {
            EntitySystem.Get<ExamineSystem>().DoExamine(target);
        }
    }
}
