using Content.Shared.Chemistry;
using Robust.Client.AutoGenerated;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.Chemistry.UI;

[GenerateTypedNameReferences]
public sealed partial class ReagentCardControl : Control
{
    public string StorageSlotId { get; }
    public Action<string>? OnPressed;
    public Action<string>? OnEjectButtonPressed;

    public ReagentCardControl(ReagentInventoryItem item)
    {
        RobustXamlLoader.Load(this);

        StorageSlotId = item.StorageSlotId;
        ColorPanel.PanelOverride = new StyleBoxFlat { BackgroundColor = item.ReagentColor };
        ReagentNameLabel.Text = item.ReagentLabel;
        ReagentNameLabel.FontColorOverride = Color.White;
        FillLabel.Text = item.StoredAmount;
        EjectButtonIcon.Text = Loc.GetString("reagent-dispenser-window-eject-container-button");

        MainButton.OnPressed += args => OnPressed?.Invoke(StorageSlotId);
        EjectButton.OnPressed += args => OnEjectButtonPressed?.Invoke(StorageSlotId);
    }
}
