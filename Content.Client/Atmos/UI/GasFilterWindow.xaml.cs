using System;
using System.Collections.Generic;
using System.Globalization;
using Content.Shared.Atmos.Prototypes;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Localization;

namespace Content.Client.Atmos.UI
{
    /// <summary>
    /// Client-side UI used to control a gas filter.
    /// </summary>
    [GenerateTypedNameReferences]
    public partial class GasFilterWindow : SS14Window
    {
        private readonly ButtonGroup _buttonGroup = new();

        public bool FilterStatus = true;
        public string? SelectedGas;
        public string? CurrentGasId;

        public event Action? ToggleStatusButtonPressed;
        public event Action<string>? FilterTransferRateChanged;
        public event Action? SelectGasPressed;

        public GasFilterWindow(IEnumerable<GasPrototype> gases)
        {
            RobustXamlLoader.Load(this);
            PopulateGasList(gases);

            ToggleStatusButton.OnPressed += _ => SetFilterStatus(!FilterStatus);
            ToggleStatusButton.OnPressed += _ => ToggleStatusButtonPressed?.Invoke();

            FilterTransferRateInput.OnTextChanged += _ => SetFilterRate.Disabled = false;
            SetFilterRate.OnPressed += _ =>
            {
                FilterTransferRateChanged?.Invoke(FilterTransferRateInput.Text ??= "");
                SetFilterRate.Disabled = true;
            };

            SelectGasButton.OnPressed += _ => SelectGasPressed?.Invoke();

            GasList.OnItemSelected += GasListOnItemSelected;
            GasList.OnItemDeselected += GasListOnItemDeselected;
        }

        public void SetTransferRate(float rate)
        {
            FilterTransferRateInput.Text = rate.ToString(CultureInfo.InvariantCulture);
        }

        public void SetFilterStatus(bool enabled)
        {
            FilterStatus = enabled;
            if (enabled)
            {
                ToggleStatusButton.Text = Loc.GetString("comp-gas-filter-ui-status-enabled");
            }
            else
            {
                ToggleStatusButton.Text = Loc.GetString("comp-gas-filter-ui-status-disabled");
            }
        }

        public void SetGasFiltered(string? id, string name)
        {
            CurrentGasId = id;
            CurrentGasLabel.Text = Loc.GetString("comp-gas-filter-ui-filter-gas-current") + $" {name}";
            GasList.ClearSelected();
            SelectGasButton.Disabled = true;
        }

        private void PopulateGasList(IEnumerable<GasPrototype> gases)
        {
            foreach (GasPrototype gas in gases)
            {
                GasList.Add(GetGasItem(gas.ID, gas.Name, GasList));
            }
        }

        private static ItemList.Item GetGasItem(string id, string name, ItemList itemList)
        {
            return new(itemList)
            {
                Metadata = id,
                Text = name
            };
        }

        private void GasListOnItemSelected(ItemList.ItemListSelectedEventArgs obj)
        {
            SelectedGas = (string) obj.ItemList[obj.ItemIndex].Metadata!;
            if(SelectedGas != CurrentGasId) SelectGasButton.Disabled = false;
        }

        private void GasListOnItemDeselected(ItemList.ItemListDeselectedEventArgs obj)
        {
            SelectedGas = CurrentGasId;
            SelectGasButton.Disabled = true;
        }
    }
}
