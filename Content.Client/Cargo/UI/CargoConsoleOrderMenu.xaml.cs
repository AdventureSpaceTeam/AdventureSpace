using System.Collections.Generic;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.IoC;

namespace Content.Client.Cargo.UI
{
    [GenerateTypedNameReferences]
    partial class CargoConsoleOrderMenu : DefaultWindow
    {
        public CargoConsoleOrderMenu()
        {
            RobustXamlLoader.Load(this);
            IoCManager.InjectDependencies(this);

            Amount.SetButtons(new List<int> { -3, -2, -1 }, new List<int> { 1, 2, 3 });
            Amount.IsValid = n => n > 0;
        }
    }
}
