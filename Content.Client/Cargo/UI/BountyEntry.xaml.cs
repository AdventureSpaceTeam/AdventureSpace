using Content.Client.Message;
using Content.Shared.Cargo;
using Content.Shared.Cargo.Prototypes;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.Cargo.UI;

[GenerateTypedNameReferences]
public sealed partial class BountyEntry : BoxContainer
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public Action? OnButtonPressed;

    public TimeSpan EndTime;

    public BountyEntry(CargoBountyData bounty)
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);

        if (!_prototype.TryIndex<CargoBountyPrototype>(bounty.Bounty, out var bountyPrototype))
            return;

        var items = new List<string>();
        foreach (var entry in bountyPrototype.Entries)
        {
            items.Add(Loc.GetString("bounty-console-manifest-entry",
                ("amount", entry.Amount),
                ("item", Loc.GetString(entry.Name))));
        }
        ManifestLabel.SetMarkup(Loc.GetString("bounty-console-manifest-label", ("item", string.Join(", ", items))));
        RewardLabel.SetMarkup(Loc.GetString("bounty-console-reward-label", ("reward", bountyPrototype.Reward)));
        DescriptionLabel.SetMarkup(Loc.GetString("bounty-console-description-label", ("description", Loc.GetString(bountyPrototype.Description))));
        IdLabel.SetMarkup(Loc.GetString("bounty-console-id-label", ("id", bounty.Id)));

        PrintButton.OnPressed += _ => OnButtonPressed?.Invoke();
    }
}
