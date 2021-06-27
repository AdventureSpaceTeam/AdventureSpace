#nullable enable
using Content.Server.Administration;
using Content.Server.ParticleAccelerator.Components;
using Content.Server.Singularity.Components;
using Content.Shared.Administration;
using Content.Shared.Singularity.Components;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Singularity
{
    [AdminCommand(AdminFlags.Admin)]
    public class StartSingularityEngineCommand : IConsoleCommand
    {
        public string Command => "startsingularityengine";
        public string Description => "Automatically turns on the particle accelerator and containment field emitters.";
        public string Help => $"{Command}";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 0)
            {
                shell.WriteLine($"Invalid amount of arguments: {args.Length}.\n{Help}");
                return;
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();
            foreach (var comp in entityManager.ComponentManager.EntityQuery<EmitterComponent>())
            {
                comp.SwitchOn();
            }
            foreach (var comp in entityManager.ComponentManager.EntityQuery<RadiationCollectorComponent>())
            {
                comp.Collecting = true;
            }
            foreach (var comp in entityManager.ComponentManager.EntityQuery<ParticleAcceleratorControlBoxComponent>())
            {
                comp.RescanParts();
                comp.SetStrength(ParticleAcceleratorPowerState.Level0);
                comp.SwitchOn();
            }
            shell.WriteLine("Done!");
        }
    }
}
