#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Content.Client.GameObjects.EntitySystems;
using Content.Client.StationEvents;
using Content.Shared.Atmos;
using Robust.Client.Console;
using Robust.Client.Graphics;
using Robust.Client.Placement;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client.UserInterface.AdminMenu
{
    public class AdminMenuWindow : SS14Window
    {
        public readonly TabContainer MasterTabContainer;
        public readonly VBoxContainer PlayerList;
        public readonly Label PlayerCount;
        private readonly IGameHud _gameHud;

        protected override Vector2? CustomSize => (500, 250);

        public delegate void PlayerListRefresh();

        public event PlayerListRefresh? OnPlayerListRefresh;

        private readonly List<CommandButton> _adminButtons = new()
        {
            new KickCommandButton(),
            new BanCommandButton(),
            new DirectCommandButton("Admin Ghost", "aghost"),
            new TeleportCommandButton(),
            new DirectCommandButton("Permissions Panel", "permissions"),
        };
        private readonly List<CommandButton> _adminbusButtons = new()
        {
            new SpawnEntitiesCommandButton(),
            new SpawnTilesCommandButton(),
            new StationEventsCommandButton()
        };
        private readonly List<CommandButton> _atmosButtons = new()
        {
            new AddAtmosCommandButton(),
            new AddGasCommandButton(),
            new FillGasCommandButton(),
            new SetTempCommandButton(),
        };
        private readonly List<CommandButton> _roundButtons = new()
        {
            new DirectCommandButton("Start Round", "startround"),
            new DirectCommandButton("End Round", "endround"),
            new DirectCommandButton("Restart Round", "restartround"),
        };
        private readonly List<CommandButton> _serverButtons = new()
        {
            new DirectCommandButton("Reboot", "restart"),
            new DirectCommandButton("Shutdown", "shutdown"),
        };

        private static readonly Color SeparatorColor = Color.FromHex("#3D4059");
        private class HSeparator : Control
        {
            public HSeparator()
            {
                AddChild(new PanelContainer {
                    PanelOverride = new StyleBoxFlat
                    {
                        BackgroundColor = SeparatorColor,
                        ContentMarginBottomOverride = 2, ContentMarginLeftOverride = 2
                    }
                });
            }
        }

        private class VSeperator : PanelContainer
        {
            public VSeperator()
            {
                CustomMinimumSize = (2, 5);
                AddChild(new PanelContainer {
                    PanelOverride = new StyleBoxFlat {
                        BackgroundColor = SeparatorColor
                    }
                });
            }
        }

        public void RefreshPlayerList(Dictionary<string, string> namesToPlayers)
        {
            PlayerList.RemoveAllChildren();
            var playerManager = IoCManager.Resolve<IPlayerManager>();
            PlayerCount.Text = $"Players: {playerManager.PlayerCount}";

            var altColor = Color.FromHex("#292B38");
            var defaultColor = Color.FromHex("#2F2F3B");

            var header = new HBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                SeparationOverride = 4,
                Children =
                    {
                        new Label { Text = "Name",
                            SizeFlagsStretchRatio = 2f,
                            SizeFlagsHorizontal = SizeFlags.FillExpand },
                        new VSeperator(),
                        new Label { Text = "Player",
                            SizeFlagsStretchRatio = 2f,
                            SizeFlagsHorizontal = SizeFlags.FillExpand },
                    }
            };
            PlayerList.AddChild(new PanelContainer
            {
                PanelOverride = new StyleBoxFlat
                {
                    BackgroundColor = altColor,
                },
                Children =
                {
                    header
                }
            });
            PlayerList.AddChild(new HSeparator());

            var useAltColor = false;
            foreach (var (name, player) in namesToPlayers)
            {
                var hBox = new HBoxContainer
                {
                    SizeFlagsHorizontal = SizeFlags.FillExpand,
                    SeparationOverride = 4,
                    Children =
                    {
                        new Label {
                            Text = name,
                            SizeFlagsStretchRatio = 2f,
                            SizeFlagsHorizontal = SizeFlags.FillExpand,
                            ClipText = true },
                        new VSeperator(),
                        new Label {
                            Text = player,
                            SizeFlagsStretchRatio = 2f,
                            SizeFlagsHorizontal = SizeFlags.FillExpand,
                            ClipText = true },
                    }
                };
                PlayerList.AddChild(new PanelContainer
                {
                    PanelOverride = new StyleBoxFlat
                    {
                        BackgroundColor = useAltColor ? altColor : defaultColor,
                    },
                    Children =
                    {
                        hBox
                    }
                });
                useAltColor ^= true;
            }
        }

        private void AddCommandButton(List<CommandButton> buttons, Control parent)
        {
            foreach (var cmd in buttons)
            {
                // Check if the player can do the command
                if (!cmd.CanPress())
                    continue;

                //TODO: make toggle?
                var button = new Button
                {
                    Text = cmd.Name
                };
                button.OnPressed += cmd.ButtonPressed;
                parent.AddChild(button);
            }
        }

        public AdminMenuWindow() //TODO: search for buttons?
        {
            _gameHud = IoCManager.Resolve<IGameHud>();
            Title = Loc.GetString("Admin Menu");

            #region PlayerList
            // Players // List of all the players, their entities and status
            var playerTabContainer = new MarginContainer
            {
                MarginLeftOverride = 4,
                MarginTopOverride = 4,
                MarginRightOverride = 4,
                MarginBottomOverride = 4,
                CustomMinimumSize = (50, 50),
            };

            PlayerCount = new Label
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                SizeFlagsStretchRatio = 0.7f,
            };
            var refreshButton = new Button
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                SizeFlagsStretchRatio = 0.3f,
                Text = "Refresh",
            };
            refreshButton.OnPressed += (_) => OnPlayerListRefresh?.Invoke();

            PlayerList = new VBoxContainer();

            var playerVBox = new VBoxContainer
            {
                SizeFlagsVertical = SizeFlags.FillExpand,
                Children =
                {
                    new HBoxContainer
                    {
                        SizeFlagsHorizontal = SizeFlags.FillExpand,
                        Children =
                        {
                            PlayerCount,
                            refreshButton,
                        }
                    },
                    new Control { CustomMinimumSize = (0, 5) },
                    new ScrollContainer
                    {
                        SizeFlagsHorizontal = SizeFlags.FillExpand,
                        SizeFlagsVertical = SizeFlags.FillExpand,
                        Children =
                        {
                            PlayerList
                        },
                    },
                }
            };
            playerTabContainer.AddChild(playerVBox);
            OnPlayerListRefresh?.Invoke();
            #endregion PlayerList

            #region Admin Tab
            // Admin Tab // Actual admin stuff
            var adminTabContainer = new MarginContainer
            {
                MarginLeftOverride = 4,
                MarginTopOverride = 4,
                MarginRightOverride = 4,
                MarginBottomOverride = 4,
                CustomMinimumSize = (50, 50),
            };
            var adminButtonGrid = new GridContainer
            {
                Columns = 4,
            };
            AddCommandButton(_adminButtons, adminButtonGrid);
            adminTabContainer.AddChild(adminButtonGrid);
            #endregion

            #region Adminbus
            // Adminbus // Fun Commands
            var adminbusTabContainer = new MarginContainer
            {
                MarginLeftOverride = 4,
                MarginTopOverride = 4,
                MarginRightOverride = 4,
                MarginBottomOverride = 4,
                CustomMinimumSize = (50, 50),
            };
            var adminbusButtonGrid = new GridContainer
            {
                Columns = 4,
            };
            AddCommandButton(_adminbusButtons, adminbusButtonGrid);
            adminbusTabContainer.AddChild(adminbusButtonGrid);
            #endregion

            #region Atmos
            // Atmos // Commands to add, modify, or remove gases.
            var atmosTabContainer = new MarginContainer
            {
                MarginLeftOverride = 4,
                MarginTopOverride = 4,
                MarginRightOverride = 4,
                MarginBottomOverride = 4,
                CustomMinimumSize = (50, 50),
            };
            var atmosButtonGrid = new GridContainer
            {
                Columns = 4,
            };
            AddCommandButton(_atmosButtons, atmosButtonGrid);
            atmosTabContainer.AddChild(atmosButtonGrid);
            #endregion

            #region Round
            // Round // Commands like Check Antags, End Round, RestartRound
            var roundTabContainer = new MarginContainer
            {
                MarginLeftOverride = 4,
                MarginTopOverride = 4,
                MarginRightOverride = 4,
                MarginBottomOverride = 4,
                CustomMinimumSize = (50, 50),
            };
            var roundButtonGrid = new GridContainer
            {
                Columns = 4,
            };
            AddCommandButton(_roundButtons, roundButtonGrid);
            roundTabContainer.AddChild(roundButtonGrid);
            #endregion

            #region Server
            // Server // Commands like Restart, Shutdown
            var serverTabContainer = new MarginContainer
            {
                MarginLeftOverride = 4,
                MarginTopOverride = 4,
                MarginRightOverride = 4,
                MarginBottomOverride = 4,
                CustomMinimumSize = (50, 50),
            };
            var serverButtonGrid = new GridContainer
            {
                Columns = 4,
            };
            AddCommandButton(_serverButtons, serverButtonGrid);
            serverTabContainer.AddChild(serverButtonGrid);
            #endregion


            //The master menu that contains all of the tabs.
            MasterTabContainer = new TabContainer();

            //Add all the tabs to the Master container.
            MasterTabContainer.AddChild(adminTabContainer);
            MasterTabContainer.SetTabTitle(0, Loc.GetString("Admin"));
            MasterTabContainer.AddChild(adminbusTabContainer);
            MasterTabContainer.SetTabTitle(1, Loc.GetString("Adminbus"));
            MasterTabContainer.AddChild(atmosTabContainer);
            MasterTabContainer.SetTabTitle(2, Loc.GetString("Atmos"));
            MasterTabContainer.AddChild(roundTabContainer);
            MasterTabContainer.SetTabTitle(3, Loc.GetString("Round"));
            MasterTabContainer.AddChild(serverTabContainer);
            MasterTabContainer.SetTabTitle(4, Loc.GetString("Server"));
            MasterTabContainer.AddChild(playerTabContainer);
            MasterTabContainer.SetTabTitle(5, Loc.GetString("Players"));
            Contents.AddChild(MasterTabContainer);
            //Request station events, so we can use them later
            IoCManager.Resolve<IStationEventManager>().RequestEvents();
        }

        protected override void ExitedTree()
        {
            base.ExitedTree();
            _gameHud.AdminButtonDown = false;

        }

        protected override void EnteredTree()
        {
            base.EnteredTree();
            _gameHud.AdminButtonDown = true;
        }

        #region CommandButtonBaseClass
        private abstract class CommandButton
        {
            public virtual string Name { get; }
            public virtual string RequiredCommand { get; }
            public abstract void ButtonPressed(ButtonEventArgs args);
            public virtual bool CanPress()
            {
                return RequiredCommand == string.Empty ||
                    IoCManager.Resolve<IClientConGroupController>().CanCommand(RequiredCommand);
            }

            public CommandButton() : this(string.Empty, string.Empty) {}
            public CommandButton(string name, string command)
            {
                Name = name;
                RequiredCommand = command;
            }
        }

        // Button that opens a UI
        private abstract class UICommandButton : CommandButton
        {
            // The text on the submit button
            public virtual string? SubmitText { get; }
            /// <summary>
            /// Called when the Submit button is pressed
            /// </summary>
            /// <param name="val">Dictionary of the parameter names and values</param>
            public abstract void Submit();
            public override void ButtonPressed(ButtonEventArgs args)
            {
                var manager = IoCManager.Resolve<IAdminMenuManager>();
                var window = new CommandWindow(this);
                window.Submit += Submit;
                manager.OpenCommand(window);
            }
            // List of all the UI Elements
            public abstract List<CommandUIControl> UI { get; }
        }

        // Button that directly calls a Command
        private class DirectCommandButton : CommandButton
        {
            public DirectCommandButton(string name, string command) : base(name, command) { }

            public override void ButtonPressed(ButtonEventArgs args)
            {
                IoCManager.Resolve<IClientConsoleHost>().ExecuteCommand(RequiredCommand);
            }
        }
        #endregion

        #region CommandButtons
        private class SpawnEntitiesCommandButton : CommandButton
        {
            public override string Name => "Spawn Entities";
            //TODO: override CanPress
            public override void ButtonPressed(ButtonEventArgs args)
            {
                var manager = IoCManager.Resolve<IAdminMenuManager>();
                var window = new EntitySpawnWindow(IoCManager.Resolve<IPlacementManager>(),
                    IoCManager.Resolve<IPrototypeManager>(),
                    IoCManager.Resolve<IResourceCache>());
                manager.OpenCommand(window);
            }
        }

        private class SpawnTilesCommandButton : CommandButton
        {
            public override string Name => "Spawn Tiles";
            //TODO: override CanPress
            public override void ButtonPressed(ButtonEventArgs args)
            {
                var manager = IoCManager.Resolve<IAdminMenuManager>();
                var window = new TileSpawnWindow(IoCManager.Resolve<ITileDefinitionManager>(),
                    IoCManager.Resolve<IPlacementManager>(),
                    IoCManager.Resolve<IResourceCache>());
                manager.OpenCommand(window);
            }
        }

        private class StationEventsCommandButton : UICommandButton
        {
            public override string Name => "Station Events";
            public override string RequiredCommand => "events";
            public override string? SubmitText => "Run";

            private readonly CommandUIDropDown _eventsDropDown = new()
            {
                Name = "Event",
                GetData = () =>
                {
                    var events = IoCManager.Resolve<IStationEventManager>().StationEvents.ToList();
                    if (events.Count == 0)
                        events.Add(Loc.GetString("Not loaded"));
                    else
                        events.Add(Loc.GetString("Random"));
                    return events.ToList<object>();
                },
                GetDisplayName = (obj) => (string) obj,
                GetValueFromData = (obj) => ((string) obj).ToLower(),
            };

            public override List<CommandUIControl> UI => new()
            {
                _eventsDropDown,
                new CommandUIButton
                {
                    Name = "Pause",
                    Handler = () =>
                    {
                        IoCManager.Resolve<IClientConsoleHost>().ExecuteCommand("events pause");
                    },
                },
                new CommandUIButton
                {
                    Name = "Resume",
                    Handler = () =>
                    {
                        IoCManager.Resolve<IClientConsoleHost>().ExecuteCommand("events resume");
                    },
                },
            };

            public override void Submit()
            {
                IoCManager.Resolve<IClientConsoleHost>().ExecuteCommand($"events run \"{_eventsDropDown.GetValue()}\"");
            }
        }

        private class KickCommandButton : UICommandButton
        {
            public override string Name => "Kick";
            public override string RequiredCommand => "kick";

            private readonly CommandUIDropDown _playerDropDown = new()
            {
                Name = "Player",
                GetData = () => IoCManager.Resolve<IPlayerManager>().Sessions.ToList<object>(),
                GetDisplayName = (obj) => $"{((IPlayerSession) obj).Name} ({((IPlayerSession) obj).AttachedEntity?.Name})",
                GetValueFromData = (obj) => ((IPlayerSession) obj).Name,
            };
            private readonly CommandUILineEdit _reason = new()
            {
                Name = "Reason"
            };

            public override List<CommandUIControl> UI => new()
            {
                _playerDropDown,
                _reason
            };

            public override void Submit()
            {
                IoCManager.Resolve<IClientConsoleHost>().ExecuteCommand($"kick \"{_playerDropDown.GetValue()}\" \"{CommandParsing.Escape(_reason.GetValue())}\"");
            }
        }

        private class BanCommandButton : UICommandButton
        {
            public override string Name => "Ban";
            public override string RequiredCommand => "ban";

            private readonly CommandUIDropDown _playerDropDown = new()
            {
                Name = "Player",
                GetData = () => IoCManager.Resolve<IPlayerManager>().Sessions.ToList<object>(),
                GetDisplayName = (obj) => $"{((IPlayerSession) obj).Name} ({((IPlayerSession) obj).AttachedEntity?.Name})",
                GetValueFromData = (obj) => ((IPlayerSession) obj).Name,
            };

            private readonly CommandUILineEdit _reason = new()
            {
                Name = "Reason"
            };

            private readonly CommandUILineEdit _minutes = new()
            {
                Name = "Minutes"
            };

            public override List<CommandUIControl> UI => new()
            {
                _playerDropDown,
                _reason,
                _minutes
            };

            public override void Submit()
            {
                IoCManager.Resolve<IClientConsoleHost>().ExecuteCommand($"ban \"{_playerDropDown.GetValue()}\" \"{CommandParsing.Escape(_reason.GetValue())}\" \"{_minutes.GetValue()}");
            }
        }

        private class TeleportCommandButton : UICommandButton
        {
            public override string Name => "Teleport";
            public override string RequiredCommand => "tpto";

            private readonly CommandUIDropDown _playerDropDown = new()
            {
                Name = "Player",
                GetData = () => IoCManager.Resolve<IPlayerManager>().Sessions.ToList<object>(),
                GetDisplayName = (obj) => $"{((IPlayerSession) obj).Name} ({((IPlayerSession) obj).AttachedEntity?.Name})",
                GetValueFromData = (obj) => ((IPlayerSession) obj).Name,
            };

            public override List<CommandUIControl> UI => new()
            {
                _playerDropDown
            };

            public override void Submit()
            {
                IoCManager.Resolve<IClientConsoleHost>().ExecuteCommand($"tpto \"{_playerDropDown.GetValue()}\"");
            }
        }

        private class AddAtmosCommandButton : UICommandButton
        {
            public override string Name => "Add Atmos";
            public override string RequiredCommand => "addatmos";

            private readonly CommandUIDropDown _grid = new()
            {
                Name = "Grid",
                GetData = () => IoCManager.Resolve<IMapManager>().GetAllGrids().Where(g => (int) g.Index != 0).ToList<object>(),
                GetDisplayName = (obj) => $"{((IMapGrid) obj).Index}{(IoCManager.Resolve<IPlayerManager>().LocalPlayer?.ControlledEntity?.Transform.GridID == ((IMapGrid) obj).Index ? " (Current)" : "")}",
                GetValueFromData = (obj) => ((IMapGrid) obj).Index.ToString(),
            };

            public override List<CommandUIControl> UI => new()
            {
                _grid,
            };

            public override void Submit()
            {
                IoCManager.Resolve<IClientConsoleHost>().ExecuteCommand($"addatmos {_grid.GetValue()}");
            }
        }

        private class AddGasCommandButton : UICommandButton
        {
            public override string Name => "Add Gas";
            public override string RequiredCommand => "addgas";

            private readonly CommandUIDropDown _grid = new()
            {
                Name = "Grid",
                GetData = () => IoCManager.Resolve<IMapManager>().GetAllGrids().Where(g => (int) g.Index != 0).ToList<object>(),
                GetDisplayName = (obj) => $"{((IMapGrid) obj).Index}{(IoCManager.Resolve<IPlayerManager>().LocalPlayer?.ControlledEntity?.Transform.GridID == ((IMapGrid) obj).Index ? " (Current)" : "")}",
                GetValueFromData = (obj) => ((IMapGrid) obj).Index.ToString(),
            };

            private readonly CommandUISpinBox _tileX = new()
            {
                Name = "TileX",
            };

            private readonly CommandUISpinBox _tileY = new()
            {
                Name = "TileY",
            };

            private readonly CommandUIDropDown _gas = new()
            {
                Name = "Gas",
                GetData = () =>
                {
                    var atmosSystem = EntitySystem.Get<AtmosphereSystem>();
                    return atmosSystem.Gases.ToList<object>();
                },
                GetDisplayName = (obj) => $"{((GasPrototype) obj).Name} ({((GasPrototype) obj).ID})",
                GetValueFromData = (obj) => ((GasPrototype) obj).ID.ToString(),
            };

            private readonly CommandUISpinBox _amount = new()
            {
                Name = "Amount"
            };

            public override List<CommandUIControl> UI => new()
            {
                _grid,
                _gas,
                _tileX,
                _tileY,
                _amount,
            };

            public override void Submit()
            {
                IoCManager.Resolve<IClientConsoleHost>().ExecuteCommand($"addgas {_tileX.GetValue()} {_tileY.GetValue()} {_grid.GetValue()} {_gas.GetValue()} {_amount.GetValue()}");
            }
        }

        private class FillGasCommandButton : UICommandButton
        {
            public override string Name => "Fill Gas";
            public override string RequiredCommand => "fillgas";

            private readonly CommandUIDropDown _grid = new()
            {
                Name = "Grid",
                GetData = () => IoCManager.Resolve<IMapManager>().GetAllGrids().Where(g => (int) g.Index != 0).ToList<object>(),
                GetDisplayName = (obj) => $"{((IMapGrid) obj).Index}{(IoCManager.Resolve<IPlayerManager>().LocalPlayer?.ControlledEntity?.Transform.GridID == ((IMapGrid) obj).Index ? " (Current)" : "")}",
                GetValueFromData = (obj) => ((IMapGrid) obj).Index.ToString(),
            };

            private readonly CommandUIDropDown _gas = new()
            {
                Name = "Gas",
                GetData = () =>
                {
                    var atmosSystem = EntitySystem.Get<AtmosphereSystem>();
                    return atmosSystem.Gases.ToList<object>();
                },
                GetDisplayName = (obj) => $"{((GasPrototype) obj).Name} ({((GasPrototype) obj).ID})",
                GetValueFromData = (obj) => ((GasPrototype) obj).ID.ToString(),
            };

            private readonly CommandUISpinBox _amount = new()
            {
                Name = "Amount"
            };

            public override List<CommandUIControl> UI => new()
            {
                _grid,
                _gas,
                _amount,
            };

            public override void Submit()
            {
                IoCManager.Resolve<IClientConsoleHost>().ExecuteCommand($"fillgas {_grid.GetValue()} {_gas.GetValue()} {_amount.GetValue()}");
            }
        }

        private class SetTempCommandButton : UICommandButton
        {
            public override string Name => "Set temperature";
            public override string RequiredCommand => "settemp";

            private readonly CommandUIDropDown _grid = new()
            {
                Name = "Grid",
                GetData = () => IoCManager.Resolve<IMapManager>().GetAllGrids().Where(g => (int) g.Index != 0).ToList<object>(),
                GetDisplayName = (obj) => $"{((IMapGrid) obj).Index}{(IoCManager.Resolve<IPlayerManager>().LocalPlayer?.ControlledEntity?.Transform.GridID == ((IMapGrid) obj).Index ? " (Current)" : "")}",
                GetValueFromData = (obj) => ((IMapGrid) obj).Index.ToString(),
            };

            private readonly CommandUISpinBox _tileX = new()
            {
                Name = "TileX",
            };

            private readonly CommandUISpinBox _tileY = new()
            {
                Name = "TileY",
            };

            private readonly CommandUISpinBox _temperature = new()
            {
                Name = "Temperature"
            };

            public override List<CommandUIControl> UI => new()
            {
                _grid,
                _tileX,
                _tileY,
                _temperature,
            };

            public override void Submit()
            {
                IoCManager.Resolve<IClientConsoleHost>().ExecuteCommand($"settemp {_tileX.GetValue()} {_tileY.GetValue()} {_grid.GetValue()} {_temperature.GetValue()}");
            }
        }
        #endregion

        #region CommandUIControls
        private abstract class CommandUIControl
        {
            public string? Name;
            public Control? Control;
            public abstract Control GetControl();
            public abstract string GetValue();
        }
        private class CommandUIDropDown : CommandUIControl
        {
            public Func<List<object>>? GetData;
            // The string that the player sees in the list
            public Func<object, string>? GetDisplayName;
            // The value that is given to Submit
            public Func<object, string>? GetValueFromData;
            // Cache
            protected List<object>? Data; //TODO: make this like IEnumerable or smth, so you don't have to do this ToList<object> shittery

            public override Control GetControl() //TODO: fix optionbutton being shitty after moving the window
            {
                var opt = new OptionButton { CustomMinimumSize = (100, 0), SizeFlagsHorizontal = SizeFlags.FillExpand };
                Data = GetData!();
                foreach (var item in Data)
                    opt.AddItem(GetDisplayName!(item));

                opt.OnItemSelected += eventArgs => opt.SelectId(eventArgs.Id);
                Control = opt;
                return Control;
            }

            public override string GetValue()
            {
                return GetValueFromData!(Data![((OptionButton)Control!).SelectedId]);
            }
        }
        private class CommandUICheckBox : CommandUIControl
        {
            public override Control GetControl()
            {
                Control = new CheckBox { SizeFlagsHorizontal = SizeFlags.FillExpand, SizeFlagsVertical = SizeFlags.ShrinkCenter };
                return Control;
            }

            public override string GetValue()
            {
                return ((CheckBox)Control!).Pressed ? "1" : "0";
            }
        }
        private class CommandUILineEdit : CommandUIControl
        {
            public override Control GetControl()
            {
                Control = new LineEdit { CustomMinimumSize = (100, 0), SizeFlagsHorizontal = SizeFlags.FillExpand };
                return Control;
            }

            public override string GetValue()
            {
                return ((LineEdit)Control!).Text;
            }
        }

        private class CommandUISpinBox : CommandUIControl
        {
            public override Control GetControl()
            {
                Control = new SpinBox { CustomMinimumSize = (100, 0), SizeFlagsHorizontal = SizeFlags.FillExpand };
                return Control;
            }

            public override string GetValue()
            {
                return ((SpinBox)Control!).Value.ToString();
            }
        }

        private class CommandUIButton : CommandUIControl
        {
            public Action? Handler { get; set; }

            public override Control GetControl()
            {
                Control = new Button {
                    CustomMinimumSize = (100, 0),
                    SizeFlagsHorizontal = SizeFlags.FillExpand,
                    Text = Name };
                return Control;
            }

            public override string GetValue()
            {
                return "";
            }
        }
        #endregion

        #region CommandWindow
        private class CommandWindow : SS14Window
        {
            List<CommandUIControl> _controls;
            public Action? Submit { get; set; }
            public CommandWindow(UICommandButton button)
            {
                Title = button.Name;
                _controls = button.UI;
                var container = new VBoxContainer //TODO: add margin between different controls
                {
                };
                // Init Controls in a hbox + a label
                foreach (var control in _controls)
                {
                    var c = control.GetControl();
                    if (c is Button)
                    {
                        ((Button) c).OnPressed += (args) =>
                         {
                             ((CommandUIButton) control).Handler?.Invoke();
                         };
                        container.AddChild(c);
                    }
                    else
                    {
                        var label = new Label
                        {
                            Text = control.Name,
                            CustomMinimumSize = (100, 0)
                        };
                        var divider = new Control
                        {
                            CustomMinimumSize = (50, 0)
                        };
                        var hbox = new HBoxContainer
                        {
                            Children =
                            {
                                label,
                                divider,
                                c
                            },
                        };
                        container.AddChild(hbox);
                    }


                }
                // Init Submit Button
                var submitButton = new Button
                {
                    Text = button.SubmitText ?? button.Name
                };
                submitButton.OnPressed += SubmitPressed;
                container.AddChild(submitButton);

                Contents.AddChild(container);
            }

            public void SubmitPressed(ButtonEventArgs args)
            {
                Submit?.Invoke();
            }
        }
        #endregion
    }
}
