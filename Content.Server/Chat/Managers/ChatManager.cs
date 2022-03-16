using System.Linq;
using System.Text;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Ghost.Components;
using Content.Server.Headset;
using Content.Server.MoMMI;
using Content.Server.Players;
using Content.Server.Preferences.Managers;
using Content.Server.Radio.EntitySystems;
using Content.Server.Disease;
using Content.Server.Disease.Components;
using Content.Shared.Disease.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Players;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using static Content.Server.Chat.Managers.IChatManager;

namespace Content.Server.Chat.Managers
{
    /// <summary>
    ///     Dispatches chat messages to clients.
    /// </summary>
    internal sealed class ChatManager : IChatManager
    {
        private static readonly Dictionary<string, string> PatronOocColors = new()
        {
            // I had plans for multiple colors and those went nowhere so...
            { "nuclear_operative", "#aa00ff" },
            { "syndicate_agent", "#aa00ff" },
            { "revolutionary", "#aa00ff" }
        };

        [Dependency] private readonly IChatSanitizationManager _sanitizer = default!;
        [Dependency] private readonly IEntityManager _entManager = default!;
        [Dependency] private readonly IServerNetManager _netManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IMoMMILink _mommiLink = default!;
        [Dependency] private readonly IAdminManager _adminManager = default!;
        [Dependency] private readonly IServerPreferencesManager _preferencesManager = default!;
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        private AdminLogSystem _logs = default!;

        /// <summary>
        /// The maximum length a player-sent message can be sent
        /// </summary>
        public int MaxMessageLength => _configurationManager.GetCVar(CCVars.ChatMaxMessageLength);

        private const int VoiceRange = 7; // how far voice goes in world units
        private const int WhisperRange = 2; // how far whisper goes in world units

        //TODO: make prio based?
        private readonly List<TransformChat> _chatTransformHandlers = new();
        private bool _oocEnabled = true;
        private bool _adminOocEnabled = true;
        private bool _loocEnabled = true;
        private bool _adminLoocEnabled = true;

        public void Initialize()
        {
            _logs = EntitySystem.Get<AdminLogSystem>();
            _netManager.RegisterNetMessage<MsgChatMessage>();

            _configurationManager.OnValueChanged(CCVars.OocEnabled, OnOocEnabledChanged, true);
            _configurationManager.OnValueChanged(CCVars.LoocEnabled, OnLoocEnabledChanged, true);
            _configurationManager.OnValueChanged(CCVars.AdminOocEnabled, OnAdminOocEnabledChanged, true);
        }

        private void OnOocEnabledChanged(bool val)
        {
            if (_oocEnabled == val) return;

            _oocEnabled = val;
            DispatchServerAnnouncement(Loc.GetString(val ? "chat-manager-ooc-chat-enabled-message" : "chat-manager-ooc-chat-disabled-message"));
        }

        private void OnLoocEnabledChanged(bool val)
        {
            if (_loocEnabled == val) return;

            _loocEnabled = val;
            DispatchServerAnnouncement(Loc.GetString(val ? "chat-manager-looc-chat-enabled-message" : "chat-manager-looc-chat-disabled-message"));
        }

        private void OnAdminOocEnabledChanged(bool val)
        {
            if (_adminOocEnabled == val) return;

            _adminOocEnabled = val;
            DispatchServerAnnouncement(Loc.GetString(val ? "chat-manager-admin-ooc-chat-enabled-message" : "chat-manager-admin-ooc-chat-disabled-message"));
        }

        public void DispatchServerAnnouncement(string message, Color? colorOverride = null)
        {
            var messageWrap = Loc.GetString("chat-manager-server-wrap-message");
            NetMessageToAll(ChatChannel.Server, message, messageWrap, colorOverride);
            Logger.InfoS("SERVER", message);

            _logs.Add(LogType.Chat, LogImpact.Low, $"Server announcement: {message}");
        }

        public void DispatchStationAnnouncement(string message, string sender = "Central Command", bool playDefaultSound = true, Color? colorOverride = null)
        {
            var messageWrap = Loc.GetString("chat-manager-sender-announcement-wrap-message", ("sender", sender));
            NetMessageToAll(ChatChannel.Radio, message, messageWrap, colorOverride);
            if (playDefaultSound)
            {
                SoundSystem.Play(Filter.Broadcast(), "/Audio/Announcements/announce.ogg", AudioParams.Default.WithVolume(-2f));
            }

            _logs.Add(LogType.Chat, LogImpact.Low, $"Station Announcement from {sender}: {message}");
        }

        public void DispatchServerMessage(IPlayerSession player, string message)
        {
            var messageWrap = Loc.GetString("chat-manager-server-wrap-message");
            var msg = _netManager.CreateNetMessage<MsgChatMessage>();
            msg.Channel = ChatChannel.Server;
            msg.Message = message;
            msg.MessageWrap = messageWrap;
            _netManager.ServerSendMessage(msg, player.ConnectedClient);

            _logs.Add(LogType.Chat, LogImpact.Low, $"Server message from {player:Player}: {message}");
        }

        public void TrySpeak(EntityUid source, string message, bool isWhisper = false, IConsoleShell? shell = null, IPlayerSession? player = null)
        {
            // Listen it avoids the 30 lines being copy-paste and means only 1 source needs updating if something changes.
            if (_entManager.HasComponent<GhostComponent>(source))
            {
                if (player == null) return;
                SendDeadChat(player, message);
            }
            else
            {
                var mindComponent = player?.ContentData()?.Mind;

                if (mindComponent == null)
                {
                    shell?.WriteError("You don't have a mind!");
                    return;
                }

                if (mindComponent.OwnedEntity is not {Valid: true} owned)
                {
                    shell?.WriteError("You don't have an entity!");
                    return;
                }

                var isEmote = _sanitizer.TrySanitizeOutSmilies(message, owned, out var sanitized, out var emoteStr);

                if (sanitized.Length != 0)
                    SendEntityChatType(owned, sanitized, isWhisper);

                if (isEmote)
                    EntityMe(owned, emoteStr!);
            }
        }

        public void TryEmote(EntityUid source, string message, IConsoleShell? shell = null, IPlayerSession? player = null)
        {
            var mindComponent = player?.ContentData()?.Mind;

            if (mindComponent == null)
            {
                shell?.WriteError("You don't have a mind!");
                return;
            }

            if (mindComponent.OwnedEntity is not {Valid: true} owned)
            {
                shell?.WriteError("You don't have an entity!");
                return;
            }

            var isEmote = _sanitizer.TrySanitizeOutSmilies(message, mindComponent.OwnedEntity.Value, out var sanitized, out var emoteStr);

            if (sanitized.Length != 0)
                EntityMe(mindComponent.OwnedEntity.Value, sanitized);

            if (isEmote)
                EntityMe(mindComponent.OwnedEntity.Value, emoteStr!);
        }

        public void EntitySay(EntityUid source, string message, bool hideChat=false)
        {
            if (!EntitySystem.Get<ActionBlockerSystem>().CanSpeak(source))
            {
                return;
            }

            if (_entManager.HasComponent<DiseasedComponent>(source) && _entManager.TryGetComponent<DiseaseCarrierComponent>(source,out var carrier))
            {
                EntitySystem.Get<DiseaseSystem>().SneezeCough(source, _random.Pick(carrier.Diseases), string.Empty);
            }

            if (MessageCharacterLimit(source, message))
            {
                return;
            }

            message = message.Trim();

            message = SanitizeMessageCapital(source, message);

            foreach (var handler in _chatTransformHandlers)
            {
                //TODO: rather return a bool and use a out var?
                message = handler(source, message);
            }

            var listeners = EntitySystem.Get<ListeningSystem>();
            listeners.PingListeners(source, message);

            message = FormattedMessage.EscapeText(message);

            var sessions = new List<ICommonSession>();
            ClientDistanceToList(source, VoiceRange, sessions);

            var messageWrap = Loc.GetString("chat-manager-entity-say-wrap-message",("entityName", _entManager.GetComponent<MetaDataComponent>(source).EntityName));

            foreach (var session in sessions)
            {
                NetMessageToOne(ChatChannel.Local, message, messageWrap, source, hideChat, session.ConnectedClient);
            }

            _logs.Add(LogType.Chat, LogImpact.Low, $"Say from {_entManager.ToPrettyString(source):user}: {message}");
        }

        public void EntityWhisper(EntityUid source, string message, bool hideChat=false)
        {
            if (!EntitySystem.Get<ActionBlockerSystem>().CanSpeak(source))
            {
                return;
            }

            if (MessageCharacterLimit(source, message))
            {
                return;
            }

            message = message.Trim();

            message = SanitizeMessageCapital(source, message);

            foreach (var handler in _chatTransformHandlers)
            {
                //TODO: rather return a bool and use a out var?
                message = handler(source, message);
            }

            var listeners = EntitySystem.Get<ListeningSystem>();
            listeners.PingListeners(source, message);

            message = FormattedMessage.EscapeText(message);

            var obfuscatedMessage = ObfuscateMessageReadability(message, 0.2f);

            var sessions = new List<ICommonSession>();
            ClientDistanceToList(source, VoiceRange, sessions);

            var transformSource = _entManager.GetComponent<TransformComponent>(source);
            var sourceCoords = transformSource.Coordinates;
            var messageWrap = Loc.GetString("chat-manager-entity-whisper-wrap-message",("entityName", _entManager.GetComponent<MetaDataComponent>(source).EntityName));

            var xforms = _entManager.GetEntityQuery<TransformComponent>();
            var ghosts = _entManager.GetEntityQuery<GhostComponent>();

            foreach (var session in sessions)
            {
                if (session.AttachedEntity is not {Valid: true} playerEntity)
                    continue;

                var transformEntity = xforms.GetComponent(playerEntity);

                if (sourceCoords.InRange(_entManager, transformEntity.Coordinates, WhisperRange) ||
                    ghosts.HasComponent(playerEntity))
                {
                    NetMessageToOne(ChatChannel.Whisper, message, messageWrap, source, hideChat, session.ConnectedClient);
                }
                else
                {
                    NetMessageToOne(ChatChannel.Whisper, obfuscatedMessage, messageWrap, source, hideChat, session.ConnectedClient);
                }
            }

            _logs.Add(LogType.Chat, LogImpact.Low, $"Whisper from {_entManager.ToPrettyString(source):user}: {message}");
        }

        public void EntityMe(EntityUid source, string action)
        {
            if (!EntitySystem.Get<ActionBlockerSystem>().CanEmote(source))
            {
                return;
            }

            if (MessageCharacterLimit(source, action))
            {
                return;
            }

            action = FormattedMessage.EscapeText(action);

            var sessions = new List<ICommonSession>();

            ClientDistanceToList(source, VoiceRange, sessions);

            var messageWrap = Loc.GetString("chat-manager-entity-me-wrap-message", ("entityName", _entManager.GetComponent<MetaDataComponent>(source).EntityName));

            foreach (var session in sessions)
            {
                NetMessageToOne(ChatChannel.Emotes, action, messageWrap, source, true, session.ConnectedClient);
            }

            _logs.Add(LogType.Chat, LogImpact.Low, $"Emote from {_entManager.ToPrettyString(source):user}: {action}");
        }

        public void SendLOOC(IPlayerSession player, string message)
        {
            if (_adminManager.IsAdmin(player))
            {
                if (!_adminLoocEnabled)
                {
                    return;
                }
            }
            else if (!_loocEnabled)
            {
                return;
            }

            // Check they're even attached to an entity before we potentially send a message length error.
            if (player.AttachedEntity is not { } entity)
            {
                return;
            }

            // Check if message exceeds the character limit
            if (message.Length > MaxMessageLength)
            {
                DispatchServerMessage(player, Loc.GetString("chat-manager-max-message-length-exceeded-message", ("limit", MaxMessageLength)));
                return;
            }

            message = FormattedMessage.EscapeText(message);
            var sessions = new List<ICommonSession>();

            ClientDistanceToList(entity, VoiceRange, sessions);

            var msg = _netManager.CreateNetMessage<MsgChatMessage>();
            msg.Channel = ChatChannel.LOOC;
            msg.Message = message;
            msg.MessageWrap = Loc.GetString("chat-manager-entity-looc-wrap-message", ("entityName", _entManager.GetComponent<MetaDataComponent>(entity).EntityName));

            _netManager.ServerSendToMany(msg, sessions.Select(o => o.ConnectedClient).ToList());

            _logs.Add(LogType.Chat, LogImpact.Low, $"LOOC from {player:Player}: {message}");
        }

        public void SendOOC(IPlayerSession player, string message)
        {
            if (_adminManager.IsAdmin(player))
            {
                if (!_adminOocEnabled)
                {
                    return;
                }
            }
            else if (!_oocEnabled)
            {
                return;
            }

            // Check if message exceeds the character limit
            if (message.Length > MaxMessageLength)
            {
                DispatchServerMessage(player, Loc.GetString("chat-manager-max-message-length-exceeded-message", ("limit", MaxMessageLength)));
                return;
            }

            message = FormattedMessage.EscapeText(message);

            var msg = _netManager.CreateNetMessage<MsgChatMessage>();
            msg.Channel = ChatChannel.OOC;
            msg.Message = message;
            msg.MessageWrap = Loc.GetString("chat-manager-send-ooc-wrap-message", ("playerName",player.Name));
            if (_adminManager.HasAdminFlag(player, AdminFlags.Admin))
            {
                var prefs = _preferencesManager.GetPreferences(player.UserId);
                msg.MessageColorOverride = prefs.AdminOOCColor;
            }
            if (player.ConnectedClient.UserData.PatronTier is { } patron &&
                     PatronOocColors.TryGetValue(patron, out var patronColor))
            {
                msg.MessageWrap = Loc.GetString("chat-manager-send-ooc-patron-wrap-message", ("patronColor", patronColor),("playerName", player.Name));
            }

            //TODO: player.Name color, this will need to change the structure of the MsgChatMessage
            _netManager.ServerSendToAll(msg);

            _mommiLink.SendOOCMessage(player.Name, message);
            _logs.Add(LogType.Chat, LogImpact.Low, $"OOC from {player:Player}: {message}");
        }

        public void SendDeadChat(IPlayerSession player, string message)
        {
            // Check if message exceeds the character limit
            if (message.Length > MaxMessageLength)
            {
                DispatchServerMessage(player, Loc.GetString("chat-manager-max-message-length-exceeded-message",("limit", MaxMessageLength)));
                return;
            }

            message = FormattedMessage.EscapeText(message);

            var clients = GetDeadChatClients();

            var msg = _netManager.CreateNetMessage<MsgChatMessage>();
            msg.Channel = ChatChannel.Dead;
            msg.Message = message;

            var playerName = player.AttachedEntity is {Valid: true} playerEntity
                ? _entManager.GetComponent<MetaDataComponent>(playerEntity).EntityName
                : "???";
            msg.MessageWrap = Loc.GetString("chat-manager-send-dead-chat-wrap-message",
                                            ("deadChannelName", Loc.GetString("chat-manager-dead-channel-name")),
                                            ("playerName", (playerName)));
            msg.SenderEntity = player.AttachedEntity.GetValueOrDefault();
            _netManager.ServerSendToMany(msg, clients.ToList());

            _logs.Add(LogType.Chat, LogImpact.Low, $"Dead chat from {player:Player}: {message}");
        }

        public void SendAdminDeadChat(IPlayerSession player, string message)
        {
            // Check if message exceeds the character limit
            if (message.Length > MaxMessageLength)
            {
                DispatchServerMessage(player, Loc.GetString("chat-manager-max-message-length-exceeded-message", ("limit", MaxMessageLength)));
                return;
            }

            message = FormattedMessage.EscapeText(message);

            var clients = GetDeadChatClients();

            var msg = _netManager.CreateNetMessage<MsgChatMessage>();
            msg.Channel = ChatChannel.Dead;
            msg.Message = message;
            msg.MessageWrap = Loc.GetString("chat-manager-send-admin-dead-chat-wrap-message",
                                            ("adminChannelName", Loc.GetString("chat-manager-admin-channel-name")),
                                            ("userName", player.ConnectedClient.UserName));
            _netManager.ServerSendToMany(msg, clients.ToList());

            _logs.Add(LogType.Chat, LogImpact.Low, $"Admin dead chat from {player:Player}: {message}");
        }

        private IEnumerable<INetChannel> GetDeadChatClients()
        {
            return Filter.Empty()
                .AddWhereAttachedEntity(uid => _entManager.HasComponent<GhostComponent>(uid))
                .Recipients
                .Union(_adminManager.ActiveAdmins)
                .Select(p => p.ConnectedClient);
        }

        public void SendAdminChat(IPlayerSession player, string message)
        {
            // Check if message exceeds the character limit
            if (message.Length > MaxMessageLength)
            {
                DispatchServerMessage(player, Loc.GetString("chat-manager-max-message-length-exceeded-message", ("limit", MaxMessageLength)));
                return;
            }

            message = FormattedMessage.EscapeText(message);

            var clients = _adminManager.ActiveAdmins.Select(p => p.ConnectedClient);

            var msg = _netManager.CreateNetMessage<MsgChatMessage>();

            msg.Channel = ChatChannel.Admin;
            msg.Message = message;
            msg.MessageWrap = Loc.GetString("chat-manager-send-admin-chat-wrap-message",
                                            ("adminChannelName", Loc.GetString("chat-manager-admin-channel-name")),
                                            ("playerName", player.Name));
            _netManager.ServerSendToMany(msg, clients.ToList());

            _logs.Add(LogType.Chat, $"Admin chat from {player:Player}: {message}");
        }

        public void SendAdminAnnouncement(string message)
        {
            var clients = _adminManager.ActiveAdmins.Select(p => p.ConnectedClient);

            message = FormattedMessage.EscapeText(message);

            var msg = _netManager.CreateNetMessage<MsgChatMessage>();

            msg.Channel = ChatChannel.Admin;
            msg.Message = message;
            msg.MessageWrap = Loc.GetString("chat-manager-send-admin-announcement-wrap-message",
                                            ("adminChannelName", Loc.GetString("chat-manager-admin-channel-name")));

            _netManager.ServerSendToMany(msg, clients.ToList());

            _logs.Add(LogType.Chat, LogImpact.Low, $"Admin announcement from {message}: {message}");
        }

        public void SendHookOOC(string sender, string message)
        {
            message = FormattedMessage.EscapeText(message);
            var messageWrap = Loc.GetString("chat-manager-send-hook-ooc-wrap-message", ("senderName", sender));
            NetMessageToAll(ChatChannel.OOC, message, messageWrap);
            _logs.Add(LogType.Chat, LogImpact.Low, $"Hook OOC from {sender}: {message}");
        }

        public void RegisterChatTransform(TransformChat handler)
        {
            // TODO: Literally just make this an event...
            _chatTransformHandlers.Add(handler);
        }

        public void SendEntityChatType(EntityUid source, string message, bool isWhisper)
        {
            // I don't know why you're trying to smile over the radio...
            // This filters out the players who just really want to try.
            if (message.StartsWith(';') && message.Length <= 1)
            {
                return;
            }

            // We check to see if message is a whisper or a standard say message.
            if (isWhisper)
            {
                EntityWhisper(source, message);
            }
            else
            {
                EntitySay(source, message);
            }
        }
        public string SanitizeMessageCapital(EntityUid source, string message)
        {
            if (message.StartsWith(';'))
            {
                // Remove semicolon
                message = message.Substring(1).TrimStart();

                // Capitalize first letter
                message = message[0].ToString().ToUpper() + message.Remove(0, 1);

                var invSystem = EntitySystem.Get<InventorySystem>();

                if (invSystem.TryGetSlotEntity(source, "ears", out var entityUid) &&
                    _entManager.TryGetComponent(entityUid, out HeadsetComponent? headset))
                {
                    headset.RadioRequested = true;
                }
                else
                {
                    source.PopupMessage(Loc.GetString("chat-manager-no-headset-on-message"));
                }
            }
            else
            {
                // Capitalize first letter
                message = message[0].ToString().ToUpper() + message.Remove(0, 1);
            }

            return message;
        }

        public void NetMessageToOne(ChatChannel channel, string message, string messageWrap, EntityUid source, bool hideChat, INetChannel client)
        {
            var msg = _netManager.CreateNetMessage<MsgChatMessage>();
            msg.Channel = channel;
            msg.Message = message;
            msg.MessageWrap = messageWrap;
            msg.SenderEntity = source;
            msg.HideChat = hideChat;
            _netManager.ServerSendMessage(msg, client);
        }

        public void NetMessageToAll(ChatChannel channel, string message, string messageWrap, Color? colorOverride = null)
        {
            var msg = _netManager.CreateNetMessage<MsgChatMessage>();
            msg.Channel = channel;
            msg.Message = message;
            msg.MessageWrap = messageWrap;
            if (colorOverride != null)
            {
                msg.MessageColorOverride = colorOverride.Value;
            }
            _netManager.ServerSendToAll(msg);
        }

        public bool MessageCharacterLimit(EntityUid source, string message)
        {
            var isOverLength = false;

            // Check if message exceeds the character limit if the sender is a player
            if (_entManager.TryGetComponent(source, out ActorComponent? actor) &&
                message.Length > MaxMessageLength)
            {
                var feedback = Loc.GetString("chat-manager-max-message-length-exceeded-message", ("limit", MaxMessageLength));

                DispatchServerMessage(actor.PlayerSession, feedback);

                isOverLength = true;
            }

            return isOverLength;
        }

        public void ClientDistanceToList(EntityUid source, int voiceRange, List<ICommonSession> playerSessions)
        {
            var ghosts = _entManager.GetEntityQuery<GhostComponent>();
            var xforms = _entManager.GetEntityQuery<TransformComponent>();

            var transformSource = xforms.GetComponent(source);
            var sourceMapId = transformSource.MapID;
            var sourceCoords = transformSource.Coordinates;

            foreach (var player in _playerManager.Sessions)
            {
                if (player.AttachedEntity is not {Valid: true} playerEntity)
                    continue;

                var transformEntity = xforms.GetComponent(playerEntity);

                if (transformEntity.MapID != sourceMapId ||
                    !ghosts.HasComponent(playerEntity) &&
                    !sourceCoords.InRange(_entManager, transformEntity.Coordinates, voiceRange))
                    continue;

                playerSessions.Add(player);
            }
        }

        public string ObfuscateMessageReadability(string message, float chance)
        {
            var modifiedMessage = new StringBuilder(message);

            for (var i = 0; i < message.Length; i++)
            {
                if (char.IsWhiteSpace((modifiedMessage[i])))
                {
                    continue;
                }

                if (_random.Prob(chance))
                {
                    modifiedMessage[i] = modifiedMessage[i];
                }
                else
                {
                    modifiedMessage[i] = '~';
                }
            }

            return modifiedMessage.ToString();
        }
    }
}
