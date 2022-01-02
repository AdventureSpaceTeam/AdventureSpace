using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Monitor;
using Content.Shared.Atmos.Monitor.Components;
using Content.Shared.Atmos.Piping.Unary.Components;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Localization;

namespace Content.Client.Atmos.Monitor.UI.Widgets
{
    [GenerateTypedNameReferences]
    public partial class ScrubberControl : BoxContainer
    {
        private GasVentScrubberData _data;
        private string _address;

        public event Action<string, IAtmosDeviceData>? ScrubberDataChanged;

        private CheckBox _enabled => CEnableDevice;
        private CollapsibleHeading _addressLabel => CAddress;
        private OptionButton _pumpDirection => CPumpDirection;
        private FloatSpinBox _volumeRate => CVolumeRate;
        private CheckBox _wideNet => CWideNet;

        private GridContainer _gases => CGasContainer;
        private Dictionary<Gas, Button> _gasControls = new();

        public ScrubberControl(GasVentScrubberData data, string address)
        {
            RobustXamlLoader.Load(this);

            this.Name = address;

            _data = data;
            _address = address;

            _addressLabel.Title = Loc.GetString("air-alarm-ui-atmos-net-device-label", ("address", $"{address}"));

            _enabled.Pressed = data.Enabled;
            _enabled.OnToggled += _ =>
            {
                _data.Enabled = _enabled.Pressed;
                ScrubberDataChanged?.Invoke(_address, _data);
            };

            _wideNet.Pressed = data.WideNet;
            _wideNet.OnToggled += _ =>
            {
                _data.WideNet = _wideNet.Pressed;
                ScrubberDataChanged?.Invoke(_address, _data);
            };

            _volumeRate.Value = (float) _data.VolumeRate!;
            _volumeRate.OnValueChanged += _ =>
            {
                _data.VolumeRate = _volumeRate.Value;
                ScrubberDataChanged?.Invoke(_address, _data);
            };
            _volumeRate.IsValid += value => value >= 0;

            foreach (var value in Enum.GetValues<ScrubberPumpDirection>())
                _pumpDirection.AddItem(Loc.GetString($"{value}"), (int) value);

            _pumpDirection.SelectId((int) _data.PumpDirection!);
            _pumpDirection.OnItemSelected += args =>
            {
                _pumpDirection.SelectId(args.Id);
                _data.PumpDirection = (ScrubberPumpDirection) args.Id;
                ScrubberDataChanged?.Invoke(_address, _data);
            };

            foreach (var value in Enum.GetValues<Gas>())
            {
                var gasButton = new Button
                {
                    Name = value.ToString(),
                    Text = Loc.GetString($"{value}"),
                    ToggleMode = true,
                    HorizontalExpand = true,
                    Pressed = _data.FilterGases!.Contains(value)
                };
                gasButton.OnToggled += args =>
                {
                    if (args.Pressed)
                        _data.FilterGases.Add(value);
                    else
                        _data.FilterGases.Remove(value);

                    ScrubberDataChanged?.Invoke(_address, _data);
                };
                _gasControls.Add(value, gasButton);
                _gases.AddChild(gasButton);
            }

        }

        public void ChangeData(GasVentScrubberData data)
        {
            _data.Enabled = data.Enabled;
            _enabled.Pressed = _data.Enabled;

            _data.PumpDirection = data.PumpDirection;
            _pumpDirection.SelectId((int) _data.PumpDirection!);

            _data.VolumeRate = data.VolumeRate;
            _volumeRate.Value = (float) _data.VolumeRate!;

            _data.WideNet = data.WideNet;
            _wideNet.Pressed = _data.WideNet;

            var intersect = _data.FilterGases!.Intersect(data.FilterGases!);

            foreach (var value in Enum.GetValues<Gas>())
                if (!intersect.Contains(value))
                    _gasControls[value].Pressed = false;
        }
    }
}
