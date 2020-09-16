﻿using System.Collections.Generic;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Client.UserInterface.Cargo
{
    class CargoConsoleOrderMenu : SS14Window
    {
        public LineEdit Requester { get; set; }
        public LineEdit Reason { get; set; }
        public SpinBox Amount { get; set; }
        public Button SubmitButton { get; set; }

        public CargoConsoleOrderMenu()
        {
            IoCManager.InjectDependencies(this);

            Title = Loc.GetString("Order Form");

            var vBox = new VBoxContainer();
            var gridContainer = new GridContainer { Columns = 2 };

            var requesterLabel = new Label { Text = Loc.GetString("Name:") };
            Requester = new LineEdit();
            gridContainer.AddChild(requesterLabel);
            gridContainer.AddChild(Requester);

            var reasonLabel = new Label { Text = Loc.GetString("Reason:") };
            Reason = new LineEdit();
            gridContainer.AddChild(reasonLabel);
            gridContainer.AddChild(Reason);

            var amountLabel = new Label { Text = Loc.GetString("Amount:") };
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
                Text = Loc.GetString("OK"),
                TextAlign = Label.AlignMode.Center,
            };
            vBox.AddChild(SubmitButton);

            Contents.AddChild(vBox);
        }
    }
}
