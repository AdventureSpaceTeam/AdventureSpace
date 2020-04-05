using Robust.Client.UserInterface;

namespace Content.Client.UserInterface.Stylesheets
{
    public interface IStylesheetManager
    {
        Stylesheet SheetNano { get; }
        Stylesheet SheetSpace { get; }

        void Initialize();
    }
}
