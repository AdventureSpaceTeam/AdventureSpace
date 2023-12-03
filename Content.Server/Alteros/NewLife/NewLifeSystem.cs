using System.Linq;
using Content.Corvax.Interfaces.Server;
using Content.Server.EUI;
using Content.Server.GameTicking;
using Content.Server.NewLife.UI;
using Content.Server.Players.PlayTimeTracking;
using Content.Server.Preferences.Managers;
using Content.Server.Station.Systems;
using Content.Shared.CCVar;
using Content.Shared.Ghost;
using Content.Shared.NewLife;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using JetBrains.Annotations;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.NewLife
{
    [UsedImplicitly]
    public sealed class NewLifeSystem : SharedNewLifeSystem
    {
        [Dependency] private readonly EuiManager _euiManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly GameTicker _gameTicker = default!;
        [Dependency] private readonly StationJobsSystem _stationJobs = default!;
        [Dependency] private readonly StationSystem _stationSystem = default!;
        [Dependency] private readonly IServerPreferencesManager _prefsManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly PlayTimeTrackingSystem _playTimeTrackings = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;

        private readonly Dictionary<ICommonSession, NewLifeEui> _openUis = new();
        private int _newLifeTimeout = 30;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
            SubscribeNetworkEvent<NewLifeOpenRequest>(OnRespawnMenuOpenRequest);

            _cfg.OnValueChanged(CCVars.NewLifeTimeout, SetNewLifeTimeout, true);
        }

        private void SetNewLifeTimeout(int value)
        {
            _newLifeTimeout = value;
        }

        private void OnRespawnMenuOpenRequest(NewLifeOpenRequest msg, EntitySessionEventArgs args)
        {
            OpenEui(args.SenderSession);
        }

        public void OnGhostRespawnMenuRequest(ICommonSession player, int? characterId, string? roleProto)
        {
            var sponsors = IoCManager.Resolve<IServerSponsorsManager>(); // Alteros-Sponsors
            if (sponsors.AllowedRespawn(player.UserId) && characterId != null)
            {
                var stationUid = _stationSystem.GetStations().FirstOrDefault();
                _prefsManager.GetPreferences(player.UserId).SetProfile(characterId.Value);
                _gameTicker.MakeJoinGame(player, stationUid, roleProto);
                var timeout = _gameTiming.CurTime + TimeSpan.FromMinutes(_newLifeTimeout);
                sponsors.SetNextAllowRespawn(player.UserId, timeout);
            }
        }

        public void OpenEui(ICommonSession session)
        {
            if (session.AttachedEntity is not {Valid: true} attached ||
                !EntityManager.HasComponent<GhostComponent>(attached))
                return;

            if(_openUis.ContainsKey(session))
                CloseEui(session);

            var preferencesManager = IoCManager.Resolve<IServerPreferencesManager>();
            var prefs = preferencesManager.GetPreferences(session.UserId);

            var jobsAvailable = new List<JobPrototype>();
            var stationUid = _stationSystem.GetStations().FirstOrDefault();
            var stationAvailable = _stationJobs.GetAvailableJobs(stationUid);

            var sponsors = IoCManager.Resolve<IServerSponsorsManager>(); // Alteros-Sponsors
            if (!sponsors.TryGetPrototypes(session.UserId, out var prototypes))
                return;

            foreach (var jobId in stationAvailable)
            {
                if (!_playTimeTrackings.IsAllowed(session, jobId) && !prototypes!.Contains(jobId))
                    continue;
                jobsAvailable.Add(_prototypeManager.Index<JobPrototype>(jobId));
            }

            jobsAvailable.Sort((x, y) =>
                -string.Compare(x.LocalizedName, y.LocalizedName, StringComparison.CurrentCultureIgnoreCase));

            if (!sponsors.TryGetNextAllowRespawn(session.UserId, out var nextAllowRespawn))
                return;

            if (!sponsors.TryGetUsedCharactersForRespawn(session.UserId, out var usedCharactersForRespawn))
                return;

            var eui = _openUis[session] = new NewLifeEui(prefs.Characters, jobsAvailable,
                nextAllowRespawn.Value, usedCharactersForRespawn);
            _euiManager.OpenEui(eui, session);
            eui.StateDirty();
        }

        public void CloseEui(ICommonSession session)
        {
            if (!_openUis.ContainsKey(session))
                return;

            _openUis.Remove(session, out var eui);

            eui?.Close();
        }

        public void UpdateAllEui()
        {
            foreach (var eui in _openUis.Values)
            {
                eui.StateDirty();
            }
        }

        public List<NewLifeCharacterInfo> GetCharactersInfo(IReadOnlyDictionary<int, ICharacterProfile> characterProfiles)
        {
            var characters = new List<NewLifeCharacterInfo>();

            foreach (var (charKey, characterProfile) in characterProfiles)
            {
                characters.Add(new NewLifeCharacterInfo {Identifier = charKey, Name = characterProfile.Name});
            }

            return characters;
        }

        public List<NewLifeRolesInfo> GetRolesInfo(List<JobPrototype> availableJobs)
        {
            var roles = new List<NewLifeRolesInfo>();

            foreach (var availableJob in availableJobs)
            {
                roles.Add(new NewLifeRolesInfo {Identifier = availableJob.ID, Name = availableJob.LocalizedName});
            }

            return roles;
        }

        private void OnPlayerAttached(PlayerAttachedEvent message)
        {
            // Close the session of any player that has a ghost roles window open and isn't a ghost anymore.
            if (!_openUis.ContainsKey(message.Player))
                return;

            if (HasComp<GhostComponent>(message.Entity))
                return;

            CloseEui(message.Player);
        }
    }
}
