using System;
using System.Collections.Generic;
using System.Globalization;
using Content.Client.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Prototypes;
using Robust.Client.AutoGenerated;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Localization;
using Robust.Shared.Maths;

namespace Content.Client.Atmos.UI
{
    /// <summary>
    /// Client-side UI used to control a gas mixer.
    /// </summary>
    [GenerateTypedNameReferences]
    public partial class GasMixerWindow : DefaultWindow
    {
        public bool MixerStatus = true;

        public event Action? ToggleStatusButtonPressed;
        public event Action<string>? MixerOutputPressureChanged;
        public event Action<string>? MixerNodePercentageChanged;

        public bool NodeOneLastEdited = true;

        public GasMixerWindow()
        {
            RobustXamlLoader.Load(this);

            ToggleStatusButton.OnPressed += _ => SetMixerStatus(!MixerStatus);
            ToggleStatusButton.OnPressed += _ => ToggleStatusButtonPressed?.Invoke();

            MixerPressureOutputInput.OnTextChanged += _ => SetOutputPressureButton.Disabled = false;
            SetOutputPressureButton.OnPressed += _ =>
            {
                MixerOutputPressureChanged?.Invoke(MixerPressureOutputInput.Text ??= "");
                SetOutputPressureButton.Disabled = true;
            };

            SetMaxPressureButton.OnPressed += _ =>
            {
                MixerPressureOutputInput.Text = Atmospherics.MaxOutputPressure.ToString(CultureInfo.InvariantCulture);
                SetOutputPressureButton.Disabled = false;
            };

            MixerNodeOneInput.OnTextChanged += _ =>
            {
                SetMixerPercentageButton.Disabled = false;
                NodeOneLastEdited = true;
            };
            MixerNodeTwoInput.OnTextChanged += _ =>
            {
                SetMixerPercentageButton.Disabled = false;
                NodeOneLastEdited = false;
            };

            SetMixerPercentageButton.OnPressed += _ =>
            {
                MixerNodePercentageChanged?.Invoke(NodeOneLastEdited ? MixerNodeOneInput.Text ??= "" : MixerNodeTwoInput.Text ??= "");
                SetMixerPercentageButton.Disabled = true;
            };
        }

        public void SetOutputPressure(float pressure)
        {
            MixerPressureOutputInput.Text = pressure.ToString(CultureInfo.InvariantCulture);
        }

        public void SetNodePercentages(float nodeOne)
        {
            nodeOne *= 100.0f;
            MixerNodeOneInput.Text = nodeOne.ToString(CultureInfo.InvariantCulture);

            float nodeTwo = 100.0f - nodeOne;
            MixerNodeTwoInput.Text = nodeTwo.ToString(CultureInfo.InvariantCulture);
        }

        public void SetMixerStatus(bool enabled)
        {
            MixerStatus = enabled;
            if (enabled)
            {
                ToggleStatusButton.Text = Loc.GetString("comp-gas-mixer-ui-status-enabled");
            }
            else
            {
                ToggleStatusButton.Text = Loc.GetString("comp-gas-mixer-ui-status-disabled");
            }
        }
    }
}
