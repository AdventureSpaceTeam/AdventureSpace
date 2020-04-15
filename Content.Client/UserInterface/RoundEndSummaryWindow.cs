﻿using Robust.Client.Graphics;
using Robust.Client.Interfaces.Input;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Content.Client.Utility;
using Robust.Client.Player;
using System.Linq;
using System.Collections.Generic;
using static Robust.Client.UserInterface.Controls.ItemList;
using static Content.Shared.SharedGameTicker;
using System;

namespace Content.Client.UserInterface
{
    public sealed class RoundEndSummaryWindow : SS14Window
    {
        private VBoxContainer RoundEndSummaryTab { get; }
        private VBoxContainer PlayerManifestoTab { get; }
        private TabContainer RoundEndWindowTabs { get; }
        protected override Vector2? CustomSize => (520, 580);

        public RoundEndSummaryWindow(string gm, TimeSpan roundTimeSpan, List<RoundEndPlayerInfo> info )
        {

            Title = Loc.GetString("Round End Summary");

            //Round End Window is split into two tabs, one about the round stats
            //and the other is a list of RoundEndPlayerInfo for each player.
            //This tab would be a good place for things like: "x many people died.",
            //"clown slipped the crew x times.", "x shots were fired this round.", etc.
            //Also good for serious info.
            RoundEndSummaryTab = new VBoxContainer()
            {
                Name = Loc.GetString("Round Information")
            };

            //Tab for listing  unique info per player.
            PlayerManifestoTab = new VBoxContainer()
            {
                Name = Loc.GetString("Player Manifesto")
            };

            RoundEndWindowTabs = new TabContainer();
            RoundEndWindowTabs.AddChild(RoundEndSummaryTab);
            RoundEndWindowTabs.AddChild(PlayerManifestoTab);

            Contents.AddChild(RoundEndWindowTabs);

            //Gamemode Name
            var gamemodeLabel = new RichTextLabel();
            gamemodeLabel.SetMarkup(Loc.GetString("Round of [color=white]{0}[/color] has ended.", gm));
            RoundEndSummaryTab.AddChild(gamemodeLabel);

            //Duration
            var roundTimeLabel = new RichTextLabel();
            roundTimeLabel.SetMarkup(Loc.GetString("It lasted for [color=yellow]{0} hours, {1} minutes, and {2} seconds.",
                roundTimeSpan.Hours,roundTimeSpan.Minutes,roundTimeSpan.Seconds));
            RoundEndSummaryTab.AddChild(roundTimeLabel);

            //Initialize what will be the list of players display.
            var scrollContainer = new ScrollContainer();
            scrollContainer.SizeFlagsVertical = SizeFlags.FillExpand;
            var innerScrollContainer = new VBoxContainer();

            //Put antags on top of the list.
            var manifestSortedList = info.OrderBy(p => !p.Antag);
            //Create labels for each player info.
            foreach (var plyinfo in manifestSortedList)
            {

                var playerInfoText = new RichTextLabel()
                {
                    SizeFlagsVertical = SizeFlags.Fill
                };

                //TODO: On Hover display a popup detailing more play info.
                //For example: their antag goals and if they completed them sucessfully.
                var icNameColor = plyinfo.Antag ? "red" : "white";
                playerInfoText.SetMarkup(
                    Loc.GetString($"[color=gray]{plyinfo.PlayerOOCName}[/color] was [color={icNameColor}]{plyinfo.PlayerICName}[/color] playing role of [color=orange]{plyinfo.Role}[/color]."));
                innerScrollContainer.AddChild(playerInfoText);
            }

            scrollContainer.AddChild(innerScrollContainer);
            //Attach the entire ScrollContainer that holds all the playerinfo.
            PlayerManifestoTab.AddChild(scrollContainer);

            //Finally, display the window.
            OpenCentered();
            MoveToFront();

        }

    }

}
