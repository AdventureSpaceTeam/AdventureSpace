using Content.Server.EUI;
using Content.Shared.Eui;
using Content.Shared.NewLife;
using Content.Shared.Preferences;
using Content.Shared.Roles;

namespace Content.Server.NewLife.UI
{
    public sealed class NewLifeEui : BaseEui
    {
        private readonly IReadOnlyDictionary<int, ICharacterProfile> _characterProfiles;
        private readonly List<JobPrototype> _availableJobs;
        private readonly TimeSpan _nextAllowRespawn;
        private readonly List<int> _usedCharactersForRespawn;

        public NewLifeEui(IReadOnlyDictionary<int, ICharacterProfile> prefsCharacters,
            List<JobPrototype> availableJobs, TimeSpan nextAllowRespawn, List<int> usedCharactersForRespawn)
        {
            _characterProfiles = prefsCharacters;
            _availableJobs = availableJobs;
            _nextAllowRespawn = nextAllowRespawn;
            _usedCharactersForRespawn = usedCharactersForRespawn;
        }

        public override NewLifeEuiState GetNewState()
        {
            return new(EntitySystem.Get<NewLifeSystem>().GetCharactersInfo(_characterProfiles),
                EntitySystem.Get<NewLifeSystem>().GetRolesInfo(_availableJobs),
                _nextAllowRespawn,
                _usedCharactersForRespawn);
        }

        public override void HandleMessage(EuiMessageBase msg)
        {
            base.HandleMessage(msg);

            switch (msg)
            {
                case NewLifeRequestSpawnMessage req:
                    EntitySystem.Get<NewLifeSystem>().OnGhostRespawnMenuRequest(Player, req.CharacterId, req.RoleProto);
                    break;
            }
        }

        public override void Closed()
        {
            base.Closed();

            EntitySystem.Get<NewLifeSystem>().CloseEui(Player);
        }
    }
}
