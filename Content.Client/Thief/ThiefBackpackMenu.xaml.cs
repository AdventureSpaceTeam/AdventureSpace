using Content.Client.UserInterface.Controls;
using Content.Shared.Thief;
using Robust.Client.AutoGenerated;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.Thief;

[GenerateTypedNameReferences]
public sealed partial class ThiefBackpackMenu : FancyWindow
{
    [Dependency] private readonly IEntitySystemManager _sysMan = default!;
    private readonly SpriteSystem _spriteSystem;

    private readonly ThiefBackpackBoundUserInterface _owner;

    public ThiefBackpackMenu(ThiefBackpackBoundUserInterface owner)
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);
        _spriteSystem = _sysMan.GetEntitySystem<SpriteSystem>();

        _owner = owner;

        ApproveButton.OnButtonDown += (args) =>
        {
            _owner.SendApprove();
        };
    }

    public void UpdateState(ThiefBackpackBoundUserInterfaceState state)
    {
        SetsGrid.RemoveAllChildren();
        int count = 0;
        int selectedNumber = 0;
        foreach (var set in state.Sets)
        {
            var child = new ThiefBackpackSet(set.Value, _spriteSystem);

            child.SetButton.OnButtonDown += (args) =>
            {
                _owner.SendChangeSelected(set.Key);
            };

            SetsGrid.AddChild(child);

            count++;

            if (set.Value.Selected)
                selectedNumber++;
        }

        SelectedSets.Text = Loc.GetString("thief-backpack-window-selected", ("selectedCount", selectedNumber), ("maxCount", state.MaxSelectedSets));
        ApproveButton.Disabled = selectedNumber == state.MaxSelectedSets ? false : true;
    }
}
