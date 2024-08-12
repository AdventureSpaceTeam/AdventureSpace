using Robust.Shared.Network;

namespace Content.Server.NewLife;

interface INewLifeSystem
{
    void AddUsedCharactersForRespawn(NetUserId userId, int usedCharacter);
    void SetNextAllowRespawn(NetUserId userId, TimeSpan nextRespawnTime);
    int NewLifeTimeout {get; set;}
}
