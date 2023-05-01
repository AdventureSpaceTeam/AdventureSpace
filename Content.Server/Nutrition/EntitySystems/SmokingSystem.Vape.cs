using Content.Server.Nutrition.Components;
using Content.Server.Body.Components;
using Content.Shared.Interaction;
using Content.Server.DoAfter;
using System.Threading;
using Content.Server.Explosion.EntitySystems;
using Content.Shared.Damage;
using Content.Server.Popups;
using Content.Shared.IdentityManagement;
using Content.Shared.DoAfter;
using Content.Shared.Emag.Systems;
using Content.Shared.Emag.Components;
using Content.Shared.Nutrition;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos;

/// <summary>
/// System for vapes
/// </summary>
namespace Content.Server.Nutrition.EntitySystems
{
    public sealed partial class SmokingSystem
    {
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        [Dependency] private readonly FoodSystem _foodSystem = default!;
        [Dependency] private readonly ExplosionSystem _explosionSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;

        private void InitializeVapes()
        {
            SubscribeLocalEvent<VapeComponent, AfterInteractEvent>(OnVapeInteraction);
            SubscribeLocalEvent<VapeComponent, VapeDoAfterEvent>(OnVapeDoAfter);
            SubscribeLocalEvent<VapeComponent, GotEmaggedEvent>(OnEmagged);
        }

        private void OnVapeInteraction(EntityUid uid, VapeComponent comp, AfterInteractEvent args) 
        {
            _solutionContainerSystem.TryGetRefillableSolution(uid, out var solution);

            var delay = comp.Delay;
            var forced = true;
            var exploded = false;

            if (!args.CanReach
            || solution == null
            || comp.CancelToken != null
            || !TryComp<BloodstreamComponent>(args.Target, out var _)
            || _foodSystem.IsMouthBlocked(args.Target.Value, args.User))
                return;

            if (args.Target == args.User)
            {
                delay = comp.UserDelay;
                forced = false;
            }

            if (comp.ExplodeOnUse || HasComp<EmaggedComponent>(uid))
            {
                _explosionSystem.QueueExplosion(uid, "Default", comp.ExplosionIntensity, 0.5f, 3, canCreateVacuum: false);
                EntityManager.DeleteEntity(uid);
                exploded = true;
            }
            else
            {
                foreach (var name in solution.Contents)
                {
                    if (name.ReagentId != comp.SolutionNeeded)
                    {
                        exploded = true;
                        _explosionSystem.QueueExplosion(uid, "Default", comp.ExplosionIntensity, 0.5f, 3, canCreateVacuum: false);
                        EntityManager.DeleteEntity(uid);
                        break;
                    }
                }
            }

            if (forced)
            {
                var targetName = Identity.Entity(args.Target.Value, EntityManager);
                var userName = Identity.Entity(args.User, EntityManager);

                _popupSystem.PopupEntity(
                    Loc.GetString("vape-component-try-use-vape-forced", ("user", userName)), args.Target.Value,
                    args.Target.Value);
                
                _popupSystem.PopupEntity(
                    Loc.GetString("vape-component-try-use-vape-forced-user", ("target", targetName)), args.User,
                    args.User);
            }
            else
            {
                _popupSystem.PopupEntity(
                    Loc.GetString("vape-component-try-use-vape"), args.User,
                    args.User);
            }

            if (!exploded)
            {
                comp.CancelToken = new CancellationTokenSource();

                var vapeDoAfterEvent = new VapeDoAfterEvent(solution, forced);
                _doAfterSystem.TryStartDoAfter(new DoAfterArgs(args.User, delay, vapeDoAfterEvent, uid, target: args.Target, used: uid)
                {
                    BreakOnTargetMove = true,
                    BreakOnUserMove = false,
                    BreakOnDamage = true
                });
            }
            args.Handled = true;
		}

        private void OnVapeDoAfter(EntityUid uid, VapeComponent comp, VapeDoAfterEvent args)
        {
            if (args.Cancelled)
            {
                comp.CancelToken = null;
                return;
            }

            comp.CancelToken = null;

            if (args.Handled
            || args.Args.Target == null)
                return;

            var environment = _atmosphereSystem.GetContainingMixture(args.Args.Target.Value, true, true);
            if (environment == null)
            {
                return;
            }

            //Smoking kills(your lungs, but there is no organ damage yet)
            _damageableSystem.TryChangeDamage(args.Args.Target.Value, comp.Damage, true);

            var merger = new GasMixture(1) { Temperature = args.Solution.Temperature};
            merger.SetMoles(comp.GasType, args.Solution.Volume.Value / comp.ReductionFactor);

            _atmosphereSystem.Merge(environment, merger);

            args.Solution.RemoveAllSolution();
            
            if (args.Forced)
            {
                var targetName = Identity.Entity(args.Args.Target.Value, EntityManager);
                var userName = Identity.Entity(args.Args.User, EntityManager);

                _popupSystem.PopupEntity(
                    Loc.GetString("vape-component-vape-success-forced", ("user", userName)), args.Args.Target.Value,
                    args.Args.Target.Value);
                
                _popupSystem.PopupEntity(
                    Loc.GetString("vape-component-vape-success-user-forced", ("target", targetName)), args.Args.User,
                    args.Args.Target.Value);
            }
            else
            {
                _popupSystem.PopupEntity(
                    Loc.GetString("vape-component-vape-success"), args.Args.Target.Value,
                    args.Args.Target.Value);
            }
        }
        private void OnEmagged(EntityUid uid, VapeComponent component, ref GotEmaggedEvent args)
        {
            args.Handled = true;
        }
	}
}