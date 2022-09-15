﻿using System.IO;
using Content.Client.Administration.Commands;
using Content.Client.Administration.Managers;
using Content.Client.Sandbox;
using Content.Client.UserInterface.Systems.DecalPlacer;
using Content.Shared.Administration;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers.Implementations;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.Administration.UI.Tabs.AdminbusTab
{
    [GenerateTypedNameReferences]
    public sealed partial class AdminbusTab : Control
    {
        public AdminbusTab()
        {
            RobustXamlLoader.Load(this);
            IoCManager.InjectDependencies(this);

            var adminManager = IoCManager.Resolve<IClientAdminManager>();

            // For the SpawnEntitiesButton and SpawnTilesButton we need to do the press manually
            // TODO: This will probably need some command check at some point
            SpawnEntitiesButton.OnPressed += SpawnEntitiesButtonOnPressed;
            SpawnTilesButton.OnPressed += SpawnTilesButtonOnOnPressed;
            SpawnDecalsButton.OnPressed += SpawnDecalsButtonOnPressed;
            LoadGamePrototypeButton.OnPressed += LoadGamePrototypeButtonOnPressed;
            LoadGamePrototypeButton.Disabled = !adminManager.HasFlag(AdminFlags.Query);
            LoadBlueprintsButton.Disabled = !adminManager.HasFlag(AdminFlags.Mapping);
        }

        private void LoadGamePrototypeButtonOnPressed(BaseButton.ButtonEventArgs obj)
        {
            LoadPrototypeCommand.LoadPrototype();
        }

        private void SpawnEntitiesButtonOnPressed(BaseButton.ButtonEventArgs obj)
        {
            IoCManager.Resolve<IUserInterfaceManager>().GetUIController<EntitySpawningUIController>().ToggleWindow();
        }

        private void SpawnTilesButtonOnOnPressed(BaseButton.ButtonEventArgs obj)
        {
            IoCManager.Resolve<IUserInterfaceManager>().GetUIController<TileSpawningUIController>().ToggleWindow();
        }

        private void SpawnDecalsButtonOnPressed(BaseButton.ButtonEventArgs obj)
        {
            IoCManager.Resolve<IUserInterfaceManager>().GetUIController<DecalPlacerUIController>().ToggleWindow();
        }
    }
}
