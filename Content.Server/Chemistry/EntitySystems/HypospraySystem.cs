using Content.Server.Chemistry.Components;
using Content.Shared.Interaction;
using Content.Shared.Weapons.Melee;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.Chemistry.EntitySystems
{
    [UsedImplicitly]
    public class HypospraySystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<HyposprayComponent, AfterInteractEvent>(OnAfterInteract);
            SubscribeLocalEvent<HyposprayComponent, ClickAttackEvent>(OnClickAttack);
            SubscribeLocalEvent<HyposprayComponent, SolutionChangedEvent>(OnSolutionChange);
        }

        private void OnSolutionChange(EntityUid uid, HyposprayComponent component, SolutionChangedEvent args)
        {
            component.Dirty();
        }

        public void OnAfterInteract(EntityUid uid, HyposprayComponent comp, AfterInteractEvent args)
        {
            if (!args.CanReach)
                return;
            var target = args.Target;
            var user = args.User;

            comp.TryDoInject(target, user);
        }

        public void OnClickAttack(EntityUid uid, HyposprayComponent comp, ClickAttackEvent args)
        {
            if (args.Target == null)
                return;

            comp.TryDoInject(args.Target.Value, args.User);
        }
    }
}
