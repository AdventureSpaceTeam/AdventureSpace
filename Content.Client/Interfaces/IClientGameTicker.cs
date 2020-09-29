﻿using System;
using System.Collections.Generic;
using Robust.Shared.Network;
using static Content.Shared.SharedGameTicker;

namespace Content.Client.Interfaces
{
    public interface IClientGameTicker
    {
        bool IsGameStarted { get; }
        string ServerInfoBlob { get; }
        bool AreWeReady { get; }
        bool DisallowedLateJoin { get; }
        DateTime StartTime { get; }
        bool Paused { get; }
        Dictionary<NetUserId, PlayerStatus> Status { get; }

        void Initialize();
        event Action InfoBlobUpdated;
        event Action LobbyStatusUpdated;
        event Action LobbyReadyUpdated;
        event Action LobbyLateJoinStatusUpdated;
    }
}
