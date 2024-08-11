using Content.Shared.Inventory;

namespace Content.Shared.Radio.Components;

[RegisterComponent]
public sealed partial class RadioNameComponent : Component
{
    [DataField]
    public string Name = string.Empty;
}
