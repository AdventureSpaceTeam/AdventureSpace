﻿using Content.Shared.Targeting;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.Targeting.UI;

[GenerateTypedNameReferences]
public sealed partial class TargetingDoll : BoxContainer
{
    public static readonly string StyleClassTargetDollZone = "target-doll-zone";


    private TargetingZone _activeZone = TargetingZone.Middle;

    public event Action<TargetingZone>? OnZoneChanged;
    public TargetingDoll()
    {
        RobustXamlLoader.Load(this);
    }

    public TargetingZone ActiveZone
    {
        get => _activeZone;
        set
        {
            if (_activeZone == value)
            {
                return;
            }

            _activeZone = value;
            OnZoneChanged?.Invoke(value);

            UpdateButtons();
        }
    }
    private void UpdateButtons()
    {
        ButtonHigh.Pressed = _activeZone == TargetingZone.High;
        ButtonMedium.Pressed = _activeZone == TargetingZone.Middle;
        ButtonLow.Pressed = _activeZone == TargetingZone.Low;
    }
}
