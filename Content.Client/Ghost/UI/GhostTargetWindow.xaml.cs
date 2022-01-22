﻿using System;
using System.Collections.Generic;
using System.Globalization;
using Content.Shared.Ghost;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;

namespace Content.Client.Ghost.UI
{
    [GenerateTypedNameReferences]
    public partial class GhostTargetWindow : DefaultWindow
    {
        private readonly IEntityNetworkManager _netManager;

        public List<string> Locations { get; set; } = new();

        public Dictionary<EntityUid, string> Players { get; set; } = new();

        public GhostTargetWindow(IEntityNetworkManager netManager)
        {
            RobustXamlLoader.Load(this);

            _netManager = netManager;
        }

        public void Populate()
        {
            ButtonContainer.DisposeAllChildren();
            AddButtonPlayers();
            AddButtonLocations();
        }

        private void AddButtonPlayers()
        {
            var sortedPlayers = new List<(string, EntityUid)>(Players.Count);

            foreach (var (key, player) in Players)
            {
                sortedPlayers.Add((player, key));
            }

            sortedPlayers.Sort((x, y) => string.Compare(x.Item1, y.Item1, StringComparison.Ordinal));

            foreach (var (key, player) in sortedPlayers)
            {
                var currentButtonRef = new Button
                {
                    Text = key,
                    TextAlign = Label.AlignMode.Right,
                    HorizontalAlignment = HAlignment.Center,
                    VerticalAlignment = VAlignment.Center,
                    SizeFlagsStretchRatio = 1,
                    MinSize = (230, 20),
                    ClipText = true,
                };

                currentButtonRef.OnPressed += (_) =>
                {
                    var msg = new GhostWarpToTargetRequestEvent(player);
                    _netManager.SendSystemNetworkMessage(msg);
                };

                ButtonContainer.AddChild(currentButtonRef);
            }
        }

        private void AddButtonLocations()
        {
            // Server COULD send these sorted but how about we just use the client to do it instead.
            var sortedLocations = new List<string>(Locations);
            sortedLocations.Sort((x, y) => string.Compare(x, y, StringComparison.Ordinal));

            foreach (var name in sortedLocations)
            {
                var currentButtonRef = new Button
                {
                    Text = Loc.GetString("ghost-target-window-current-button", ("name", name)),
                    TextAlign = Label.AlignMode.Right,
                    HorizontalAlignment = HAlignment.Center,
                    VerticalAlignment = VAlignment.Center,
                    SizeFlagsStretchRatio = 1,
                    MinSize = (230, 20),
                    ClipText = true,
                };

                currentButtonRef.OnPressed += _ =>
                {
                    var msg = new GhostWarpToLocationRequestEvent(name);
                    _netManager.SendSystemNetworkMessage(msg);
                };

                ButtonContainer.AddChild(currentButtonRef);
            }
        }
    }
}
