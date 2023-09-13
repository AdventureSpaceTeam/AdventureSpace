using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Reagent;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Client.Utility;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System.Linq;
using System.Numerics;
using Content.Shared.FixedPoint;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Chemistry.UI
{
    /// <summary>
    /// Client-side UI used to control a <see cref="SharedChemMasterComponent"/>
    /// </summary>
    [GenerateTypedNameReferences]
    public sealed partial class ChemMasterWindow : FancyWindow
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        public event Action<BaseButton.ButtonEventArgs, ReagentButton>? OnReagentButtonPressed;
        public readonly Button[] PillTypeButtons;

        private const string PillsRsiPath = "/Textures/Objects/Specific/Chemistry/pills.rsi";

        /// <summary>
        /// Create and initialize the chem master UI client-side. Creates the basic layout,
        /// actual data isn't filled in until the server sends data about the chem master.
        /// </summary>
        public ChemMasterWindow()
        {
            RobustXamlLoader.Load(this);
            IoCManager.InjectDependencies(this);

            // Pill type selection buttons, in total there are 20 pills.
            // Pill rsi file should have states named as pill1, pill2, and so on.
            var resourcePath = new ResPath(PillsRsiPath);
            var pillTypeGroup = new ButtonGroup();
            PillTypeButtons = new Button[20];
            for (uint i = 0; i < PillTypeButtons.Length; i++)
            {
                // For every button decide which stylebase to have
                // Every row has 10 buttons
                String styleBase = StyleBase.ButtonOpenBoth;
                uint modulo = i % 10;
                if (i > 0 && modulo == 0)
                    styleBase = StyleBase.ButtonOpenRight;
                else if (i > 0 && modulo == 9)
                    styleBase = StyleBase.ButtonOpenLeft;
                else if (i == 0)
                    styleBase = StyleBase.ButtonOpenRight;

                // Generate buttons
                PillTypeButtons[i] = new Button
                {
                    Access = AccessLevel.Public,
                    StyleClasses = { styleBase },
                    MaxSize = new Vector2(42, 28),
                    Group = pillTypeGroup
                };

                // Generate buttons textures
                var specifier = new SpriteSpecifier.Rsi(resourcePath, "pill" + (i + 1));
                TextureRect pillTypeTexture = new TextureRect
                {
                    Texture = specifier.Frame0(),
                    TextureScale = new Vector2(1.75f, 1.75f),
                    Stretch = TextureRect.StretchMode.KeepCentered,
                };

                PillTypeButtons[i].AddChild(pillTypeTexture);
                Grid.AddChild(PillTypeButtons[i]);
            }

            PillDosage.InitDefaultButtons();
            PillNumber.InitDefaultButtons();
            BottleDosage.InitDefaultButtons();

            // Ensure label length is within the character limit.
            LabelLineEdit.IsValid = s => s.Length <= SharedChemMaster.LabelMaxLength;

            Tabs.SetTabTitle(0, Loc.GetString("chem-master-window-input-tab"));
            Tabs.SetTabTitle(1, Loc.GetString("chem-master-window-output-tab"));
        }

        private ReagentButton MakeReagentButton(string text, ChemMasterReagentAmount amount, ReagentId id, bool isBuffer, string styleClass)
        {
            var button = new ReagentButton(text, amount, id, isBuffer, styleClass);
            button.OnPressed += args
                => OnReagentButtonPressed?.Invoke(args, button);
            return button;
        }

        /// <summary>
        /// Update the UI state when new state data is received from the server.
        /// </summary>
        /// <param name="state">State data sent by the server.</param>
        public void UpdateState(BoundUserInterfaceState state)
        {
            var castState = (ChemMasterBoundUserInterfaceState) state;
            if (castState.UpdateLabel)
                LabelLine = GenerateLabel(castState);
            UpdatePanelInfo(castState);

            var output = castState.OutputContainerInfo;

            BufferCurrentVolume.Text = $" {castState.BufferCurrentVolume?.Int() ?? 0}u";

            InputEjectButton.Disabled = castState.InputContainerInfo is null;
            OutputEjectButton.Disabled = output is null;
            CreateBottleButton.Disabled = output?.Reagents == null;
            CreatePillButton.Disabled = output?.Entities == null;

            var remainingCapacity = output is null ? 0 : (output.MaxVolume - output.CurrentVolume).Int();
            var holdsReagents = output?.Reagents != null;
            var pillNumberMax = holdsReagents ? 0 : remainingCapacity;
            var bottleAmountMax = holdsReagents ? remainingCapacity : 0;

            PillTypeButtons[castState.SelectedPillType].Pressed = true;
            PillNumber.IsValid = x => x >= 0 && x <= pillNumberMax;
            PillDosage.IsValid = x => x > 0 && x <= castState.PillDosageLimit;
            BottleDosage.IsValid = x => x >= 0 && x <= bottleAmountMax;

            if (PillNumber.Value > pillNumberMax)
                PillNumber.Value = pillNumberMax;
            if (BottleDosage.Value > bottleAmountMax)
                BottleDosage.Value = bottleAmountMax;
        }

        /// <summary>
        /// Generate a product label based on reagents in the buffer.
        /// </summary>
        /// <param name="state">State data sent by the server.</param>
        private string GenerateLabel(ChemMasterBoundUserInterfaceState state)
        {
            if (state.BufferCurrentVolume == 0)
                return "";

            var reagent = state.BufferReagents.OrderBy(r => r.Quantity).First().Reagent;
            _prototypeManager.TryIndex(reagent.Prototype, out ReagentPrototype? proto);
            return proto?.LocalizedName ?? "";
        }

        /// <summary>
        /// Update the container, buffer, and packaging panels.
        /// </summary>
        /// <param name="state">State data for the dispenser.</param>
        private void UpdatePanelInfo(ChemMasterBoundUserInterfaceState state)
        {
            BufferTransferButton.Pressed = state.Mode == ChemMasterMode.Transfer;
            BufferDiscardButton.Pressed = state.Mode == ChemMasterMode.Discard;

            BuildContainerUI(InputContainerInfo, state.InputContainerInfo, true);
            BuildContainerUI(OutputContainerInfo, state.OutputContainerInfo, false);

            BufferInfo.Children.Clear();

            if (!state.BufferReagents.Any())
            {
                BufferInfo.Children.Add(new Label { Text = Loc.GetString("chem-master-window-buffer-empty-text") });

                return;
            }

            var bufferHBox = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal
            };
            BufferInfo.AddChild(bufferHBox);

            var bufferLabel = new Label { Text = $"{Loc.GetString("chem-master-window-buffer-label")} " };
            bufferHBox.AddChild(bufferLabel);
            var bufferVol = new Label
            {
                Text = $"{state.BufferCurrentVolume}u",
                StyleClasses = {StyleNano.StyleClassLabelSecondaryColor}
            };
            bufferHBox.AddChild(bufferVol);

            foreach (var (reagent, quantity) in state.BufferReagents)
            {
                // Try to get the prototype for the given reagent. This gives us its name.
                _prototypeManager.TryIndex(reagent.Prototype, out ReagentPrototype? proto);
                var name = proto?.LocalizedName ?? Loc.GetString("chem-master-window-unknown-reagent-text");

                if (proto != null)
                {
                    BufferInfo.Children.Add(new BoxContainer
                    {
                        Orientation = LayoutOrientation.Horizontal,
                        Children =
                        {
                            new Label {Text = $"{name}: "},
                            new Label
                            {
                                Text = $"{quantity}u",
                                StyleClasses = {StyleNano.StyleClassLabelSecondaryColor}
                            },

                            // Padding
                            new Control {HorizontalExpand = true},

                            MakeReagentButton("1", ChemMasterReagentAmount.U1, reagent, true, StyleBase.ButtonOpenRight),
                            MakeReagentButton("5", ChemMasterReagentAmount.U5, reagent, true, StyleBase.ButtonOpenBoth),
                            MakeReagentButton("10", ChemMasterReagentAmount.U10, reagent, true, StyleBase.ButtonOpenBoth),
                            MakeReagentButton("25", ChemMasterReagentAmount.U25, reagent, true, StyleBase.ButtonOpenBoth),
                            MakeReagentButton("50", ChemMasterReagentAmount.U50, reagent, true, StyleBase.ButtonOpenBoth),
                            MakeReagentButton("100", ChemMasterReagentAmount.U100, reagent, true, StyleBase.ButtonOpenBoth),
                            MakeReagentButton(Loc.GetString("chem-master-window-buffer-all-amount"), ChemMasterReagentAmount.All, reagent, true, StyleBase.ButtonOpenLeft),
                        }
                    });
                }
            }
        }

        private void BuildContainerUI(Control control, ContainerInfo? info, bool addReagentButtons)
        {
            control.Children.Clear();

            if (info is null)
            {
                control.Children.Add(new Label
                {
                    Text = Loc.GetString("chem-master-window-no-container-loaded-text")
                });
            }
            else
            {
                // Name of the container and its fill status (Ex: 44/100u)
                control.Children.Add(new BoxContainer
                {
                    Orientation = LayoutOrientation.Horizontal,
                    Children =
                    {
                        new Label {Text = $"{info.DisplayName}: "},
                        new Label
                        {
                            Text = $"{info.CurrentVolume}/{info.MaxVolume}",
                            StyleClasses = {StyleNano.StyleClassLabelSecondaryColor}
                        }
                    }
                });

                IEnumerable<(string Name, ReagentId Id, FixedPoint2 Quantity)> contents;

                if (info.Entities != null)
                {
                    contents = info.Entities.Select(x => (x.Id, default(ReagentId), x.Quantity));
                }
                else if (info.Reagents != null)
                {
                    contents = info.Reagents.Select(x =>
                        {
                            _prototypeManager.TryIndex(x.Reagent.Prototype, out ReagentPrototype? proto);
                            var name = proto?.LocalizedName
                                       ?? Loc.GetString("chem-master-window-unknown-reagent-text");

                            return (name, Id: x.Reagent, x.Quantity);
                        })
                        .OrderBy(r => r.Item1);
                }
                else
                {
                    return;
                }


                foreach (var (name, id, quantity) in contents)
                {
                    var inner = new BoxContainer
                    {
                        Orientation = LayoutOrientation.Horizontal,
                        Children =
                        {
                            new Label { Text = $"{name}: " },
                            new Label
                            {
                                Text = $"{quantity}u",
                                StyleClasses = { StyleNano.StyleClassLabelSecondaryColor },
                            }
                        }
                    };

                    if (addReagentButtons)
                    {
                        var cs = inner.Children;

                        // Padding
                        cs.Add(new Control { HorizontalExpand = true });

                        cs.Add(MakeReagentButton(
                            "1", ChemMasterReagentAmount.U1, id, false, StyleBase.ButtonOpenRight));
                        cs.Add(MakeReagentButton(
                            "5", ChemMasterReagentAmount.U5, id, false, StyleBase.ButtonOpenBoth));
                        cs.Add(MakeReagentButton(
                            "10", ChemMasterReagentAmount.U10, id, false, StyleBase.ButtonOpenBoth));
                        cs.Add(MakeReagentButton(
                            "25", ChemMasterReagentAmount.U25, id, false, StyleBase.ButtonOpenBoth));
                        cs.Add(MakeReagentButton(
                            "50", ChemMasterReagentAmount.U50, id, false, StyleBase.ButtonOpenBoth));
                        cs.Add(MakeReagentButton(
                            "100", ChemMasterReagentAmount.U100, id, false, StyleBase.ButtonOpenBoth));
                        cs.Add(MakeReagentButton(
                            Loc.GetString("chem-master-window-buffer-all-amount"),
                            ChemMasterReagentAmount.All, id, false, StyleBase.ButtonOpenLeft));
                    }

                    control.Children.Add(inner);
                }

            }
        }

        public String LabelLine
        {
            get
            {
                return LabelLineEdit.Text;
            }
            set
            {
                LabelLineEdit.Text = value;
            }
        }
    }

    public sealed class ReagentButton : Button
    {
        public ChemMasterReagentAmount Amount { get; set; }
        public bool IsBuffer = true;
        public ReagentId Id { get; set; }
        public ReagentButton(string text, ChemMasterReagentAmount amount, ReagentId id, bool isBuffer, string styleClass)
        {
            AddStyleClass(styleClass);
            Text = text;
            Amount = amount;
            Id = id;
            IsBuffer = isBuffer;
        }
    }
}
