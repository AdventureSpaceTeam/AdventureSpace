using System.Linq;
using Content.Shared.Fax;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.Fax.UI;

[GenerateTypedNameReferences]
public sealed partial class FaxWindow : DefaultWindow
{
    public event Action? SendButtonPressed;
    public event Action? RefreshButtonPressed;
    public event Action<string>? PeerSelected;

    public FaxWindow()
    {
        RobustXamlLoader.Load(this);

        SendButton.OnPressed += _ => SendButtonPressed?.Invoke();
        RefreshButton.OnPressed += _ => RefreshButtonPressed?.Invoke();
        PeerSelector.OnItemSelected += args =>
            PeerSelected?.Invoke((string) args.Button.GetItemMetadata(args.Id)!);
    }

    public void UpdateState(FaxUiState state)
    {
        SendButton.Disabled = !state.CanSend;
        FromLabel.Text = state.DeviceName;

        if (state.IsPaperInserted)
        {
            PaperStatusLabel.FontColorOverride = Color.Green;
            PaperStatusLabel.Text = Loc.GetString("fax-machine-ui-paper-inserted");
        }
        else
        {
            PaperStatusLabel.FontColorOverride = Color.Red;
            PaperStatusLabel.Text = Loc.GetString("fax-machine-ui-paper-not-inserted");
        }

        if (state.AvailablePeers.Count == 0)
        {
            PeerSelector.AddItem(Loc.GetString("fax-machine-ui-no-peers"));
            PeerSelector.Disabled = true;
        }

        if (PeerSelector.Disabled && state.AvailablePeers.Count != 0)
        {
            PeerSelector.Clear();
            PeerSelector.Disabled = false;
        }

        // always must be selected destination
        if (string.IsNullOrEmpty(state.DestinationAddress) && state.AvailablePeers.Count != 0)
        {
            PeerSelected?.Invoke(state.AvailablePeers.First().Key);
            return;
        }

        if (state.AvailablePeers.Count != 0)
        {
            PeerSelector.Clear();

            foreach (var (address, name) in state.AvailablePeers)
            {
                var id = AddPeerSelect(name, address);
                if (address == state.DestinationAddress)
                    PeerSelector.Select(id);
            }
        }
    }

    private int AddPeerSelect(string name, string address)
    {
        PeerSelector.AddItem(name);
        PeerSelector.SetItemMetadata(PeerSelector.ItemCount - 1, address);
        return PeerSelector.ItemCount - 1;
    }
}
