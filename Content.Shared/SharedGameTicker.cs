﻿using System;
using System.Collections.Generic;
using Lidgren.Network;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Network;

namespace Content.Shared
{
    public abstract class SharedGameTicker
    {
        // See ideally these would be pulled from the job definition or something.
        // But this is easier, and at least it isn't hardcoded.
        public const string OverflowJob = "Assistant";
        public const string OverflowJobName = "assistant";

        protected class MsgTickerJoinLobby : NetMessage
        {
            #region REQUIRED

            public const MsgGroups GROUP = MsgGroups.Command;
            public const string NAME = nameof(MsgTickerJoinLobby);
            public MsgTickerJoinLobby(INetChannel channel) : base(NAME, GROUP) { }

            #endregion

            public override void ReadFromBuffer(NetIncomingMessage buffer)
            {
            }

            public override void WriteToBuffer(NetOutgoingMessage buffer)
            {
            }
        }

        protected class MsgTickerJoinGame : NetMessage
        {
            #region REQUIRED

            public const MsgGroups GROUP = MsgGroups.Command;
            public const string NAME = nameof(MsgTickerJoinGame);
            public MsgTickerJoinGame(INetChannel channel) : base(NAME, GROUP) { }

            #endregion

            public override void ReadFromBuffer(NetIncomingMessage buffer)
            {
            }

            public override void WriteToBuffer(NetOutgoingMessage buffer)
            {
            }
        }

        protected class MsgTickerLobbyStatus : NetMessage
        {
            #region REQUIRED

            public const MsgGroups GROUP = MsgGroups.Command;
            public const string NAME = nameof(MsgTickerLobbyStatus);
            public MsgTickerLobbyStatus(INetChannel channel) : base(NAME, GROUP) { }

            #endregion

            public bool IsRoundStarted { get; set; }
            public bool YouAreReady { get; set; }
            // UTC.
            public DateTime StartTime { get; set; }
            public bool Paused { get; set; }

            public override void ReadFromBuffer(NetIncomingMessage buffer)
            {
                IsRoundStarted = buffer.ReadBoolean();

                if (IsRoundStarted)
                {
                    return;
                }

                YouAreReady = buffer.ReadBoolean();
                StartTime = new DateTime(buffer.ReadInt64(), DateTimeKind.Utc);
                Paused = buffer.ReadBoolean();
            }

            public override void WriteToBuffer(NetOutgoingMessage buffer)
            {
                buffer.Write(IsRoundStarted);

                if (IsRoundStarted)
                {
                    return;
                }

                buffer.Write(YouAreReady);
                buffer.Write(StartTime.Ticks);
                buffer.Write(Paused);
            }
        }

        protected class MsgTickerLobbyInfo : NetMessage
        {
            #region REQUIRED

            public const MsgGroups GROUP = MsgGroups.Command;
            public const string NAME = nameof(MsgTickerLobbyInfo);
            public MsgTickerLobbyInfo(INetChannel channel) : base(NAME, GROUP) { }

            #endregion

            public string TextBlob { get; set; }

            public override void ReadFromBuffer(NetIncomingMessage buffer)
            {
                TextBlob = buffer.ReadString();
            }

            public override void WriteToBuffer(NetOutgoingMessage buffer)
            {
                buffer.Write(TextBlob);
            }
        }

        protected class MsgTickerLobbyCountdown : NetMessage
        {
            #region REQUIRED

            public const MsgGroups GROUP = MsgGroups.Command;
            public const string NAME = nameof(MsgTickerLobbyCountdown);
            public MsgTickerLobbyCountdown(INetChannel channel) : base(NAME, GROUP) { }

            #endregion

            /// <summary>
            /// The total amount of seconds to go until the countdown finishes
            /// </summary>
            public DateTime StartTime { get; set; }

            /// <summary>
            /// Whether or not the countdown is paused
            /// </summary>
            public bool Paused { get; set; }

            public override void ReadFromBuffer(NetIncomingMessage buffer)
            {
                StartTime = new DateTime(buffer.ReadInt64(), DateTimeKind.Utc);
                Paused = buffer.ReadBoolean();
            }

            public override void WriteToBuffer(NetOutgoingMessage buffer)
            {
                buffer.Write(StartTime.Ticks);
                buffer.Write(Paused);
            }
        }


        public struct RoundEndPlayerInfo
        {
            public string PlayerOOCName;
            public string PlayerICName;
            public string Role;
            public bool Antag;

        }

        protected class MsgRoundEndMessage : NetMessage
        {

            #region REQUIRED

            public const MsgGroups GROUP = MsgGroups.Command;
            public const string NAME = nameof(MsgRoundEndMessage);
            public MsgRoundEndMessage(INetChannel channel) : base(NAME, GROUP) { }

            #endregion

            public string GamemodeTitle;
            public TimeSpan RoundDuration;


            public int PlayerCount;

            public List<RoundEndPlayerInfo> AllPlayersEndInfo;

            public override void ReadFromBuffer(NetIncomingMessage buffer)
            {
                GamemodeTitle = buffer.ReadString();

                var hours = buffer.ReadInt32();
                var mins = buffer.ReadInt32();
                var seconds = buffer.ReadInt32();
                RoundDuration = new TimeSpan(hours, mins, seconds);

                PlayerCount = buffer.ReadInt32();
                AllPlayersEndInfo = new List<RoundEndPlayerInfo>();
                for(var i = 0; i < PlayerCount; i++)
                {
                    var readPlayerData = new RoundEndPlayerInfo
                    {
                        PlayerOOCName = buffer.ReadString(),
                        PlayerICName = buffer.ReadString(),
                        Role = buffer.ReadString(),
                        Antag = buffer.ReadBoolean()
                    };

                    AllPlayersEndInfo.Add(readPlayerData);
                }

            }

            public override void WriteToBuffer(NetOutgoingMessage buffer)
            {
                buffer.Write(GamemodeTitle);
                buffer.Write(RoundDuration.Hours);
                buffer.Write(RoundDuration.Minutes);
                buffer.Write(RoundDuration.Seconds);


                buffer.Write(AllPlayersEndInfo.Count);
                foreach(var playerEndInfo in AllPlayersEndInfo)
                {
                    buffer.Write(playerEndInfo.PlayerOOCName);
                    buffer.Write(playerEndInfo.PlayerICName);
                    buffer.Write(playerEndInfo.Role);
                    buffer.Write(playerEndInfo.Antag);
                }
            }

        }
    }
}

