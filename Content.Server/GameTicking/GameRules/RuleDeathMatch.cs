﻿using System;
using System.Threading;
using Content.Server.Interfaces.Chat;
using Content.Server.Interfaces.GameTicking;
using Content.Shared.GameObjects.Components.Damage;
using Robust.Server.Interfaces.Player;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Timer = Robust.Shared.Timers.Timer;

namespace Content.Server.GameTicking.GameRules
{
    /// <summary>
    ///     Simple GameRule that will do a free-for-all death match.
    ///     Kill everybody else to win.
    /// </summary>
    public sealed class RuleDeathMatch : GameRule, IEntityEventSubscriber
    {
        private static readonly TimeSpan DeadCheckDelay = TimeSpan.FromSeconds(5);

        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly IGameTicker _gameTicker = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;

        private CancellationTokenSource _checkTimerCancel;

        public override void Added()
        {
            _chatManager.DispatchServerAnnouncement(Loc.GetString("The game is now a death match. Kill everybody else to win!"));

            _entityManager.EventBus.SubscribeEvent<HealthChangedEventArgs>(EventSource.Local, this, OnHealthChanged);
            _playerManager.PlayerStatusChanged += PlayerManagerOnPlayerStatusChanged;
        }

        public override void Removed()
        {
            base.Removed();

            _entityManager.EventBus.UnsubscribeEvent<HealthChangedEventArgs>(EventSource.Local, this);
            _playerManager.PlayerStatusChanged -= PlayerManagerOnPlayerStatusChanged;
        }

        private void OnHealthChanged(HealthChangedEventArgs message)
        {
            _runDelayedCheck();
        }

        private void _checkForWinner()
        {
            _checkTimerCancel = null;

            if (!_cfg.GetCVar<bool>("game.enablewin"))
                return;

            IPlayerSession winner = null;
            foreach (var playerSession in _playerManager.GetAllPlayers())
            {
                if (playerSession.AttachedEntity == null
                    || !playerSession.AttachedEntity.TryGetComponent(out IDamageableComponent damageable))
                {
                    continue;
                }

                if (damageable.CurrentDamageState != DamageState.Alive)
                {
                    continue;
                }

                if (winner != null)
                {
                    // Found a second person alive, nothing decided yet!
                    return;
                }

                winner = playerSession;
            }

            _chatManager.DispatchServerAnnouncement(winner == null
                ? Loc.GetString("Everybody is dead, it's a stalemate!")
                : Loc.GetString("{0} wins the death match!", winner));

            var restartDelay = 10;

            _chatManager.DispatchServerAnnouncement(Loc.GetString("Restarting in {0} seconds.", restartDelay));

            Timer.Spawn(TimeSpan.FromSeconds(restartDelay), () => _gameTicker.RestartRound());
        }

        private void PlayerManagerOnPlayerStatusChanged(object sender, SessionStatusEventArgs e)
        {
            if (e.NewStatus == SessionStatus.Disconnected)
            {
                _runDelayedCheck();
            }
        }

        private void _runDelayedCheck()
        {
            _checkTimerCancel?.Cancel();
            _checkTimerCancel = new CancellationTokenSource();

            Timer.Spawn(DeadCheckDelay, _checkForWinner, _checkTimerCancel.Token);
        }
    }
}
