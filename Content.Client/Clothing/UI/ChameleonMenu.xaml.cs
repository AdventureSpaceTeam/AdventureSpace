using System.Linq;
using System.Numerics;
using Content.Client.Clothing.Systems;
using Content.Client.Stylesheets;
using Robust.Client.AutoGenerated;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Prototypes;

namespace Content.Client.Clothing.UI;

[GenerateTypedNameReferences]
public sealed partial class ChameleonMenu : DefaultWindow
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    private readonly SpriteSystem _sprite;
    public event Action<string>? OnIdSelected;

    private IEnumerable<string> _possibleIds = Enumerable.Empty<string>();
    private string? _selectedId;
    private string _searchFilter = "";

    public ChameleonMenu()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);
        _sprite = _entityManager.System<SpriteSystem>();

        Search.OnTextChanged += OnSearchEntered;
    }

    public void UpdateState(IEnumerable<string> possibleIds, string? selectedId)
    {
        _possibleIds = possibleIds;
        _selectedId = selectedId;
        UpdateGrid();
    }

    private void OnSearchEntered(LineEdit.LineEditEventArgs obj)
    {
        _searchFilter = obj.Text;
        UpdateGrid();
    }

    private void UpdateGrid()
    {
        ClearGrid();

        var group = new ButtonGroup();
        var searchFilterLow = _searchFilter.ToLowerInvariant();

        foreach (var id in _possibleIds)
        {
            if (!_prototypeManager.TryIndex(id, out EntityPrototype? proto))
                continue;

            var lowId = id.ToLowerInvariant();
            var lowName = proto.Name.ToLowerInvariant();
            if (!lowId.Contains(searchFilterLow) && !lowName.Contains(_searchFilter))
                continue;

            var button = new Button
            {
                MinSize = new Vector2(48, 48),
                HorizontalExpand = true,
                Group = group,
                StyleClasses = {StyleBase.ButtonSquare},
                ToggleMode = true,
                Pressed = _selectedId == id,
                ToolTip = proto.Name
            };
            button.OnPressed += _ => OnIdSelected?.Invoke(id);
            Grid.AddChild(button);
            var entityPrototypeView = new EntityPrototypeView();
            button.AddChild(entityPrototypeView);
            entityPrototypeView.SetPrototype(proto);
        }
    }

    private void ClearGrid()
    {
        Grid.RemoveAllChildren();
    }
}
