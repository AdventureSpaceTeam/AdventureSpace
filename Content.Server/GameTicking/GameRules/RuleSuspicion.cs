﻿using System;
using System.Threading;
using Content.Server.GameObjects.Components.Suspicion;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces.Chat;
using Content.Server.Interfaces.GameTicking;
using Content.Server.Mobs.Roles.Suspicion;
using Content.Server.Players;
using Content.Shared;
using Content.Shared.GameObjects.Components.Damage;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Timer = Robust.Shared.Timers.Timer;

namespace Content.Server.GameTicking.GameRules
{
    /// <summary>
    ///     Simple GameRule that will do a free-for-all death match.
    ///     Kill everybody else to win.
    /// </summary>
    public sealed class RuleSuspicion : GameRule, IEntityEventSubscriber
    {
        private static readonly TimeSpan DeadCheckDelay = TimeSpan.FromSeconds(1);

        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly IGameTicker _gameTicker = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;

        private readonly CancellationTokenSource _checkTimerCancel = new();

        public override void Added()
        {
            _chatManager.DispatchServerAnnouncement(Loc.GetString("There are traitors on the station! Find them, and kill them!"));

            bool Predicate(IPlayerSession session) => session.ContentData()?.Mind?.HasRole<SuspicionTraitorRole>() ?? false;

            EntitySystem.Get<AudioSystem>().PlayGlobal("/Audio/Misc/tatoralert.ogg", AudioParams.Default, Predicate);

            EntitySystem.Get<DoorSystem>().AccessType = DoorSystem.AccessTypes.AllowAllNoExternal;

            Timer.SpawnRepeating(DeadCheckDelay, _checkWinConditions, _checkTimerCancel.Token);
        }

        public override void Removed()
        {
            base.Removed();

            EntitySystem.Get<DoorSystem>().AccessType = DoorSystem.AccessTypes.Id;

            _checkTimerCancel.Cancel();
        }

        private void _checkWinConditions()
        {
            if (!_cfg.GetCVar(CCVars.GameLobbyEnableWin))
                return;

            var traitorsAlive = 0;
            var innocentsAlive = 0;

            foreach (var playerSession in _playerManager.GetAllPlayers())
            {
                if (playerSession.AttachedEntity == null
                    || !playerSession.AttachedEntity.TryGetComponent(out IDamageableComponent damageable)
                    || !playerSession.AttachedEntity.HasComponent<SuspicionRoleComponent>())
                {
                    continue;
                }

                if (damageable.CurrentState != DamageState.Alive)
                {
                    continue;
                }

                var mind = playerSession.ContentData()?.Mind;

                if (mind != null && mind.HasRole<SuspicionTraitorRole>())
                    traitorsAlive++;
                else
                    innocentsAlive++;
            }

            if (innocentsAlive + traitorsAlive == 0)
            {
                _chatManager.DispatchServerAnnouncement(Loc.GetString("Everybody is dead, it's a stalemate!"));
                EndRound(Victory.Stalemate);
            }

            else if (traitorsAlive == 0)
            {
                _chatManager.DispatchServerAnnouncement(Loc.GetString("The traitors are dead! The innocents win."));
                EndRound(Victory.Innocents);
            }
            else if (innocentsAlive == 0)
            {
                _chatManager.DispatchServerAnnouncement(Loc.GetString("The innocents are dead! The traitors win."));
                EndRound(Victory.Traitors);
            }
        }

        private enum Victory
        {
            Stalemate,
            Innocents,
            Traitors
        }

        private void EndRound(Victory victory)
        {
            string text;

            switch (victory)
            {
                case Victory.Innocents:
                    text = Loc.GetString("The innocents have won!");
                    break;
                case Victory.Traitors:
                    text = Loc.GetString("The traitors have won!");
                    break;
                default:
                    text = Loc.GetString("Nobody wins!");
                    break;
            }

            _gameTicker.EndRound(text);

            var restartDelay = 10;

            _chatManager.DispatchServerAnnouncement(Loc.GetString("Restarting in {0} seconds.", restartDelay));
            _checkTimerCancel.Cancel();

            Timer.Spawn(TimeSpan.FromSeconds(restartDelay), () => _gameTicker.RestartRound());
        }
    }
}
