using System.Globalization;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Client.GameStates;
using Content.Client.Entry;

namespace Content.Client.Options.UI.Tabs
{
    [GenerateTypedNameReferences]
    public sealed partial class NetworkTab : Control
    {
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IClientGameStateManager _stateMan = default!;

        public NetworkTab()
        {

            RobustXamlLoader.Load(this);
            IoCManager.InjectDependencies(this);

            ApplyButton.OnPressed += OnApplyButtonPressed;
            ResetButton.OnPressed += OnResetButtonPressed;
            DefaultButton.OnPressed += OnDefaultButtonPressed;
            NetPredictCheckbox.OnToggled += OnPredictToggled;
            NetInterpRatioSlider.OnValueChanged += OnSliderChanged;
            NetInterpRatioSlider.MinValue = _stateMan.MinBufferSize;
            NetPredictTickBiasSlider.OnValueChanged += OnSliderChanged;
            NetPvsSpawnSlider.OnValueChanged += OnSliderChanged;
            NetPvsEntrySlider.OnValueChanged += OnSliderChanged;
            NetPvsLeaveSlider.OnValueChanged += OnSliderChanged;

            Reset();
        }

        protected override void Dispose(bool disposing)
        {
            ApplyButton.OnPressed -= OnApplyButtonPressed;
            ResetButton.OnPressed -= OnResetButtonPressed;
            DefaultButton.OnPressed -= OnDefaultButtonPressed;
            NetPredictCheckbox.OnToggled -= OnPredictToggled;
            NetInterpRatioSlider.OnValueChanged -= OnSliderChanged;
            NetPredictTickBiasSlider.OnValueChanged -= OnSliderChanged;
            NetPvsSpawnSlider.OnValueChanged -= OnSliderChanged;
            NetPvsEntrySlider.OnValueChanged -= OnSliderChanged;
            NetPvsLeaveSlider.OnValueChanged -= OnSliderChanged;
            base.Dispose(disposing);
        }

        private void OnPredictToggled(BaseButton.ButtonToggledEventArgs obj)
        {
            UpdateChanges();
        }

        private void OnSliderChanged(Robust.Client.UserInterface.Controls.Range range)
        {
            UpdateChanges();
        }

        private void OnApplyButtonPressed(BaseButton.ButtonEventArgs args)
        {
            _cfg.SetCVar(CVars.NetBufferSize, (int) NetInterpRatioSlider.Value - _stateMan.MinBufferSize);
            _cfg.SetCVar(CVars.NetPredictTickBias, (int) NetPredictTickBiasSlider.Value);
            _cfg.SetCVar(CVars.NetPVSEntityBudget, (int) NetPvsSpawnSlider.Value);
            _cfg.SetCVar(CVars.NetPVSEntityEnterBudget, (int) NetPvsEntrySlider.Value);
            _cfg.SetCVar(CVars.NetPVSEntityExitBudget, (int) NetPvsLeaveSlider.Value);
            _cfg.SetCVar(CVars.NetPredict, NetPredictCheckbox.Pressed);

            _cfg.SaveToFile();
            UpdateChanges();
        }

        private void OnResetButtonPressed(BaseButton.ButtonEventArgs args)
        {
            Reset();
        }

        private void OnDefaultButtonPressed(BaseButton.ButtonEventArgs obj)
        {
            NetPredictTickBiasSlider.Value = CVars.NetPredictTickBias.DefaultValue;
            NetPvsSpawnSlider.Value = CVars.NetPVSEntityBudget.DefaultValue;
            NetPvsEntrySlider.Value = CVars.NetPVSEntityEnterBudget.DefaultValue;
            NetPvsLeaveSlider.Value = CVars.NetPVSEntityExitBudget.DefaultValue;
            NetInterpRatioSlider.Value = CVars.NetBufferSize.DefaultValue + _stateMan.MinBufferSize;

            UpdateChanges();
        }

        private void Reset()
        {
            NetInterpRatioSlider.Value = _cfg.GetCVar(CVars.NetBufferSize) + _stateMan.MinBufferSize;
            NetPredictTickBiasSlider.Value = _cfg.GetCVar(CVars.NetPredictTickBias);
            NetPvsSpawnSlider.Value = _cfg.GetCVar(CVars.NetPVSEntityBudget);
            NetPvsEntrySlider.Value = _cfg.GetCVar(CVars.NetPVSEntityEnterBudget);
            NetPvsLeaveSlider.Value = _cfg.GetCVar(CVars.NetPVSEntityExitBudget);
            NetPredictCheckbox.Pressed = _cfg.GetCVar(CVars.NetPredict);
            UpdateChanges();
        }

        private void UpdateChanges()
        {
            var isEverythingSame =
                NetInterpRatioSlider.Value == _cfg.GetCVar(CVars.NetBufferSize) + _stateMan.MinBufferSize &&
                NetPredictTickBiasSlider.Value == _cfg.GetCVar(CVars.NetPredictTickBias) &&
                NetPredictCheckbox.Pressed == _cfg.GetCVar(CVars.NetPredict) &&
                NetPvsSpawnSlider.Value == _cfg.GetCVar(CVars.NetPVSEntityBudget) &&
                NetPvsEntrySlider.Value == _cfg.GetCVar(CVars.NetPVSEntityEnterBudget) &&
                NetPvsLeaveSlider.Value == _cfg.GetCVar(CVars.NetPVSEntityExitBudget);

            ApplyButton.Disabled = isEverythingSame;
            ResetButton.Disabled = isEverythingSame;
            NetInterpRatioLabel.Text = NetInterpRatioSlider.Value.ToString(CultureInfo.InvariantCulture);
            NetPredictTickBiasLabel.Text = NetPredictTickBiasSlider.Value.ToString(CultureInfo.InvariantCulture);
            NetPvsSpawnLabel.Text = NetPvsSpawnSlider.Value.ToString(CultureInfo.InvariantCulture);
            NetPvsEntryLabel.Text = NetPvsEntrySlider.Value.ToString(CultureInfo.InvariantCulture);
            NetPvsLeaveLabel.Text = NetPvsLeaveSlider.Value.ToString(CultureInfo.InvariantCulture);

            // TODO disable / grey-out the predict and interp sliders if prediction is disabled.
            // Currently no option to do this, but should be added to the slider control in general
        }
    }
}
