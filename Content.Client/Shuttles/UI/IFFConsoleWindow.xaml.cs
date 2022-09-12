using Content.Client.Computer;
using Content.Client.UserInterface.Controls;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Shuttles.Components;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.Shuttles.UI;

[GenerateTypedNameReferences]
public sealed partial class IFFConsoleWindow : FancyWindow,
    IComputerWindow<IFFConsoleBoundUserInterfaceState>
{
    private readonly ButtonGroup _showIFFButtonGroup = new();
    private readonly ButtonGroup _showVesselButtonGroup = new();
    public event Action<bool>? ShowIFF;
    public event Action<bool>? ShowVessel;

    public IFFConsoleWindow()
    {
        RobustXamlLoader.Load(this);
        ShowIFFOffButton.Group = _showIFFButtonGroup;
        ShowIFFOnButton.Group = _showIFFButtonGroup;
        ShowIFFOnButton.OnPressed += args => ShowIFFPressed(true);
        ShowIFFOffButton.OnPressed += args => ShowIFFPressed(false);

        ShowVesselOffButton.Group = _showVesselButtonGroup;
        ShowVesselOnButton.Group = _showVesselButtonGroup;
        ShowVesselOnButton.OnPressed += args => ShowVesselPressed(true);
        ShowVesselOffButton.OnPressed += args => ShowVesselPressed(false);
    }

    private void ShowIFFPressed(bool pressed)
    {
        ShowIFF?.Invoke(pressed);
    }

    private void ShowVesselPressed(bool pressed)
    {
        ShowVessel?.Invoke(pressed);
    }

    public void UpdateState(IFFConsoleBoundUserInterfaceState state)
    {
        if ((state.AllowedFlags & IFFFlags.HideLabel) != 0x0)
        {
            ShowIFFOffButton.Disabled = false;
            ShowIFFOnButton.Disabled = false;

            if ((state.Flags & IFFFlags.HideLabel) != 0x0)
            {
                ShowIFFOffButton.Pressed = true;
            }
            else
            {
                ShowIFFOnButton.Pressed = true;
            }
        }
        else
        {
            ShowIFFOffButton.Disabled = true;
            ShowIFFOnButton.Disabled = true;
        }

        if ((state.AllowedFlags & IFFFlags.Hide) != 0x0)
        {
            ShowVesselOffButton.Disabled = false;
            ShowVesselOnButton.Disabled = false;

            if ((state.Flags & IFFFlags.Hide) != 0x0)
            {
                ShowVesselOffButton.Pressed = true;
            }
            else
            {
                ShowVesselOnButton.Pressed = true;
            }
        }
        else
        {
            ShowVesselOffButton.Disabled = true;
            ShowVesselOnButton.Disabled = true;
        }
    }
}
