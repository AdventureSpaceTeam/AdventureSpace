using Content.Shared.Eui;
using Content.Shared.Roles;
using Robust.Shared.Serialization;

namespace Content.Shared.NewLife
{
    [NetSerializable, Serializable]
    public struct NewLifeCharacterInfo
    {
        public int Identifier { get; set; }
        public string Name { get; set; }
    }

    [NetSerializable, Serializable]
    public struct NewLifeRolesInfo
    {
        public string Identifier { get; set; }
        public string Name { get; set; }
    }

    [NetSerializable, Serializable]
    public sealed class NewLifeEuiState : EuiStateBase
    {
        public List<NewLifeCharacterInfo> Characters { get; }
        public List<NewLifeRolesInfo> Roles { get; }
        public TimeSpan NextRespawnTime { get; }
        public List<int> UsedCharactersForRespawn { get; }

        public NewLifeEuiState(List<NewLifeCharacterInfo> characters, List<NewLifeRolesInfo> roles,
            TimeSpan nextRespawnTime, List<int> usedCharactersForRespawn)
        {
            Characters = characters;
            Roles = roles;
            NextRespawnTime = nextRespawnTime;
            UsedCharactersForRespawn = usedCharactersForRespawn;
        }
    }

    [NetSerializable, Serializable]
    public sealed class NewLifeRequestSpawnMessage : EuiMessageBase
    {
        public int? CharacterId { get; }
        public string? RoleProto { get; }
        public NewLifeRequestSpawnMessage(int? characterId, string? roleProto)
        {
            CharacterId = characterId;
            RoleProto = roleProto;
        }
    }
}
