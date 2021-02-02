﻿using System;
using Content.Server.Administration;
using Content.Server.GameTicking;
using Content.Server.Interfaces.GameTicking;
using Content.Shared.Administration;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Console;
using Robust.Shared.IoC;

namespace Content.Server.Commands.GameTicking
{
    [AdminCommand(AdminFlags.Server)]
    class DelayStartCommand : IConsoleCommand
    {
        public string Command => "delaystart";
        public string Description => "Delays the round start.";
        public string Help => $"Usage: {Command} <seconds>\nPauses/Resumes the countdown if no argument is provided.";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var ticker = IoCManager.Resolve<IGameTicker>();
            if (ticker.RunLevel != GameRunLevel.PreRoundLobby)
            {
                shell.WriteLine("This can only be executed while the game is in the pre-round lobby.");
                return;
            }

            if (args.Length == 0)
            {
                var paused = ticker.TogglePause();
                shell.WriteLine(paused ? "Paused the countdown." : "Resumed the countdown.");
                return;
            }

            if (args.Length != 1)
            {
                shell.WriteLine("Need zero or one arguments.");
                return;
            }

            if (!uint.TryParse(args[0], out var seconds) || seconds == 0)
            {
                shell.WriteLine($"{args[0]} isn't a valid amount of seconds.");
                return;
            }

            var time = TimeSpan.FromSeconds(seconds);
            if (!ticker.DelayStart(time))
            {
                shell.WriteLine("An unknown error has occurred.");
            }
        }
    }
}