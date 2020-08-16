﻿using Content.Shared.Network.NetMessages;
using Robust.Server.Interfaces.Player;

namespace Content.Server.Players
{
    public static class PlayerSessionExt
    {
        public static void RequestWindowAttention(this IPlayerSession session)
        {
            var msg = session.ConnectedClient.CreateNetMessage<MsgRequestWindowAttention>();
            session.ConnectedClient.SendMessage(msg);
        }
    }
}
