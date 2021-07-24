using Content.Server.CombatMode;
using Content.Shared.Actions.Behaviors;
using Content.Shared.Notification;
using Content.Shared.Notification.Managers;
using JetBrains.Annotations;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Actions.Actions
{
    [UsedImplicitly]
    [DataDefinition]
    public class CombatMode : IToggleAction
    {
        public bool DoToggleAction(ToggleActionEventArgs args)
        {
            if (!args.Performer.TryGetComponent(out CombatModeComponent? combatMode))
            {
                return false;
            }

            args.Performer.PopupMessage(Loc.GetString(args.ToggledOn ? "hud-combat-enabled" : "hud-combat-disabled"));
            combatMode.IsInCombatMode = args.ToggledOn;

            return true;
        }
    }
}
