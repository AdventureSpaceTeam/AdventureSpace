using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;

namespace Content.Client._Sunrise.StatsBoard;

[GenerateTypedNameReferences]
public sealed partial class StatsEntries : BoxContainer
{
    public StatsEntries()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);
    }

    public void AddEntry(StatsEntry entry)
    {
        EntriesContainer.AddChild(entry);
    }
}
