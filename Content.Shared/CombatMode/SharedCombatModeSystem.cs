using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Targeting;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.CombatMode
{
    public abstract class SharedCombatModeSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _protoMan = default!;
        [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<CombatModeComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<CombatModeComponent, ComponentShutdown>(OnShutdown);
            SubscribeLocalEvent<CombatModeComponent, ToggleCombatActionEvent>(OnActionPerform);
        }

        private void OnStartup(EntityUid uid, CombatModeComponent component, ComponentStartup args)
        {
            if (component.CombatToggleAction == null
                && _protoMan.TryIndex(component.CombatToggleActionId, out InstantActionPrototype? toggleProto))
            {
                component.CombatToggleAction = new(toggleProto);
            }

            if (component.CombatToggleAction != null)
                _actionsSystem.AddAction(uid, component.CombatToggleAction, null);
        }

        private void OnShutdown(EntityUid uid, CombatModeComponent component, ComponentShutdown args)
        {
            if (component.CombatToggleAction != null)
                _actionsSystem.RemoveAction(uid, component.CombatToggleAction);
        }

        private void OnActionPerform(EntityUid uid, CombatModeComponent component, ToggleCombatActionEvent args)
        {
            if (args.Handled)
                return;

            SetInCombatMode(uid, !component.IsInCombatMode, component);
            args.Handled = true;
        }

        public void SetCanDisarm(EntityUid entity, bool canDisarm, CombatModeComponent? component = null)
        {
            if (!Resolve(entity, ref component))
                return;

            component.CanDisarm = canDisarm;
        }

        public bool IsInCombatMode(EntityUid? entity, CombatModeComponent? component = null)
        {
            return entity != null && Resolve(entity.Value, ref component, false) && component.IsInCombatMode;
        }

        public virtual void SetInCombatMode(EntityUid entity, bool inCombatMode,
            CombatModeComponent? component = null)
        {
            if (!Resolve(entity, ref component))
                return;

            component.IsInCombatMode = inCombatMode;
        }

        public virtual void SetActiveZone(EntityUid entity, TargetingZone zone,
            CombatModeComponent? component = null)
        {
            if (!Resolve(entity, ref component))
                return;

            component.ActiveZone = zone;
        }

        [Serializable, NetSerializable]
        protected sealed class CombatModeComponentState : ComponentState
        {
            public bool IsInCombatMode { get; }
            public TargetingZone TargetingZone { get; }

            public CombatModeComponentState(bool isInCombatMode, TargetingZone targetingZone)
            {
                IsInCombatMode = isInCombatMode;
                TargetingZone = targetingZone;
            }
        }
    }

    public sealed class ToggleCombatActionEvent : InstantActionEvent { }
}
