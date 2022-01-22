﻿using System.Collections.Generic;
using System.Linq;
using Content.Client.Atmos.EntitySystems;
using Content.Shared.Atmos.Prototypes;
using JetBrains.Annotations;
using Robust.Client.AutoGenerated;
using Robust.Client.Console;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Client.Administration.UI.Tabs.AtmosTab
{
    [GenerateTypedNameReferences]
    [UsedImplicitly]
    public partial class AddGasWindow : DefaultWindow
    {
        private IEnumerable<IMapGrid>? _gridData;
        private IEnumerable<GasPrototype>? _gasData;

        protected override void EnteredTree()
        {
            // Fill out grids
            _gridData = IoCManager.Resolve<IMapManager>().GetAllGrids().Where(g => (int) g.Index != 0);
            foreach (var grid in _gridData)
            {
                var player = IoCManager.Resolve<IPlayerManager>().LocalPlayer?.ControlledEntity;
                var playerGrid = IoCManager.Resolve<IEntityManager>().GetComponentOrNull<TransformComponent>(player)?.GridID;
                GridOptions.AddItem($"{grid.Index} {(playerGrid == grid.Index ? " (Current)" : "")}");
            }

            GridOptions.OnItemSelected += eventArgs => GridOptions.SelectId(eventArgs.Id);

            // Fill out gases
            _gasData = EntitySystem.Get<AtmosphereSystem>().Gases;
            foreach (var gas in _gasData)
            {
                GasOptions.AddItem($"{gas.Name} ({gas.ID})");
            }

            GasOptions.OnItemSelected += eventArgs => GasOptions.SelectId(eventArgs.Id);

            SubmitButton.OnPressed += SubmitButtonOnOnPressed;
        }

        private void SubmitButtonOnOnPressed(BaseButton.ButtonEventArgs obj)
        {
            if (_gridData == null || _gasData == null)
                return;

            var gridList = _gridData.ToList();
            var gridIndex = gridList[GridOptions.SelectedId].Index;

            var gasList = _gasData.ToList();
            var gasId = gasList[GasOptions.SelectedId].ID;

            IoCManager.Resolve<IClientConsoleHost>().ExecuteCommand(
                $"addgas {TileXSpin.Value} {TileYSpin.Value} {gridIndex} {gasId} {AmountSpin.Value}");
        }
    }
}
