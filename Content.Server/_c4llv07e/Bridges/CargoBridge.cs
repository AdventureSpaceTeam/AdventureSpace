using Content.Shared.Cargo.Prototypes;
using Content.Shared.GameTicking;

namespace Content.Server._c4llv07e.Bridges;

//Little bridge to communicate from secrets
//TODO BY UR
public sealed class CargoBridge : EntitySystem
{
    private List<CargoProductPrototype> advancedPrototypes = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundEndedEvent>(OnRoundEnded);
    }

    private void OnRoundEnded(RoundEndedEvent ev)
    {
        advancedPrototypes.Clear();
    }

    public void AddAdvancedPrototypes(List<CargoProductPrototype> newPrototypes)
    {
        advancedPrototypes.AddRange(newPrototypes);
    }

    public List<CargoProductPrototype> GetAdvancedPrototypes()
    {
        return advancedPrototypes;
    }
}
