using System.Linq;
using System.Text;
using Content.Client.Materials;
using Content.Shared.Lathe;
using Content.Shared.Lathe.Prototypes;
using Content.Shared.Research.Prototypes;
using Robust.Client.AutoGenerated;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Prototypes;

namespace Content.Client.Lathe.UI;

[GenerateTypedNameReferences]
public sealed partial class LatheMenu : DefaultWindow
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private readonly SpriteSystem _spriteSystem;
    private readonly LatheSystem _lathe;
    private readonly MaterialStorageSystem _materialStorage;

    public event Action<BaseButton.ButtonEventArgs>? OnServerListButtonPressed;
    public event Action<string, int>? RecipeQueueAction;

    public List<ProtoId<LatheRecipePrototype>> Recipes = new();

    public List<ProtoId<LatheCategoryPrototype>>? Categories;

    public ProtoId<LatheCategoryPrototype>? CurrentCategory;

    public EntityUid Entity;

    public LatheMenu()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);

        _spriteSystem = _entityManager.System<SpriteSystem>();
        _lathe = _entityManager.System<LatheSystem>();
        _materialStorage = _entityManager.System<MaterialStorageSystem>();

        SearchBar.OnTextChanged += _ =>
        {
            PopulateRecipes();
        };
        AmountLineEdit.OnTextChanged += _ =>
        {
            PopulateRecipes();
        };

        FilterOption.OnItemSelected += OnItemSelected;

        ServerListButton.OnPressed += a => OnServerListButtonPressed?.Invoke(a);
    }

    public void SetEntity(EntityUid uid)
    {
        Entity = uid;

        if (_entityManager.TryGetComponent<LatheComponent>(Entity, out var latheComponent))
        {
            if (!latheComponent.DynamicRecipes.Any())
            {
                ServerListButton.Visible = false;
            }
        }

        MaterialsList.SetOwner(Entity);
    }

    /// <summary>
    /// Populates the list of all the recipes
    /// </summary>
    public void PopulateRecipes()
    {
        var recipesToShow = new List<LatheRecipePrototype>();
        foreach (var recipe in Recipes)
        {
            if (!_prototypeManager.TryIndex(recipe, out var proto))
                continue;

            if (CurrentCategory != null && proto.Category != CurrentCategory)
                continue;

            if (SearchBar.Text.Trim().Length != 0)
            {
                if (_lathe.GetRecipeName(recipe).ToLowerInvariant().Contains(SearchBar.Text.Trim().ToLowerInvariant()))
                    recipesToShow.Add(proto);
            }
            else
            {
                recipesToShow.Add(proto);
            }
        }

        if (!int.TryParse(AmountLineEdit.Text, out var quantity) || quantity <= 0)
            quantity = 1;

        var sortedRecipesToShow = recipesToShow.OrderBy(_lathe.GetRecipeName);
        RecipeList.Children.Clear();
        _entityManager.TryGetComponent(Entity, out LatheComponent? lathe);

        foreach (var prototype in sortedRecipesToShow)
        {
            var canProduce = _lathe.CanProduce(Entity, prototype, quantity, component: lathe);

            var control = new RecipeControl(_lathe, prototype, () => GenerateTooltipText(prototype), canProduce, GetRecipeDisplayControl(prototype));
            control.OnButtonPressed += s =>
            {
                if (!int.TryParse(AmountLineEdit.Text, out var amount) || amount <= 0)
                    amount = 1;
                RecipeQueueAction?.Invoke(s, amount);
            };
            RecipeList.AddChild(control);
        }
    }

    private string GenerateTooltipText(LatheRecipePrototype prototype)
    {
        StringBuilder sb = new();
        var multiplier = _entityManager.GetComponent<LatheComponent>(Entity).MaterialUseMultiplier;

        foreach (var (id, amount) in prototype.Materials)
        {
            if (!_prototypeManager.TryIndex(id, out var proto))
                continue;

            var adjustedAmount = SharedLatheSystem.AdjustMaterial(amount, prototype.ApplyMaterialDiscount, multiplier);
            var sheetVolume = _materialStorage.GetSheetVolume(proto);

            var unit = Loc.GetString(proto.Unit);
            var sheets = adjustedAmount / (float) sheetVolume;

            var availableAmount = _materialStorage.GetMaterialAmount(Entity, id);
            var missingAmount = Math.Max(0, adjustedAmount - availableAmount);
            var missingSheets = missingAmount / (float) sheetVolume;

            var name = Loc.GetString(proto.Name);

            string tooltipText;
            if (missingSheets > 0)
            {
                tooltipText = Loc.GetString("lathe-menu-material-amount-missing", ("amount", sheets), ("missingAmount", missingSheets), ("unit", unit), ("material", name));
            }
            else
            {
                var amountText = Loc.GetString("lathe-menu-material-amount", ("amount", sheets), ("unit", unit));
                tooltipText = Loc.GetString("lathe-menu-tooltip-display", ("material", name), ("amount", amountText));
            }

            sb.AppendLine(tooltipText);
        }

        var desc = _lathe.GetRecipeDescription(prototype);
        if (!string.IsNullOrWhiteSpace(desc))
            sb.AppendLine(Loc.GetString("lathe-menu-description-display", ("description", desc)));

        // Remove last newline
        if (sb.Length > 0)
            sb.Remove(sb.Length - 1, 1);

        return sb.ToString();
    }

    public void UpdateCategories()
    {
        var currentCategories = new List<ProtoId<LatheCategoryPrototype>>();
        foreach (var recipeId in Recipes)
        {
            var recipe = _prototypeManager.Index(recipeId);

            if (recipe.Category == null)
                continue;

            if (currentCategories.Contains(recipe.Category.Value))
                continue;

            currentCategories.Add(recipe.Category.Value);
        }

        if (Categories != null && (Categories.Count == currentCategories.Count || !Categories.All(currentCategories.Contains)))
            return;

        Categories = currentCategories;
        var sortedCategories = currentCategories
            .Select(p => _prototypeManager.Index(p))
            .OrderBy(p => Loc.GetString(p.Name))
            .ToList();

        FilterOption.Clear();
        FilterOption.AddItem(Loc.GetString("lathe-menu-category-all"), -1);
        foreach (var category in sortedCategories)
        {
            FilterOption.AddItem(Loc.GetString(category.Name), Categories.IndexOf(category.ID));
        }

        FilterOption.SelectId(-1);
    }

    /// <summary>
    /// Populates the build queue list with all queued items
    /// </summary>
    /// <param name="queue"></param>
    public void PopulateQueueList(List<LatheRecipePrototype> queue)
    {
        QueueList.DisposeAllChildren();

        var idx = 1;
        foreach (var recipe in queue)
        {
            var queuedRecipeBox = new BoxContainer();
            queuedRecipeBox.Orientation = BoxContainer.LayoutOrientation.Horizontal;

            queuedRecipeBox.AddChild(GetRecipeDisplayControl(recipe));

            var queuedRecipeLabel = new Label();
            queuedRecipeLabel.Text = $"{idx}. {_lathe.GetRecipeName(recipe)}";
            queuedRecipeBox.AddChild(queuedRecipeLabel);
            QueueList.AddChild(queuedRecipeBox);
            idx++;
        }
    }

    public void SetQueueInfo(LatheRecipePrototype? recipe)
    {
        FabricatingContainer.Visible = recipe != null;
        if (recipe == null)
            return;

        FabricatingDisplayContainer.Children.Clear();
        FabricatingDisplayContainer.AddChild(GetRecipeDisplayControl(recipe));

        NameLabel.Text = _lathe.GetRecipeName(recipe);
    }

    public Control GetRecipeDisplayControl(LatheRecipePrototype recipe)
    {
        if (recipe.Icon != null)
        {
            var textRect = new TextureRect();
            textRect.Texture = _spriteSystem.Frame0(recipe.Icon);
            return textRect;
        }

        if (recipe.Result is { } result)
        {
            var entProtoView = new EntityPrototypeView();
            entProtoView.SetPrototype(result);
            return entProtoView;
        }

        return new Control();
    }

    private void OnItemSelected(OptionButton.ItemSelectedEventArgs obj)
    {
        FilterOption.SelectId(obj.Id);
        if (obj.Id == -1)
        {
            CurrentCategory = null;
        }
        else
        {
            CurrentCategory = Categories?[obj.Id];
        }
        PopulateRecipes();
    }
}
