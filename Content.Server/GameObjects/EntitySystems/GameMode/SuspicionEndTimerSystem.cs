﻿using System;
using System.Linq;
using Content.Server.Interfaces.GameTicking;
using Content.Shared.GameObjects.EntitySystemMessages;
using Content.Shared.GameTicking;
using JetBrains.Annotations;
using Robust.Server.Interfaces.Player;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;

#nullable enable

namespace Content.Server.GameObjects.EntitySystems.GameMode
{
    [UsedImplicitly]
    public sealed class SuspicionEndTimerSystem : EntitySystem, IResettingEntitySystem
    {
        [Dependency] private readonly IPlayerManager _playerManager = null!;
        [Dependency] private readonly IGameTiming _timing = null!;
        [Dependency] private readonly IGameTicker _gameTicker = null!;

        private TimeSpan? _endTime;

        public TimeSpan? EndTime
        {
            get => _endTime;
            set
            {
                _endTime = value;
                SendUpdateToAll();
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            _playerManager.PlayerStatusChanged += PlayerManagerOnPlayerStatusChanged;
        }

        public override void Shutdown()
        {
            base.Shutdown();

            _playerManager.PlayerStatusChanged -= PlayerManagerOnPlayerStatusChanged;
        }

        private void PlayerManagerOnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
        {
            if (e.NewStatus == SessionStatus.InGame)
            {
                SendUpdateTimerMessage(e.Session);
            }
        }

        private void SendUpdateToAll()
        {
            foreach (var player in _playerManager.GetAllPlayers().Where(p => p.Status == SessionStatus.InGame))
            {
                SendUpdateTimerMessage(player);
            }
        }

        private void SendUpdateTimerMessage(IPlayerSession player)
        {
            var msg = new SuspicionMessages.SetSuspicionEndTimerMessage
            {
                EndTime = EndTime
            };

            EntityNetworkManager.SendSystemNetworkMessage(msg, player.ConnectedClient);
        }

        void IResettingEntitySystem.Reset()
        {
            EndTime = null;
        }
    }
}
