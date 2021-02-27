using System;
using Robust.Client.AutoGenerated;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

#nullable enable

namespace Content.Client.Construction
{
    /// <summary>
    /// This is the interface for a UI View of the construction window. The point of it is to abstract away the actual
    /// UI controls and just provide higher level operations on the entire window. This View is completely passive and
    /// just raises events to the outside world. This class is controlled by the <see cref="ConstructionMenuPresenter"/>.
    /// </summary>
    public interface IConstructionMenuView : IDisposable
    {
        // It isn't optimal to expose UI controls like this, but the UI control design is
        // questionable so it can't be helped.
        string[] Categories { get; set; }
        OptionButton CategoryButton { get; }

        bool EraseButtonPressed { get; set; }
        bool BuildButtonPressed { get; set; }

        ItemList Recipes { get; }
        ItemList RecipeStepList { get; }

        event EventHandler<(string search, string catagory)> PopulateRecipes;
        event EventHandler<ItemList.Item?> RecipeSelected;
        event EventHandler<bool> BuildButtonToggled;
        event EventHandler<bool> EraseButtonToggled;
        event EventHandler ClearAllGhosts;

        void ClearRecipeInfo();
        void SetRecipeInfo(string name, string description, Texture iconTexture, bool isItem);
        void ResetPlacement();

        #region Window Control

        event Action? OnClose;

        bool IsOpen { get; }

        void OpenCentered();
        void MoveToFront();
        bool IsAtFront();
        void Close();

        #endregion
    }

    [GenerateTypedNameReferences]
    public partial class ConstructionMenu : SS14Window, IConstructionMenuView
    {
        public bool BuildButtonPressed
        {
            get => BuildButton.Pressed;
            set => BuildButton.Pressed = value;
        }

        public string[] Categories { get; set; } = Array.Empty<string>();

        public OptionButton CategoryButton => Category;

        public bool EraseButtonPressed
        {
            get => EraseButton.Pressed;
            set => EraseButton.Pressed = value;
        }

        /// <inheritdoc />
        public ItemList Recipes => RecipesList;

        public ItemList RecipeStepList => StepList;

        public ConstructionMenu()
        {
            SetSize = MinSize = (720, 320);

            IoCManager.InjectDependencies(this);
            RobustXamlLoader.Load(this);

            Title = Loc.GetString("Construction");

            BuildButton.Text = Loc.GetString("Place construction ghost");
            RecipesList.OnItemSelected += obj => RecipeSelected?.Invoke(this, obj.ItemList[obj.ItemIndex]);
            RecipesList.OnItemDeselected += _ => RecipeSelected?.Invoke(this, null);

            SearchBar.OnTextChanged += _ => PopulateRecipes?.Invoke(this, (SearchBar.Text, Categories[Category.SelectedId]));
            Category.OnItemSelected += obj =>
            {
                Category.SelectId(obj.Id);
                PopulateRecipes?.Invoke(this, (SearchBar.Text, Categories[obj.Id]));
            };

            BuildButton.Text = Loc.GetString("Place construction ghost");
            BuildButton.OnToggled += args => BuildButtonToggled?.Invoke(this, args.Pressed);
            ClearButton.Text = Loc.GetString("Clear All");
            ClearButton.OnPressed += _ => ClearAllGhosts?.Invoke(this, EventArgs.Empty);
            EraseButton.Text = Loc.GetString("Eraser Mode");
            EraseButton.OnToggled += args => EraseButtonToggled?.Invoke(this, args.Pressed);
        }

        public event EventHandler? ClearAllGhosts;

        public event EventHandler<(string search, string catagory)>? PopulateRecipes;
        public event EventHandler<ItemList.Item?>? RecipeSelected;
        public event EventHandler<bool>? BuildButtonToggled;
        public event EventHandler<bool>? EraseButtonToggled;

        public void ResetPlacement()
        {
            BuildButton.Pressed = false;
            EraseButton.Pressed = false;
        }

        public void SetRecipeInfo(string name, string description, Texture iconTexture, bool isItem)
        {
            BuildButton.Disabled = false;
            BuildButton.Text = Loc.GetString(isItem ? "Place construction ghost" : "Craft");
            TargetName.SetMessage(name);
            TargetDesc.SetMessage(description);
            TargetTexture.Texture = iconTexture;
        }

        public void ClearRecipeInfo()
        {
            BuildButton.Disabled = true;
            TargetName.SetMessage(string.Empty);
            TargetDesc.SetMessage(string.Empty);
            TargetTexture.Texture = null;
            StepList.Clear();
        }
    }
}
