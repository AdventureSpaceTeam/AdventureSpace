﻿using Content.Shared.Station;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.GameTicking
{
    public abstract class SharedGameTicker : EntitySystem
    {
        // See ideally these would be pulled from the job definition or something.
        // But this is easier, and at least it isn't hardcoded.
        public const string FallbackOverflowJob = "Assistant";
        public const string FallbackOverflowJobName = "assistant";
    }

    [Serializable, NetSerializable]
    public sealed class TickerJoinLobbyEvent : EntityEventArgs
    {
    }

    [Serializable, NetSerializable]
    public sealed class TickerJoinGameEvent : EntityEventArgs
    {
    }

    [Serializable, NetSerializable]
    public sealed class TickerLateJoinStatusEvent : EntityEventArgs
    {
        // TODO: Make this a replicated CVar, honestly.
        public bool Disallowed { get; }

        public TickerLateJoinStatusEvent(bool disallowed)
        {
            Disallowed = disallowed;
        }
    }


    [Serializable, NetSerializable]
    public sealed class TickerLobbyStatusEvent : EntityEventArgs
    {
        public bool IsRoundStarted { get; }
        public string? LobbySong { get; }
        public string? LobbyBackground { get; }
        public bool YouAreReady { get; }
        // UTC.
        public TimeSpan StartTime { get; }
        public bool Paused { get; }

        public TickerLobbyStatusEvent(bool isRoundStarted, string? lobbySong, string? lobbyBackground, bool youAreReady, TimeSpan startTime, bool paused)
        {
            IsRoundStarted = isRoundStarted;
            LobbySong = lobbySong;
            LobbyBackground = lobbyBackground;
            YouAreReady = youAreReady;
            StartTime = startTime;
            Paused = paused;
        }
    }

    [Serializable, NetSerializable]
    public sealed class TickerLobbyInfoEvent : EntityEventArgs
    {
        public string TextBlob { get; }

        public TickerLobbyInfoEvent(string textBlob)
        {
            TextBlob = textBlob;
        }
    }

    [Serializable, NetSerializable]
    public sealed class TickerLobbyCountdownEvent : EntityEventArgs
    {
        /// <summary>
        /// The game time that the game will start at.
        /// </summary>
        public TimeSpan StartTime { get; }

        /// <summary>
        /// Whether or not the countdown is paused
        /// </summary>
        public bool Paused { get; }

        public TickerLobbyCountdownEvent(TimeSpan startTime, bool paused)
        {
            StartTime = startTime;
            Paused = paused;
        }
    }

    [Serializable, NetSerializable]
    public sealed class TickerLobbyReadyEvent : EntityEventArgs
    {
        /// <summary>
        /// The Status of the Player in the lobby (ready, observer, ...)
        /// </summary>
        public Dictionary<NetUserId, LobbyPlayerStatus> Status { get; }

        public TickerLobbyReadyEvent(Dictionary<NetUserId, LobbyPlayerStatus> status)
        {
            Status = status;
        }
    }

    [Serializable, NetSerializable]
    public sealed class TickerJobsAvailableEvent : EntityEventArgs
    {
        /// <summary>
        /// The Status of the Player in the lobby (ready, observer, ...)
        /// </summary>
        public Dictionary<StationId, Dictionary<string, int>> JobsAvailableByStation { get; }
        public Dictionary<StationId, string> StationNames { get; }

        public TickerJobsAvailableEvent(Dictionary<StationId, string> stationNames, Dictionary<StationId, Dictionary<string, int>> jobsAvailableByStation)
        {
            StationNames = stationNames;
            JobsAvailableByStation = jobsAvailableByStation;
        }
    }

    [Serializable, NetSerializable]
    public sealed class RoundEndMessageEvent : EntityEventArgs
    {
        [Serializable, NetSerializable]
        public struct RoundEndPlayerInfo
        {
            public string PlayerOOCName;
            public string? PlayerICName;
            public string Role;
            public bool Antag;
            public bool Observer;
            public bool Connected;
        }

        public string GamemodeTitle { get; }
        public string RoundEndText { get; }
        public TimeSpan RoundDuration { get; }
        public int RoundId { get; }
        public int PlayerCount { get; }
        public RoundEndPlayerInfo[] AllPlayersEndInfo { get; }

        public RoundEndMessageEvent(string gamemodeTitle, string roundEndText, TimeSpan roundDuration, int roundId,
            int playerCount, RoundEndPlayerInfo[] allPlayersEndInfo)
        {
            GamemodeTitle = gamemodeTitle;
            RoundEndText = roundEndText;
            RoundDuration = roundDuration;
            RoundId = roundId;
            PlayerCount = playerCount;
            AllPlayersEndInfo = allPlayersEndInfo;
        }
    }


    [Serializable, NetSerializable]
    public enum LobbyPlayerStatus : sbyte
    {
        NotReady = 0,
        Ready,
        Observer,
    }
}

