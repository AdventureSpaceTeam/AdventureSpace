using System.Linq;
using System.Text;
using Content.Client.Stylesheets;
using Content.Shared.Lathe;
using Content.Shared.Materials;
using Content.Shared.Research.Prototypes;
using Robust.Client.AutoGenerated;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
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

    // public event Action<BaseButton.ButtonEventArgs>? OnMaterialsEjectionButtonPressed;
    public event Action<BaseButton.ButtonEventArgs>? OnServerListButtonPressed;
    public event Action<string, int>? RecipeQueueAction;
    public event Action<string, int>? OnEjectPressed;
    public List<string> Recipes = new();

    public LatheMenu(LatheBoundUserInterface owner)
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);

        _spriteSystem = _entityManager.System<SpriteSystem>();
        _lathe = _entityManager.System<LatheSystem>();

        Title = _entityManager.GetComponent<MetaDataComponent>(owner.Owner).EntityName;

        SearchBar.OnTextChanged += _ =>
        {
            PopulateRecipes(owner.Owner);
        };
        AmountLineEdit.OnTextChanged += _ =>
        {
            PopulateRecipes(owner.Owner);
        };

        //MaterialsEjectionButton.OnPressed += a => OnMaterialsEjectionButtonPressed?.Invoke(a);
        ServerListButton.OnPressed += a => OnServerListButtonPressed?.Invoke(a);

        if (_entityManager.TryGetComponent<LatheComponent>(owner.Owner, out var latheComponent))
        {
            if (!latheComponent.DynamicRecipes.Any())
            {
                ServerListButton.Visible = false;
            }
        }
    }

    public void PopulateMaterials(EntityUid lathe)
    {
        if (!_entityManager.TryGetComponent<MaterialStorageComponent>(lathe, out var materials))
            return;

        MaterialsList.DisposeAllChildren();

        foreach (var (materialId, volume) in materials.Storage)
        {
            if (volume <= 0)
                continue;

            if (!_prototypeManager.TryIndex(materialId, out MaterialPrototype? material))
                continue;

            var name = Loc.GetString(material.Name);
            var mat = Loc.GetString("lathe-menu-material-display",
                ("material", name), ("amount", volume));
            int volumePerSheet = 0;
            int maxEjectableSheets = 0;

            if (material.StackEntity != null)
            {
                var proto = _prototypeManager.Index<EntityPrototype>(material.StackEntity);
                name = proto.Name;

                if (proto.TryGetComponent<PhysicalCompositionComponent>(out var composition))
                {
                    volumePerSheet = composition.MaterialComposition.FirstOrDefault(kvp => kvp.Key == materialId).Value;
                    maxEjectableSheets = (int) MathF.Floor(volume / volumePerSheet);
                }
            }

            var row = new LatheMaterialEjector(materialId, OnEjectPressed, volumePerSheet, maxEjectableSheets)
            {
                Icon = { Texture = _spriteSystem.Frame0(material.Icon) },
                ProductName = { Text = mat }
            };

            MaterialsList.AddChild(row);
        }

        if (MaterialsList.ChildCount == 0)
        {
            var noMaterialsMsg = Loc.GetString("lathe-menu-no-materials-message");
            var noItemRow = new Label();
            noItemRow.Text = noMaterialsMsg;
            noItemRow.Align = Label.AlignMode.Center;
            MaterialsList.AddChild(noItemRow);
        }
    }
    /// <summary>
    /// Populates the list of all the recipes
    /// </summary>
    /// <param name="lathe"></param>
    public void PopulateRecipes(EntityUid lathe)
    {
        if (!_entityManager.TryGetComponent<LatheComponent>(lathe, out var component))
            return;

        var recipesToShow = new List<LatheRecipePrototype>();
        foreach (var recipe in Recipes)
        {
            if (!_prototypeManager.TryIndex<LatheRecipePrototype>(recipe, out var proto))
                continue;

            if (SearchBar.Text.Trim().Length != 0)
            {
                if (proto.Name.ToLowerInvariant().Contains(SearchBar.Text.Trim().ToLowerInvariant()))
                    recipesToShow.Add(proto);
            }
            else
            {
                recipesToShow.Add(proto);
            }
        }

        if (!int.TryParse(AmountLineEdit.Text, out var quantity) || quantity <= 0)
            quantity = 1;

        RecipeList.Children.Clear();
        foreach (var prototype in recipesToShow)
        {
            StringBuilder sb = new();
            var first = true;
            foreach (var (id, amount) in prototype.RequiredMaterials)
            {
                if (!_prototypeManager.TryIndex<MaterialPrototype>(id, out var proto))
                    continue;

                if (first)
                    first = false;
                else
                    sb.Append('\n');

                var adjustedAmount = SharedLatheSystem.AdjustMaterial(amount, prototype.ApplyMaterialDiscount, component.MaterialUseMultiplier);

                sb.Append(Loc.GetString("lathe-menu-tooltip-display", ("amount", adjustedAmount), ("material", Loc.GetString(proto.Name))));
            }

            var icon = prototype.Icon == null
                ? _spriteSystem.GetPrototypeIcon(prototype.Result).Default
                : _spriteSystem.Frame0(prototype.Icon);
            var canProduce = _lathe.CanProduce(lathe, prototype, quantity);

            var control = new RecipeControl(prototype, sb.ToString(), canProduce, icon);
            control.OnButtonPressed += s =>
            {
                if (!int.TryParse(AmountLineEdit.Text, out var amount) || amount <= 0)
                    amount = 1;
                RecipeQueueAction?.Invoke(s, amount);
            };
            RecipeList.AddChild(control);
        }
    }

    /// <summary>
    /// Populates the build queue list with all queued items
    /// </summary>
    /// <param name="queue"></param>
    public void PopulateQueueList(List<LatheRecipePrototype> queue)
    {
        QueueList.Clear();
        var idx = 1;
        foreach (var recipe in queue)
        {
            var icon = recipe.Icon == null
                ? _spriteSystem.GetPrototypeIcon(recipe.Result).Default
                : _spriteSystem.Frame0(recipe.Icon);
            QueueList.AddItem($"{idx}. {recipe.Name}", icon);
            idx++;
        }
    }

    public void SetQueueInfo(LatheRecipePrototype? recipe)
    {
        if (recipe != null)
        {
            Icon.Texture = recipe.Icon == null
                ? _spriteSystem.GetPrototypeIcon(recipe.Result).Default
                : _spriteSystem.Frame0(recipe.Icon);
            FabricatingActiveLabel.Text = "Fabricating...";
            NameLabel.Text = $"{recipe.Name}";
        }
        else
        {
            Icon.Texture = Texture.Transparent;
            FabricatingActiveLabel.Text = String.Empty;
            NameLabel.Text = String.Empty;
        }
    }
}
