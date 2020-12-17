﻿#nullable enable
using System;
using System.Collections.Generic;
using Content.Server.Administration;
using Content.Server.GameObjects.Components.Singularity;
using Content.Server.GameObjects.Components.PA;
using Content.Shared.Administration;
using Content.Shared.GameObjects.Components;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    public class StartSingularityEngineCommand : IClientCommand
    {
        public string Command => "startsingularityengine";
        public string Description => "Automatically turns on the particle accelerator and containment field emitters.";
        public string Help => $"{Command}";

        public void Execute(IConsoleShell shell, IPlayerSession? player, string[] args)
        {
            if (args.Length != 0)
            {
                shell.SendText(player, $"Invalid amount of arguments: {args.Length}.\n{Help}");
                return;
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();
            foreach (var ent in entityManager.GetEntities(new TypeEntityQuery(typeof(EmitterComponent))))
            {
                ent.GetComponent<EmitterComponent>().SwitchOn();
            }
            foreach (var ent in entityManager.GetEntities(new TypeEntityQuery(typeof(ParticleAcceleratorControlBoxComponent))))
            {
                var pacb = ent.GetComponent<ParticleAcceleratorControlBoxComponent>();
                pacb.RescanParts();
                pacb.SetStrength(ParticleAcceleratorPowerState.Level1);
                pacb.SwitchOn();
            }
            shell.SendText(player, "Done!");
        }
    }
}
