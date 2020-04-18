﻿using System.Collections.Generic;
using Content.Client.UserInterface;
using Content.Client.UserInterface.Stylesheets;
using Content.Shared.Chemistry;
using Content.Shared.GameObjects.Components.Chemistry;
using Robust.Client.Graphics.Drawing;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using static Content.Shared.GameObjects.Components.Chemistry.SharedReagentDispenserComponent;

namespace Content.Client.GameObjects.Components.Chemistry
{
    /// <summary>
    /// Client-side UI used to control a <see cref="SharedReagentDispenserComponent"/>
    /// </summary>
    public class ReagentDispenserWindow : SS14Window
    {
        /// <summary>Contains info about the reagent container such as it's contents, if one is loaded into the dispenser.</summary>
        private readonly VBoxContainer ContainerInfo;

        /// <summary>Sets the dispense amount to 1 when pressed.</summary>
        public Button DispenseButton1 { get; }

        /// <summary>Sets the dispense amount to 5 when pressed.</summary>
        public Button DispenseButton5 { get; }

        /// <summary>Sets the dispense amount to 10 when pressed.</summary>
        public Button DispenseButton10 { get; }

        /// <summary>Sets the dispense amount to 25 when pressed.</summary>
        public Button DispenseButton25 { get; }

        /// <summary>Sets the dispense amount to 50 when pressed.</summary>
        public Button DispenseButton50 { get; }

        /// <summary>Sets the dispense amount to 100 when pressed.</summary>
        public Button DispenseButton100 { get; }

        /// <summary>Ejects the reagent container from the dispenser.</summary>
        public Button ClearButton { get; }

        /// <summary>Removes all reagents from the reagent container.</summary>
        public Button EjectButton { get; }

        /// <summary>A grid of buttons for each reagent which can be dispensed.</summary>
        public GridContainer ChemicalList { get; }

#pragma warning disable 649
        [Dependency] private readonly IPrototypeManager _prototypeManager;
        [Dependency] private readonly ILocalizationManager _localizationManager;
#pragma warning restore 649

        protected override Vector2? CustomSize => (500, 600);

        /// <summary>
        /// Create and initialize the dispenser UI client-side. Creates the basic layout,
        /// actual data isn't filled in until the server sends data about the dispenser.
        /// </summary>
        public ReagentDispenserWindow()
        {
            IoCManager.InjectDependencies(this);

            var dispenseAmountGroup = new ButtonGroup();

            Contents.AddChild(new VBoxContainer
            {
                Children =
                {
                    //First, our dispense amount buttons
                    new HBoxContainer
                    {
                        Children =
                        {
                            new Label {Text = _localizationManager.GetString("Amount")},
                            //Padding
                            new Control {CustomMinimumSize = (20, 0)},
                            (DispenseButton1 = new Button {Text = "1", Group = dispenseAmountGroup}),
                            (DispenseButton5 = new Button {Text = "5", Group = dispenseAmountGroup}),
                            (DispenseButton10 = new Button {Text = "10", Group = dispenseAmountGroup}),
                            (DispenseButton25 = new Button {Text = "25", Group = dispenseAmountGroup}),
                            (DispenseButton50 = new Button {Text = "50", Group = dispenseAmountGroup}),
                            (DispenseButton100 = new Button {Text = "100", Group = dispenseAmountGroup}),
                        }
                    },
                    //Padding
                    new Control {CustomMinimumSize = (0.0f, 10.0f)},
                    //Grid of which reagents can be dispensed.
                    (ChemicalList = new GridContainer
                    {
                        Columns = 5
                    }),
                    //Padding
                    new Control {CustomMinimumSize = (0.0f, 10.0f)},
                    new HBoxContainer
                    {
                        Children =
                        {
                            new Label {Text = _localizationManager.GetString("Container: ")},
                            (ClearButton = new Button {Text = _localizationManager.GetString("Clear")}),
                            (EjectButton = new Button {Text = _localizationManager.GetString("Eject")})
                        }
                    },
                    //Wrap the container info in a PanelContainer so we can color it's background differently.
                    new PanelContainer
                    {
                        SizeFlagsVertical = SizeFlags.FillExpand,
                        SizeFlagsStretchRatio = 6,
                        CustomMinimumSize = (0, 150),
                        PanelOverride = new StyleBoxFlat
                        {
                            BackgroundColor = new Color(27, 27, 30)
                        },
                        Children =
                        {
                            //Currently empty, when server sends state data this will have container contents and fill volume.
                            (ContainerInfo = new VBoxContainer
                            {
                                SizeFlagsHorizontal = SizeFlags.FillExpand,
                                Children =
                                {
                                    new Label
                                    {
                                        Text = _localizationManager.GetString("No container loaded.")
                                    }
                                }
                            }),
                        }
                    },
                }
            });
        }

        /// <summary>
        /// Update the button grid of reagents which can be dispensed.
        /// <para>The actions for these buttons are set in <see cref="ReagentDispenserBoundUserInterface.UpdateReagentsList"/>.</para>
        /// </summary>
        /// <param name="inventory">Reagents which can be dispensed by this dispenser</param>
        public void UpdateReagentsList(List<ReagentDispenserInventoryEntry> inventory)
        {
            if (ChemicalList == null) return;
            if (inventory == null) return;

            ChemicalList.Children.Clear();

            foreach (var entry in inventory)
            {
                if (_prototypeManager.TryIndex(entry.ID, out ReagentPrototype proto))
                {
                    ChemicalList.AddChild(new Button {Text = proto.Name});
                }
                else
                {
                    ChemicalList.AddChild(new Button {Text = _localizationManager.GetString("Reagent name not found")});
                }
            }
        }

        /// <summary>
        /// Update the UI state when new state data is received from the server.
        /// </summary>
        /// <param name="state">State data sent by the server.</param>
        public void UpdateState(BoundUserInterfaceState state)
        {
            var castState = (ReagentDispenserBoundUserInterfaceState) state;
            Title = castState.DispenserName;
            UpdateContainerInfo(castState);

            switch (castState.SelectedDispenseAmount.Int())
            {
                case 1:
                    DispenseButton1.Pressed = true;
                    break;
                case 5:
                    DispenseButton5.Pressed = true;
                    break;
                case 10:
                    DispenseButton10.Pressed = true;
                    break;
                case 25:
                    DispenseButton25.Pressed = true;
                    break;
                case 50:
                    DispenseButton50.Pressed = true;
                    break;
                case 100:
                    DispenseButton100.Pressed = true;
                    break;
            }
        }

        /// <summary>
        /// Update the fill state and list of reagents held by the current reagent container, if applicable.
        /// <para>Also highlights a reagent if it's dispense button is being mouse hovered.</para>
        /// </summary>
        /// <param name="state">State data for the dispenser.</param>
        /// <param name="highlightedReagentId">Prototype id of the reagent whose dispense button is currently being mouse hovered.</param>
        public void UpdateContainerInfo(ReagentDispenserBoundUserInterfaceState state,
            string highlightedReagentId = null)
        {
            ContainerInfo.Children.Clear();

            if (!state.HasBeaker)
            {
                ContainerInfo.Children.Add(new Label {Text = _localizationManager.GetString("No container loaded.")});
                return;
            }

            ContainerInfo.Children.Add(new HBoxContainer // Name of the container and its fill status (Ex: 44/100u)
            {
                Children =
                {
                    new Label {Text = $"{state.ContainerName}: "},
                    new Label
                    {
                        Text = $"{state.BeakerCurrentVolume}/{state.BeakerMaxVolume}",
                        StyleClasses = {StyleNano.StyleClassLabelSecondaryColor}
                    }
                }
            });

            if (state.ContainerReagents == null)
            {
                return;
            }

            foreach (var reagent in state.ContainerReagents)
            {
                var name = _localizationManager.GetString("Unknown reagent");
                //Try to the prototype for the given reagent. This gives us it's name.
                if (_prototypeManager.TryIndex(reagent.ReagentId, out ReagentPrototype proto))
                {
                    name = proto.Name;
                }

                //Check if the reagent is being moused over. If so, color it green.
                if (proto != null && proto.ID == highlightedReagentId)
                {
                    ContainerInfo.Children.Add(new HBoxContainer
                    {
                        Children =
                        {
                            new Label
                            {
                                Text = $"{name}: ",
                                StyleClasses = {StyleNano.StyleClassPowerStateGood}
                            },
                            new Label
                            {
                                Text = $"{reagent.Quantity}u",
                                StyleClasses = {StyleNano.StyleClassPowerStateGood}
                            }
                        }
                    });
                }
                else //Otherwise, color it the normal colors.
                {
                    ContainerInfo.Children.Add(new HBoxContainer
                    {
                        Children =
                        {
                            new Label {Text = $"{name}: "},
                            new Label
                            {
                                Text = $"{reagent.Quantity}u",
                                StyleClasses = {StyleNano.StyleClassLabelSecondaryColor}
                            }
                        }
                    });
                }
            }
        }
    }
}
