﻿using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using System;
using System.Collections.Generic;

namespace Content.Client.UserInterface.Cargo
{
    class CargoConsoleOrderMenu : SS14Window
    {
#pragma warning disable 649
        [Dependency] private readonly ILocalizationManager _loc;
#pragma warning restore 649

        public LineEdit Requester { get; set; }
        public LineEdit Reason { get; set; }
        public SpinBox Amount { get; set; }
        public Button SubmitButton { get; set; }

        public CargoConsoleOrderMenu()
        {
            IoCManager.InjectDependencies(this);

            Title = _loc.GetString("Order Form");

            var vBox = new VBoxContainer();
            var gridContainer = new GridContainer { Columns = 2 };

            var requesterLabel = new Label { Text = _loc.GetString("Name:") };
            Requester = new LineEdit();
            gridContainer.AddChild(requesterLabel);
            gridContainer.AddChild(Requester);

            var reasonLabel = new Label { Text = _loc.GetString("Reason:") };
            Reason = new LineEdit();
            gridContainer.AddChild(reasonLabel);
            gridContainer.AddChild(Reason);

            var amountLabel = new Label { Text = _loc.GetString("Amount:") };
            Amount = new SpinBox
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                Value = 1
            };
            Amount.SetButtons(new List<int>() { -100, -10, -1 }, new List<int>() { 1, 10, 100 });
            Amount.IsValid = (n) => {
                return (n > 0);
            };
            gridContainer.AddChild(amountLabel);
            gridContainer.AddChild(Amount);

            vBox.AddChild(gridContainer);

            SubmitButton = new Button()
            {
                Text = _loc.GetString("OK"),
                TextAlign = Label.AlignMode.Center,
            };
            vBox.AddChild(SubmitButton);

            Contents.AddChild(vBox);
        }
    }
}
