using System.Linq;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Prototypes;
using Content.Shared.Access;
using Content.Shared.Access.Systems;

namespace Content.Client.Access.UI;

[GenerateTypedNameReferences]
public sealed partial class AccessLevelControl : GridContainer
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public readonly Dictionary<string, Button> ButtonsList = new();

    public AccessLevelControl(List<string> accessLevels, IPrototypeManager prototypeManager)
    {
        RobustXamlLoader.Load(this);
        foreach (var access in accessLevels)
        {
            if (!prototypeManager.TryIndex<AccessLevelPrototype>(access, out var accessLevel))
            {
                Logger.ErrorS(AccessLevelSystem.Sawmill, $"Unable to find accesslevel for {ac
cess}");
                continue;
            }

            var newButton = new Button
            {
                Text = GetAccessLevelName(accessLevel),
                ToggleMode = true,
            };
            AddChild(newButton);
            ButtonsList.Add(accessLevel.ID, newButton);
        }
    }

    private static string GetAccessLevelName(AccessLevelPrototype prototype)
    {
        if (prototype.Name is { } name)
            return Loc.GetString(name);

        return prototype.ID;
    }
    public void UpdateState(List<String> pressedList, List<String>? enabledList = null)
    {
        foreach (var (accessName, button) in ButtonsList)
        {
            button.Pressed = pressedList.Contains(accessName);
            button.Disabled = !(enabledList?.Contains(accessName) ?? true);
        }
    }
}
