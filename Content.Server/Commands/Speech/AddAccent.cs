﻿#nullable enable
using System;
using System.Linq;
using Content.Server.Administration;
using Content.Server.GameObjects.Components.Mobs.Speech;
using Content.Shared.Administration;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Commands.Speech
{
    [AdminCommand(AdminFlags.Fun)]
    public class AddAccent : IClientCommand
    {
        public string Command => "addaccent";
        public string Description => "Add a speech component to the current player";
        public string Help => $"{Command} <component>/?";

        public void Execute(IConsoleShell shell, IPlayerSession? player, string[] args)
        {
            if (player?.AttachedEntity == null)
            {
                shell.SendText(player, "You don't have an entity!");
                return;
            }

            if (args.Length == 0)
            {
                shell.SendText(player, Help);
                return;
            }

            var compFactory = IoCManager.Resolve<IComponentFactory>();

            if (args[0] == "?")
            {
                // Get all components that implement the ISpeechComponent except
                var speeches = compFactory.GetAllRefTypes()
                    .Where(c => typeof(IAccentComponent).IsAssignableFrom(c) && c.IsClass);
                var msg = "";
                foreach(var s in speeches)
                {
                    msg += $"{compFactory.GetRegistration(s).Name}\n";
                }
                shell.SendText(player, msg);
            }
            else
            {
                var name = args[0];
                // Try to get the Component
                Type type;
                try
                {
                    var comp = compFactory.GetComponent(name);
                    type = comp.GetType();
                }
                catch (Exception)
                {
                    shell.SendText(player, $"Accent {name} not found. Try {Command} ? to get a list of all appliable accents.");
                    return;
                }

                // Check if that already exists
                try
                {
                    var comp = player.AttachedEntity.GetComponent(type);
                    shell.SendText(player, "You already have this accent!");
                    return;
                }
                catch (Exception)
                {
                    // Accent not found
                }

                // Generic fuckery
                var ensure = typeof(IEntity).GetMethod("AddComponent");
                if (ensure == null)
                    return;
                var method = ensure.MakeGenericMethod(type);
                method.Invoke(player.AttachedEntity, null);
            }
        }
    }
}
