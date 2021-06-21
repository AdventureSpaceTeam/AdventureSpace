#nullable enable
using Content.Server.Administration.UI;
using Content.Server.EUI;
using Content.Server.Interfaces;
using Content.Server.Inventory.Components;
using Content.Server.Items;
using Content.Server.PDA;
using Content.Server.Preferences.Managers;
using Content.Shared.Administration;
using Content.Shared.Inventory;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    class SetOutfitCommand : IConsoleCommand
    {
        public string Command => "setoutfit";

        public string Description => Loc.GetString("set-outfit-command-description", ("requiredComponent", nameof(InventoryComponent)));

        public string Help => Loc.GetString("set-outfit-command-help-text", ("command",Command));

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 1)
            {
                shell.WriteLine(Loc.GetString("shell-wrong-arguments-number"));
                return;
            }

            if (!int.TryParse(args[0], out var entityUid))
            {
                shell.WriteLine(Loc.GetString("shell-entity-uid-must-be-number"));
                return;
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();

            var eUid = new EntityUid(entityUid);

            if (!eUid.IsValid() || !entityManager.EntityExists(eUid))
            {
                shell.WriteLine(Loc.GetString("shell-invalid-entity-id"));
                return;
            }

            var target = entityManager.GetEntity(eUid);

            if (!target.TryGetComponent<InventoryComponent>(out var inventoryComponent))
            {
                shell.WriteLine(Loc.GetString("shell-target-entity-does-not-have-message",("missing", "inventory")));
                return;
            }

            if (args.Length == 1)
            {
                if (shell.Player is not IPlayerSession player)
                {
                    shell.WriteError(Loc.GetString("set-outfit-command-is-not-player-error"));
                    return;
                }

                var eui = IoCManager.Resolve<EuiManager>();
                var ui = new SetOutfitEui(target);
                eui.OpenEui(ui, player);
                return;
            }

            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            if (!prototypeManager.TryIndex<StartingGearPrototype>(args[1], out var startingGear))
            {
                shell.WriteLine(Loc.GetString("set-outfit-command-invalid-outfit-id-error"));
                return;
            }

            HumanoidCharacterProfile? profile = null;
            // Check if we are setting the outfit of a player to respect the preferences
            if (target.TryGetComponent<ActorComponent>(out var actorComponent))
            {
                var userId = actorComponent.PlayerSession.UserId;
                var preferencesManager = IoCManager.Resolve<IServerPreferencesManager>();
                var prefs = preferencesManager.GetPreferences(userId);
                profile = prefs.SelectedCharacter as HumanoidCharacterProfile;
            }

            foreach (var slot in inventoryComponent.Slots)
            {
                inventoryComponent.ForceUnequip(slot);
                var gearStr = startingGear.GetGear(slot, profile);
                if (gearStr == string.Empty)
                {
                    continue;
                }
                var equipmentEntity = entityManager.SpawnEntity(gearStr, target.Transform.Coordinates);
                if (slot == EquipmentSlotDefines.Slots.IDCARD &&
                    equipmentEntity.TryGetComponent<PDAComponent>(out var pdaComponent) &&
                    pdaComponent.ContainedID != null)
                {
                    pdaComponent.ContainedID.FullName = target.Name;
                }

                inventoryComponent.Equip(slot, equipmentEntity.GetComponent<ItemComponent>(), false);
            }
        }
    }
}
