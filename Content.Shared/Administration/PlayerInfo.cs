using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration
{
    [Serializable, NetSerializable]
    public record PlayerInfo(
        string Username,
        string CharacterName,
        string IdentityName,
        string StartingJob,
        bool Antag,
        NetEntity? NetEntity,
        NetUserId SessionId,
        bool Connected,
        bool ActiveThisRound,
        TimeSpan? OverallPlaytime,
        bool IsSponsor,
        string? SponsorTitle)
    {
        private string? _playtimeString;

        public string PlaytimeString => _playtimeString ??=
            OverallPlaytime?.ToString("%d':'hh':'mm") ?? Loc.GetString("generic-unknown-title");
    }
}
