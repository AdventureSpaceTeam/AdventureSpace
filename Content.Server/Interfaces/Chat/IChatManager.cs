using Robust.Server.Interfaces.Player;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.Interfaces.Chat
{
    public interface IChatManager
    {
        void Initialize();

        /// <summary>
        ///     Dispatch a server announcement to every connected player.
        /// </summary>
        void DispatchServerAnnouncement(string message);

        void DispatchServerMessage(IPlayerSession player, string message);

        void EntitySay(IEntity source, string message);
        void EntityMe(IEntity source, string action);

        void SendOOC(IPlayerSession player, string message);
        void SendDeadChat(IPlayerSession player, string message);

        void SendHookOOC(string sender, string message);
    }
}
