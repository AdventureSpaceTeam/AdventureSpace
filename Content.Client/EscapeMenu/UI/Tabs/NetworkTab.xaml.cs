using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Client.GameStates;
using Content.Client.Entry;

namespace Content.Client.EscapeMenu.UI.Tabs
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
            NetInterpRatioSlider.OnValueChanged += OnSliderChanged;
            NetInterpRatioSlider.MinValue = _stateMan.MinBufferSize;
            NetPredictTickBiasSlider.OnValueChanged += OnSliderChanged;
            NetPvsEntrySlider.OnValueChanged += OnSliderChanged;
            NetPvsLeaveSlider.OnValueChanged += OnSliderChanged;

            Reset();
        }

        protected override void Dispose(bool disposing)
        {
            ApplyButton.OnPressed -= OnApplyButtonPressed;
            ResetButton.OnPressed -= OnResetButtonPressed;
            DefaultButton.OnPressed -= OnDefaultButtonPressed;
            NetInterpRatioSlider.OnValueChanged -= OnSliderChanged;
            NetPredictTickBiasSlider.OnValueChanged -= OnSliderChanged;
            NetPvsEntrySlider.OnValueChanged -= OnSliderChanged;
            NetPvsLeaveSlider.OnValueChanged -= OnSliderChanged;
            base.Dispose(disposing);
        }

        private void OnSliderChanged(Robust.Client.UserInterface.Controls.Range range)
        {
            UpdateChanges();
        }

        private void OnApplyButtonPressed(BaseButton.ButtonEventArgs args)
        {
            _cfg.SetCVar(CVars.NetBufferSize, (int) NetInterpRatioSlider.Value - _stateMan.MinBufferSize);
            _cfg.SetCVar(CVars.NetPredictTickBias, (int) NetPredictTickBiasSlider.Value);
            _cfg.SetCVar(CVars.NetPVSEntityBudget, (int) NetPvsEntrySlider.Value);
            _cfg.SetCVar(CVars.NetPVSEntityExitBudget, (int) NetPvsLeaveSlider.Value);

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
            NetPvsEntrySlider.Value = CVars.NetPVSEntityBudget.DefaultValue;
            NetPvsLeaveSlider.Value = CVars.NetPVSEntityExitBudget.DefaultValue;

            // Apparently default value doesn't get updated when using override defaults, so using a const
            NetInterpRatioSlider.Value = EntryPoint.NetBufferSizeOverride + _stateMan.MinBufferSize;

            UpdateChanges();
        }

        private void Reset()
        {
            NetInterpRatioSlider.Value = _cfg.GetCVar(CVars.NetBufferSize) + _stateMan.MinBufferSize;
            NetPredictTickBiasSlider.Value = _cfg.GetCVar(CVars.NetPredictTickBias);
            NetPvsEntrySlider.Value = _cfg.GetCVar(CVars.NetPVSEntityBudget);
            NetPvsLeaveSlider.Value = _cfg.GetCVar(CVars.NetPVSEntityExitBudget);
            UpdateChanges();
        }

        private void UpdateChanges()
        {
            var isEverythingSame =
                NetInterpRatioSlider.Value == _cfg.GetCVar(CVars.NetBufferSize) + _stateMan.MinBufferSize &&
                NetPredictTickBiasSlider.Value == _cfg.GetCVar(CVars.NetPredictTickBias) &&
                NetPvsEntrySlider.Value == _cfg.GetCVar(CVars.NetPVSEntityBudget) &&
                NetPvsLeaveSlider.Value == _cfg.GetCVar(CVars.NetPVSEntityExitBudget);

            ApplyButton.Disabled = isEverythingSame;
            ResetButton.Disabled = isEverythingSame;
            NetInterpRatioLabel.Text = NetInterpRatioSlider.Value.ToString();
            NetPredictTickBiasLabel.Text = NetPredictTickBiasSlider.Value.ToString();
            NetPvsEntryLabel.Text = NetPvsEntrySlider.Value.ToString();
            NetPvsLeaveLabel.Text = NetPvsLeaveSlider.Value.ToString();
        }
    }
}
