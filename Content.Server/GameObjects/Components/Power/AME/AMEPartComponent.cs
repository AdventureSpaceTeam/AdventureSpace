﻿using System.Threading.Tasks;
using System.Linq;
using Content.Server.GameObjects.Components.Interactable;
using Content.Server.Interfaces.GameObjects.Components.Items;
using Content.Shared.GameObjects.Components.Interactable;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.GameObjects.Components.Power.AME
{
    [RegisterComponent]
    [ComponentReference(typeof(IInteractUsing))]
    public class AMEPartComponent : Component, IInteractUsing
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IServerEntityManager _serverEntityManager = default!;

        public override string Name => "AMEPart";

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs args)
        {
            if (!args.User.TryGetComponent(out IHandsComponent hands))
            {
                Owner.PopupMessage(args.User, Loc.GetString("You have no hands."));
                return true;
            }

            var activeHandEntity = hands.GetActiveHand.Owner;
            if (activeHandEntity.TryGetComponent<ToolComponent>(out var multitool) && multitool.Qualities == ToolQuality.Multitool)
            {

                var mapGrid = _mapManager.GetGrid(args.ClickLocation.GetGridId(_serverEntityManager));
                var snapPos = mapGrid.SnapGridCellFor(args.ClickLocation, SnapGridOffset.Center);
                if (mapGrid.GetSnapGridCell(snapPos, SnapGridOffset.Center).Any(sc => sc.Owner.HasComponent<AMEShieldComponent>()))
                {
                    Owner.PopupMessage(args.User, Loc.GetString("Shielding is already there!"));
                    return true;
                }

                var ent = _serverEntityManager.SpawnEntity("AMEShielding", mapGrid.GridTileToLocal(snapPos));
                ent.Transform.LocalRotation = Owner.Transform.LocalRotation;

                Owner.Delete();
            }

            return true;
        }
    }
}
