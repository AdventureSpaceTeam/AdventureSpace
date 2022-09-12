﻿using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.UserInterface.Controls
{
    [GenerateTypedNameReferences]
    [Virtual]
    public partial class FancyWindow : BaseWindow
    {
        public FancyWindow()
        {
            RobustXamlLoader.Load(this);

            CloseButton.OnPressed += _ => Close();
            XamlChildren = ContentsContainer.Children;
        }

        public string? Title
        {
            get => WindowTitle.Text;
            set => WindowTitle.Text = value;
        }

        protected override DragMode GetDragModeFor(Vector2 relativeMousePos)
        {
            return DragMode.Move;
        }
    }
}
