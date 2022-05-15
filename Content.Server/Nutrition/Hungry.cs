using Content.Server.Administration;
using Content.Server.Nutrition.Components;
using Content.Shared.Administration;
using Content.Shared.Nutrition.Components;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.Nutrition
{
    [AdminCommand(AdminFlags.Debug)]
    public sealed class Hungry : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entities = default!;

        public string Command => "hungry";
        public string Description => "Makes you hungry.";
        public string Help => $"{Command}";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            if (player == null)
            {
                shell.WriteLine("You cannot use this command unless you are a player.");
                return;
            }

            if (player.AttachedEntity is not {Valid: true} playerEntity)
            {
                shell.WriteLine("You cannot use this command without an entity.");
                return;
            }

            if (!_entities.TryGetComponent(playerEntity, out HungerComponent? hunger))
            {
                shell.WriteLine($"Your entity does not have a {nameof(HungerComponent)} component.");
                return;
            }

            var hungryThreshold = hunger.HungerThresholds[HungerThreshold.Starving];
            hunger.CurrentHunger = hungryThreshold;
        }
    }
}
