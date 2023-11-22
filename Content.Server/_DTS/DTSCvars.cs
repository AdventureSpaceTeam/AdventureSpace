using Robust.Shared.Configuration;

namespace Content.Server._DTS;

[CVarDefs]
public sealed class DTSCvars
{
    public static readonly CVarDef<bool> DisableMapRepetition =
        CVarDef.Create("dts.disable_map_repetition", true, CVar.SERVERONLY | CVar.ARCHIVE);

    public static readonly CVarDef<bool> AutoMapVoteOnRoundEnd =
        CVarDef.Create("dts.auto_map_vote_on_round_end", true, CVar.SERVERONLY | CVar.ARCHIVE);

    public static readonly CVarDef<bool> AutoGameModVoteOnRoundEnd =
        CVarDef.Create("dts.auto_game_mode_vote_on_round_end", true, CVar.SERVERONLY | CVar.ARCHIVE);
}
