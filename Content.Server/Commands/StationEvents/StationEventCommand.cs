#nullable enable
using Content.Server.Administration;
using Content.Server.GameObjects.EntitySystems.StationEvents;
using Content.Shared.Administration;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Console;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Localization;

namespace Content.Server.Commands.StationEvents
{
    [AdminCommand(AdminFlags.Server)]
    public sealed class StationEventCommand : IConsoleCommand
    {
        public string Command => "events";
        public string Description => "Provides admin control to station events";
        public string Help => $"events <running/list/pause/resume/stop/run <eventName/random>>\n{RunningHelp}\n{ListHelp}\n{PauseHelp}\n{ResumeHelp}\n{RunHelp}";

        private const string RunningHelp = "running: return the current running event";

        private const string ListHelp = "list: return all event names that can be run";

        private const string PauseHelp = "pause: stop all random events from running and any one currently running";

        private const string ResumeHelp = "resume: allow random events to run again";

        private const string RunHelp =
            "run <eventName/random>: start a particular event now; <eventName> is case-insensitive and not localized";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            if (args.Length == 0)
            {
                shell.WriteLine($"Invalid amount of arguments.\n{Help}");
                return;
            }

            switch (args[0])
            {
                case "list":
                    List(shell, player);
                    break;
                case "running":
                    Running(shell, player);
                    break;
                // Didn't use a "toggle" so it's explicit
                case "pause":
                    Pause(shell, player);
                    break;
                case "resume":
                    Resume(shell, player);
                    break;
                case "stop":
                    Stop(shell, player);
                    break;
                case "run":
                    if (args.Length != 2)
                    {
                        shell.WriteLine($"Need 2 arguments, there were {args.Length}.\n{RunHelp}");
                        break;
                    }

                    Run(shell, player, args[1]);
                    break;
                default:
                    shell.WriteLine(Loc.GetString($"Invalid events command.\n{Help}"));
                    break;
            }
        }

        private void Run(IConsoleShell shell, IPlayerSession? player, string eventName)
        {
            var stationSystem = EntitySystem.Get<StationEventSystem>();

            var resultText = eventName == "random"
                ? stationSystem.RunRandomEvent()
                : stationSystem.RunEvent(eventName);

            shell.WriteLine(resultText);
        }

        private void Running(IConsoleShell shell, IPlayerSession? player)
        {
            var eventName = EntitySystem.Get<StationEventSystem>().CurrentEvent?.Name;
            if (!string.IsNullOrEmpty(eventName))
            {
                shell.WriteLine(eventName);
            }
            else
            {
                shell.WriteLine(Loc.GetString("No station event running"));
            }
        }

        private void List(IConsoleShell shell, IPlayerSession? player)
        {
            var resultText = "Random\n" + EntitySystem.Get<StationEventSystem>().GetEventNames();
            shell.WriteLine(resultText);
        }

        private void Pause(IConsoleShell shell, IPlayerSession? player)
        {
            var stationEventSystem = EntitySystem.Get<StationEventSystem>();

            if (!stationEventSystem.Enabled)
            {
                shell.WriteLine(Loc.GetString("Station events are already paused"));
            }
            else
            {
                stationEventSystem.Enabled = false;
                shell.WriteLine(Loc.GetString("Station events paused"));
            }
        }

        private void Resume(IConsoleShell shell, IPlayerSession? player)
        {
            var stationEventSystem = EntitySystem.Get<StationEventSystem>();

            if (stationEventSystem.Enabled)
            {
                shell.WriteLine(Loc.GetString("Station events are already running"));
            }
            else
            {
                stationEventSystem.Enabled = true;
                shell.WriteLine(Loc.GetString("Station events resumed"));
            }
        }

        private void Stop(IConsoleShell shell, IPlayerSession? player)
        {
            var resultText = EntitySystem.Get<StationEventSystem>().StopEvent();
            shell.WriteLine(resultText);
        }
    }
}
