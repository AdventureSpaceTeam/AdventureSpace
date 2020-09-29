using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Shared.Preferences;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Network;

namespace Content.Server.Interfaces
{
    public interface IServerPreferencesManager
    {
        void Init();

        void OnClientConnected(IPlayerSession session);
        void OnClientDisconnected(IPlayerSession session);

        bool HavePreferencesLoaded(IPlayerSession session);
        Task WaitPreferencesLoaded(IPlayerSession session);

        PlayerPreferences GetPreferences(NetUserId userId);
        IEnumerable<KeyValuePair<NetUserId, ICharacterProfile>> GetSelectedProfilesForPlayers(List<NetUserId> userIds);
    }
}
