﻿using System;
using Content.Server.Administration;
using Content.Server.RoundEnd;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.GameTicking.Commands
{
    [AdminCommand(AdminFlags.Server)]
    public class RestartRoundCommand : IConsoleCommand
    {
        public string Command => "restartround";
        public string Description => "Ends the current round and starts the countdown for the next lobby.";
        public string Help => String.Empty;

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            EntitySystem.Get<RoundEndSystem>().EndRound();
        }
    }

    [AdminCommand(AdminFlags.Server)]
    public class RestartRoundNowCommand : IConsoleCommand
    {
        public string Command => "restartroundnow";
        public string Description => "Moves the server from PostRound to a new PreRoundLobby.";
        public string Help => String.Empty;

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            EntitySystem.Get<GameTicker>().RestartRound();
        }
    }
}
