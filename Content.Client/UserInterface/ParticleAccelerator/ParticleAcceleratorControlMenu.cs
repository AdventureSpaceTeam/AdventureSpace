﻿using System;
using Content.Client.Animations;
using Content.Client.UserInterface;
using Content.Client.UserInterface.Stylesheets;
using Content.Client.Utility;
using Content.Shared.GameObjects.Components;
using Robust.Client.Animations;
using Robust.Client.Graphics;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Graphics.Shaders;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Noise;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.ParticleAccelerator
{
    public sealed class ParticleAcceleratorControlMenu : BaseWindow
    {
        private readonly ShaderInstance _greyScaleShader;

        private readonly ParticleAcceleratorBoundUserInterface Owner;

        private readonly Label _drawLabel;
        private readonly NoiseGenerator _drawNoiseGenerator;
        private readonly Button _onButton;
        private readonly Button _offButton;
        private readonly Button _scanButton;
        private readonly Label _statusLabel;
        private readonly SpinBox _stateSpinBox;

        private readonly VBoxContainer _alarmControl;
        private readonly Animation _alarmControlAnimation;

        private readonly PASegmentControl _endCapTexture;
        private readonly PASegmentControl _fuelChamberTexture;
        private readonly PASegmentControl _controlBoxTexture;
        private readonly PASegmentControl _powerBoxTexture;
        private readonly PASegmentControl _emitterCenterTexture;
        private readonly PASegmentControl _emitterRightTexture;
        private readonly PASegmentControl _emitterLeftTexture;

        private float _time;
        private int _lastDraw;
        private int _lastReceive;

        private bool _blockSpinBox;
        private bool _assembled;
        private bool _shouldContinueAnimating;

        public ParticleAcceleratorControlMenu(ParticleAcceleratorBoundUserInterface owner)
        {
            _greyScaleShader = IoCManager.Resolve<IPrototypeManager>().Index<ShaderPrototype>("Greyscale").Instance();

            Owner = owner;
            _drawNoiseGenerator = new NoiseGenerator(NoiseGenerator.NoiseType.Fbm);
            _drawNoiseGenerator.SetFrequency(0.5f);

            var resourceCache = IoCManager.Resolve<IResourceCache>();
            var font = resourceCache.GetFont("/Fonts/Boxfont-round/Boxfont Round.ttf", 13);
            var panelTex = resourceCache.GetTexture("/Textures/Interface/Nano/button.svg.96dpi.png");

            MouseFilter = MouseFilterMode.Stop;

            _alarmControlAnimation = new Animation
            {
                Length = TimeSpan.FromSeconds(1),
                AnimationTracks =
                {
                    new AnimationTrackControlProperty
                    {
                        Property = nameof(Control.Visible),
                        KeyFrames =
                        {
                            new AnimationTrackProperty.KeyFrame(true, 0),
                            new AnimationTrackProperty.KeyFrame(false, 0.75f),
                        }
                    }
                }
            };

            var back = new StyleBoxTexture
            {
                Texture = panelTex,
                Modulate = Color.FromHex("#25252A"),
            };
            back.SetPatchMargin(StyleBox.Margin.All, 10);

            var back2 = new StyleBoxTexture(back)
            {
                Modulate = Color.FromHex("#202023")
            };

            AddChild(new PanelContainer
            {
                PanelOverride = back,
                MouseFilter = MouseFilterMode.Pass
            });

            _stateSpinBox = new SpinBox
            {
                Value = 0,
            };
            _stateSpinBox.IsValid = StrengthSpinBoxValid;
            _stateSpinBox.InitDefaultButtons();
            _stateSpinBox.ValueChanged += PowerStateChanged;
            _stateSpinBox.LineEditDisabled = true;

            _offButton = new Button
            {
                ToggleMode = false,
                Text = "Off",
                StyleClasses = {StyleBase.ButtonOpenRight},
            };
            _offButton.OnPressed += args => owner.SendEnableMessage(false);

            _onButton = new Button
            {
                ToggleMode = false,
                Text = "On",
                StyleClasses = {StyleBase.ButtonOpenLeft},
            };
            _onButton.OnPressed += args => owner.SendEnableMessage(true);

            var closeButton = new TextureButton
            {
                StyleClasses = {"windowCloseButton"},
                SizeFlagsHorizontal = SizeFlags.ShrinkEnd
            };
            closeButton.OnPressed += args => Close();

            var serviceManual = new Label
            {
                SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
                StyleClasses = {StyleBase.StyleClassLabelSubText},
                Text = Loc.GetString("Refer to p.132 of service manual")
            };
            _drawLabel = new Label();
            var imgSize = new Vector2(32, 32);
            AddChild(new VBoxContainer
            {
                Children =
                {
                    new MarginContainer
                    {
                        MarginLeftOverride = 2,
                        MarginTopOverride = 2,
                        Children =
                        {
                            new Label
                            {
                                Text = Loc.GetString("Mark 2 Particle Accelerator"),
                                FontOverride = font,
                                FontColorOverride = StyleNano.NanoGold,
                            },
                            new MarginContainer
                            {
                                MarginRightOverride = 8,
                                Children =
                                {
                                    closeButton
                                }
                            }
                        }
                    },
                    new PanelContainer
                    {
                        PanelOverride = new StyleBoxFlat {BackgroundColor = StyleNano.NanoGold},
                        CustomMinimumSize = (0, 2),
                    },
                    new Control
                    {
                        CustomMinimumSize = (0, 4)
                    },

                    new HBoxContainer
                    {
                        SizeFlagsVertical = SizeFlags.FillExpand,
                        Children =
                        {
                            new MarginContainer
                            {
                                MarginLeftOverride = 4,
                                Children =
                                {
                                    new VBoxContainer
                                    {
                                        SizeFlagsHorizontal = SizeFlags.FillExpand,
                                        Children =
                                        {
                                            new HBoxContainer
                                            {
                                                Children =
                                                {
                                                    new Label
                                                    {
                                                        Text = Loc.GetString("Power: "),
                                                        SizeFlagsHorizontal = SizeFlags.Expand
                                                    },
                                                    _offButton,
                                                    _onButton
                                                }
                                            },
                                            new HBoxContainer
                                            {
                                                Children =
                                                {
                                                    new Label
                                                    {
                                                        Text = Loc.GetString("Strength: "),
                                                        SizeFlagsHorizontal = SizeFlags.Expand
                                                    },
                                                    _stateSpinBox
                                                }
                                            },
                                            new Control
                                            {
                                                CustomMinimumSize = (0, 10),
                                            },
                                            _drawLabel,
                                            new Control
                                            {
                                                SizeFlagsVertical = SizeFlags.Expand
                                            },
                                            (_alarmControl = new VBoxContainer
                                            {
                                                Children =
                                                {
                                                    new Label
                                                    {
                                                        Text = Loc.GetString("PARTICLE STRENGTH\nLIMITER FAILURE"),
                                                        FontColorOverride = Color.Red,
                                                        Align = Label.AlignMode.Center
                                                    },
                                                    serviceManual
                                                }
                                            }),
                                        }
                                    }
                                }
                            },
                            new VBoxContainer
                            {
                                SizeFlagsHorizontal = SizeFlags.FillExpand,
                                Children =
                                {
                                    (_statusLabel = new Label
                                    {
                                        SizeFlagsHorizontal = SizeFlags.ShrinkCenter
                                    }),
                                    new Control
                                    {
                                        CustomMinimumSize = (0, 20)
                                    },
                                    new PanelContainer
                                    {
                                        SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
                                        PanelOverride = back2,
                                        Children =
                                        {
                                            new GridContainer
                                            {
                                                Columns = 3,
                                                VSeparationOverride = 0,
                                                HSeparationOverride = 0,
                                                Children =
                                                {
                                                    new Control {CustomMinimumSize = imgSize},
                                                    (_endCapTexture = Segment("end_cap")),
                                                    new Control {CustomMinimumSize = imgSize},
                                                    (_controlBoxTexture = Segment("control_box")),
                                                    (_fuelChamberTexture = Segment("fuel_chamber")),
                                                    new Control {CustomMinimumSize = imgSize},
                                                    new Control {CustomMinimumSize = imgSize},
                                                    (_powerBoxTexture = Segment("power_box")),
                                                    new Control {CustomMinimumSize = imgSize},
                                                    (_emitterLeftTexture = Segment("emitter_left")),
                                                    (_emitterCenterTexture = Segment("emitter_center")),
                                                    (_emitterRightTexture = Segment("emitter_right")),
                                                }
                                            }
                                        }
                                    },
                                    (_scanButton = new Button
                                    {
                                        Text = Loc.GetString("Scan Parts"),
                                        SizeFlagsHorizontal = SizeFlags.ShrinkCenter
                                    })
                                }
                            }
                        }
                    },
                    new StripeBack
                    {
                        Children =
                        {
                            new MarginContainer
                            {
                                MarginLeftOverride = 4,
                                MarginTopOverride = 4,
                                MarginBottomOverride = 4,
                                Children =
                                {
                                    new Label
                                    {
                                        Text = Loc.GetString("Ensure containment field is active before operation"),
                                        SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
                                        StyleClasses = {StyleBase.StyleClassLabelSubText},
                                    }
                                }
                            }
                        }
                    },
                    new MarginContainer
                    {
                        MarginLeftOverride = 12,
                        Children =
                        {
                            new HBoxContainer
                            {
                                Children =
                                {
                                    new Label
                                    {
                                        Text = "FOO-BAR-BAZ",
                                        StyleClasses = {StyleBase.StyleClassLabelSubText}
                                    }
                                }
                            }
                        }
                    },
                }
            });

            _scanButton.OnPressed += args => Owner.SendScanPartsMessage();

            _alarmControl.AnimationCompleted += s =>
            {
                if (_shouldContinueAnimating)
                {
                    _alarmControl.PlayAnimation(_alarmControlAnimation, "warningAnim");
                }
                else
                {
                    _alarmControl.Visible = false;
                }
            };

            PASegmentControl Segment(string name)
            {
                return new(this, resourceCache, name);
            }
        }

        private bool StrengthSpinBoxValid(int n)
        {
            return (n >= 0 && n <= 4 && !_blockSpinBox);
        }

        private void PowerStateChanged(object sender, ValueChangedEventArgs e)
        {
            ParticleAcceleratorPowerState newState;
            switch (e.Value)
            {
                case 0:
                    newState = ParticleAcceleratorPowerState.Standby;
                    break;
                case 1:
                    newState = ParticleAcceleratorPowerState.Level0;
                    break;
                case 2:
                    newState = ParticleAcceleratorPowerState.Level1;
                    break;
                case 3:
                    newState = ParticleAcceleratorPowerState.Level2;
                    break;
                case 4:
                    newState = ParticleAcceleratorPowerState.Level3;
                    break;
                default:
                    return;
            }

            Owner.SendPowerStateMessage(newState);
        }

        protected override DragMode GetDragModeFor(Vector2 relativeMousePos)
        {
            return DragMode.Move;
        }

        protected override Vector2 CalculateMinimumSize()
        {
            return (400, 300);
        }

        public void DataUpdate(ParticleAcceleratorUIState uiState)
        {
            _assembled = uiState.Assembled;
            UpdateUI(uiState.Assembled, uiState.InterfaceBlock, uiState.Enabled,
                uiState.WirePowerBlock);
            _statusLabel.Text = Loc.GetString($"Status: {(uiState.Assembled ? "Operational" : "Incomplete")}");
            UpdatePowerState(uiState.State, uiState.Enabled, uiState.Assembled,
                uiState.MaxLevel);
            UpdatePreview(uiState);
            _lastDraw = uiState.PowerDraw;
            _lastReceive = uiState.PowerReceive;
        }

        private void UpdatePowerState(ParticleAcceleratorPowerState state, bool enabled, bool assembled,
            ParticleAcceleratorPowerState maxState)
        {
            _stateSpinBox.OverrideValue(state switch
            {
                ParticleAcceleratorPowerState.Standby => 0,
                ParticleAcceleratorPowerState.Level0 => 1,
                ParticleAcceleratorPowerState.Level1 => 2,
                ParticleAcceleratorPowerState.Level2 => 3,
                ParticleAcceleratorPowerState.Level3 => 4,
                _ => 0
            });


            _shouldContinueAnimating = false;
            _alarmControl.StopAnimation("warningAnim");
            _alarmControl.Visible = false;
            if (maxState == ParticleAcceleratorPowerState.Level3 && enabled == true && assembled == true)
            {
                _shouldContinueAnimating = true;
                _alarmControl.PlayAnimation(_alarmControlAnimation, "warningAnim");
            }
        }

        private void UpdateUI(bool assembled, bool blocked, bool enabled, bool powerBlock)
        {
            _onButton.Pressed = enabled;
            _offButton.Pressed = !enabled;

            var cantUse = !assembled || blocked || powerBlock;
            _onButton.Disabled = cantUse;
            _offButton.Disabled = cantUse;
            _scanButton.Disabled = blocked;

            var cantChangeLevel = !assembled || blocked;
            _stateSpinBox.SetButtonDisabled(cantChangeLevel);
            _blockSpinBox = cantChangeLevel;
        }

        private void UpdatePreview(ParticleAcceleratorUIState updateMessage)
        {
            _endCapTexture.SetPowerState(updateMessage, updateMessage.EndCapExists);
            _fuelChamberTexture.SetPowerState(updateMessage, updateMessage.FuelChamberExists);
            _controlBoxTexture.SetPowerState(updateMessage, true);
            _powerBoxTexture.SetPowerState(updateMessage, updateMessage.PowerBoxExists);
            _emitterCenterTexture.SetPowerState(updateMessage, updateMessage.EmitterCenterExists);
            _emitterLeftTexture.SetPowerState(updateMessage, updateMessage.EmitterLeftExists);
            _emitterRightTexture.SetPowerState(updateMessage, updateMessage.EmitterRightExists);
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);

            if (!_assembled)
            {
                _drawLabel.Text = Loc.GetString("Draw: N/A");
                return;
            }

            _time += args.DeltaSeconds;

            var watts = 0;
            if (_lastDraw != 0)
            {
                var val = _drawNoiseGenerator.GetNoise(_time);
                watts = (int) (_lastDraw + val * 5);
            }

            _drawLabel.Text = Loc.GetString("Draw: {0:##,##0}/{1:##,##0} W", watts, _lastReceive);
        }

        private sealed class PASegmentControl : Control
        {
            private readonly ParticleAcceleratorControlMenu _menu;
            private readonly string _baseState;
            private readonly TextureRect _base;
            private readonly TextureRect _unlit;
            private readonly RSI _rsi;

            public PASegmentControl(ParticleAcceleratorControlMenu menu, IResourceCache cache, string name)
            {
                _menu = menu;
                _baseState = name;
                _rsi = cache.GetResource<RSIResource>($"/Textures/Constructible/Power/PA/{name}.rsi").RSI;

                AddChild(_base = new TextureRect {Texture = _rsi[$"{name}c"].Frame0});
                AddChild(_unlit = new TextureRect());
            }

            public void SetPowerState(ParticleAcceleratorUIState state, bool exists)
            {
                _base.ShaderOverride = exists ? null : _menu._greyScaleShader;
                _base.ModulateSelfOverride = exists ? (Color?)null : new Color(127, 127, 127);

                if (!state.Enabled || !exists)
                {
                    _unlit.Visible = false;
                    return;
                }

                _unlit.Visible = true;

                var suffix = state.State switch
                {
                    ParticleAcceleratorPowerState.Standby => "_unlitp",
                    ParticleAcceleratorPowerState.Level0 => "_unlitp0",
                    ParticleAcceleratorPowerState.Level1 => "_unlitp1",
                    ParticleAcceleratorPowerState.Level2 => "_unlitp2",
                    ParticleAcceleratorPowerState.Level3 => "_unlitp3",
                    _ => ""
                };

                if (!_rsi.TryGetState(_baseState + suffix, out var rState))
                {
                    _unlit.Visible = false;
                    return;
                }

                _unlit.Texture = rState.Frame0;
            }

            protected override Vector2 CalculateMinimumSize()
            {
                return _rsi.Size;
            }
        }
    }
}
