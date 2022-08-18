using System;
using System.Collections.Generic;
using Content.Client.Atmos.Monitor.UI.Widgets;
using Content.Client.Message;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Monitor;
using Content.Shared.Atmos.Monitor.Components;
using Content.Shared.Atmos.Piping.Unary.Components;
using Content.Shared.Temperature;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Localization;

namespace Content.Client.Atmos.Monitor.UI
{
    [GenerateTypedNameReferences]
    public sealed partial class AirAlarmWindow : DefaultWindow
    {
        public event Action<string, IAtmosDeviceData>? AtmosDeviceDataChanged;
        public event Action<string, AtmosMonitorThresholdType, AtmosAlarmThreshold, Gas?>? AtmosAlarmThresholdChanged;
        public event Action<AirAlarmMode>? AirAlarmModeChanged;
        public event Action<string>? ResyncDeviceRequested;
        public event Action? ResyncAllRequested;
        public event Action<AirAlarmTab>? AirAlarmTabChange;

        private Label _address => CDeviceAddress;
        private Label _deviceTotal => CDeviceTotal;
        private RichTextLabel _pressure => CPressureLabel;
        private RichTextLabel _temperature => CTemperatureLabel;
        private RichTextLabel _alarmState => CStatusLabel;

        private TabContainer _tabContainer => CTabContainer;
        private BoxContainer _ventDevices => CVentContainer;
        private BoxContainer _scrubberDevices => CScrubberContainer;

        private Dictionary<string, PumpControl> _pumps = new();
        private Dictionary<string, ScrubberControl> _scrubbers = new();
        private Button _resyncDevices => CResyncButton;

        private ThresholdControl? _pressureThresholdControl;
        private ThresholdControl? _temperatureThresholdControl;
        private Dictionary<Gas, ThresholdControl> _gasThresholdControls = new();

        private Dictionary<Gas, Label> _gasLabels = new();

        private OptionButton _modes => CModeButton;

        public AirAlarmWindow()
        {
            RobustXamlLoader.Load(this);

            foreach (var mode in Enum.GetValues<AirAlarmMode>())
                _modes.AddItem($"{mode}", (int) mode);

            _modes.OnItemSelected += args =>
            {
                _modes.SelectId(args.Id);
                AirAlarmModeChanged!.Invoke((AirAlarmMode) args.Id);
            };

            /*
            foreach (var gas in Enum.GetValues<Gas>())
            {
                var gasLabel = new Label();
                _gasReadout.AddChild(gasLabel);
                _gasLabels.Add(gas, gasLabel);
            }
            */

            _tabContainer.SetTabTitle(0, Loc.GetString("air-alarm-ui-window-tab-vents"));
            _tabContainer.SetTabTitle(1, Loc.GetString("air-alarm-ui-window-tab-scrubbers"));
            _tabContainer.SetTabTitle(2, Loc.GetString("air-alarm-ui-window-tab-sensors"));

            _tabContainer.OnTabChanged += idx =>
            {
                AirAlarmTabChange!((AirAlarmTab) idx);
            };

            _resyncDevices.OnPressed += _ =>
            {
                _ventDevices.RemoveAllChildren();
                _pumps.Clear();
                _scrubberDevices.RemoveAllChildren();
                _scrubbers.Clear();
                ResyncAllRequested!.Invoke();
            };
        }

        public void UpdateState(AirAlarmUIState state)
        {
            _pressure.SetMarkup(Loc.GetString("air-alarm-ui-window-pressure", ("pressure", $"{state.PressureAverage:0.##}")));
            _temperature.SetMarkup(Loc.GetString("air-alarm-ui-window-temperature", ("tempC", $"{TemperatureHelpers.KelvinToCelsius(state.TemperatureAverage):0.#}"), ("temperature", $"{state.TemperatureAverage:0.##}")));
            _alarmState.SetMarkup(Loc.GetString("air-alarm-ui-window-alarm-state", ("state", $"{state.AlarmType}")));
            UpdateModeSelector(state.Mode);
            foreach (var (addr, dev) in state.DeviceData)
            {
                UpdateDeviceData(addr, dev);
            }

            _tabContainer.CurrentTab = (int) state.Tab;
        }

        public void SetAddress(string address)
        {
            _address.Text = address;
        }

        /*
        public void UpdateGasData(ref AirAlarmAirData state)
        {
            _pressure.SetMarkup(Loc.GetString("air-alarm-ui-window-pressure", ("pressure", $"{state.Pressure:0.##}")));
            _temperature.SetMarkup(Loc.GetString("air-alarm-ui-window-temperature", ("tempC", $"{TemperatureHelpers.KelvinToCelsius(state.Temperature ?? 0):0.#}"), ("temperature", $"{state.Temperature:0.##}")));
            _alarmState.SetMarkup(Loc.GetString("air-alarm-ui-window-alarm-state", ("state", $"{state.AlarmState}")));

            if (state.Gases != null)
                foreach (var (gas, amount) in state.Gases)
                    _gasLabels[gas].Text = Loc.GetString("air-alarm-ui-gases", ("gas", $"{gas}"), ("amount", $"{amount:0.####}"), ("percentage", $"{(amount / state.TotalMoles):0.##}"));
        }
        */

        public void UpdateModeSelector(AirAlarmMode mode)
        {
            _modes.SelectId((int) mode);
        }

        public void UpdateDeviceData(string addr, IAtmosDeviceData device)
        {
            switch (device)
            {
                case GasVentPumpData pump:
                    if (!_pumps.TryGetValue(addr, out var pumpControl))
                    {
                        var control= new PumpControl(pump, addr);
                        control.PumpDataChanged += AtmosDeviceDataChanged!.Invoke;
                        _pumps.Add(addr, control);
                        CVentContainer.AddChild(control);
                    }
                    else
                    {
                        pumpControl.ChangeData(pump);
                    }

                    break;
                case GasVentScrubberData scrubber:
                    if (!_scrubbers.TryGetValue(addr, out var scrubberControl))
                    {
                        var control = new ScrubberControl(scrubber, addr);
                        control.ScrubberDataChanged += AtmosDeviceDataChanged!.Invoke;
                        _scrubbers.Add(addr, control);
                        CScrubberContainer.AddChild(control);
                    }
                    else
                    {
                        scrubberControl.ChangeData(scrubber);
                    }

                    break;
            }

            _deviceTotal.Text = $"{_pumps.Count + _scrubbers.Count}";
        }

        /*
        public void UpdateThreshold(ref AirAlarmUpdateAlarmThresholdMessage message)
        {
            switch (message.Type)
            {
                case AtmosMonitorThresholdType.Pressure:
                    if (_pressureThresholdControl == null)
                    {
                        _pressureThresholdControl = new ThresholdControl(Loc.GetString("air-alarm-ui-thresholds-pressure-title"), message.Threshold, message.Type);
                        _pressureThresholdControl.ThresholdDataChanged += AtmosAlarmThresholdChanged!.Invoke;
                        _pressureThreshold.AddChild(_pressureThresholdControl);
                    }
                    else
                    {
                        _pressureThresholdControl.UpdateThresholdData(message.Threshold);
                    }

                    break;
                case AtmosMonitorThresholdType.Temperature:
                    if (_temperatureThresholdControl == null)
                    {
                        _temperatureThresholdControl = new ThresholdControl(Loc.GetString("air-alarm-ui-thresholds-temperature-title"), message.Threshold, message.Type);
                        _temperatureThresholdControl.ThresholdDataChanged += AtmosAlarmThresholdChanged!.Invoke;
                        _temperatureThreshold.AddChild(_temperatureThresholdControl);
                    }
                    else
                    {
                        _temperatureThresholdControl.UpdateThresholdData(message.Threshold);
                    }

                    break;
                case AtmosMonitorThresholdType.Gas:
                    if (_gasThresholdControls.TryGetValue((Gas) message.Gas!, out var control))
                    {
                        control.UpdateThresholdData(message.Threshold);
                        break;
                    }

                    var gasThreshold = new ThresholdControl(Loc.GetString($"air-alarm-ui-thresholds-gas-title", ("gas", $"{(Gas) message.Gas!}")), message.Threshold, AtmosMonitorThresholdType.Gas, (Gas) message.Gas!, 100);
                    gasThreshold.ThresholdDataChanged += AtmosAlarmThresholdChanged!.Invoke;
                    _gasThresholdControls.Add((Gas) message.Gas!, gasThreshold);
                    _gasThreshold.AddChild(gasThreshold);

                    break;
            }
        }
        */
    }
}
