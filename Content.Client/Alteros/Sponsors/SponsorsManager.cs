using Content.Corvax.Interfaces.Client;
using Content.Shared.Alteros.Sponsors;
using Robust.Shared.Network;

namespace Content.Client.Alteros.Sponsors;

public sealed class SponsorsManager : IClientSponsorsManager
{
    [Dependency] private readonly IClientNetManager _netMgr = default!;

    public void Initialize()
    {
        _netMgr.RegisterNetMessage<MsgSponsorInfo>(OnUpdate);
    }

    private void OnUpdate(MsgSponsorInfo message)
    {
        Reset();

        if (message.Info == null)
        {
            return;
        }

        OocColor = Color.TryFromHex(message.Info.OOCColor);
        Prototypes.AddRange(message.Info.AllowedMarkings);
        Prototypes.AddRange(message.Info.AllowedRoles);
        Prototypes.AddRange(message.Info.AllowedSpecies);
        PriorityJoin = message.Info.HavePriorityJoin;
        ExtraCharSlots = message.Info.ExtraSlots;
        GhostTheme = message.Info.GhostTheme;
        OpenRoles = message.Info.OpenRoles;
        OpenAntags = message.Info.OpenAntags;
        HavePriorityRoles = message.Info.HavePriorityRoles;
        HavePriorityAntags = message.Info.HavePriorityAntags;
    }

    private void Reset()
    {
        Prototypes.Clear();
        PriorityJoin = false;
        OocColor = null;
        ExtraCharSlots = 0;
        GhostTheme = null;
        OpenRoles = false;
        OpenAntags = false;
        HavePriorityRoles = false;
        HavePriorityAntags = false;
    }


    public List<string> Prototypes { get; } = new();
    public bool PriorityJoin { get; private set; }
    public Color? OocColor { get; private set; }
    public int ExtraCharSlots { get; private set; }
    public string? GhostTheme { get; private set; }
    public bool OpenRoles { get; private set; }
    public bool OpenAntags { get; private set; }
    public bool HavePriorityRoles { get; private set; }
    public bool HavePriorityAntags { get; private set; }
}
