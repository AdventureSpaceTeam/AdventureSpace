using Content.Client.Markers;
using Content.Client.Popups;
using Content.Client.SubFloor;
using Content.Shared.SubFloor;
using Robust.Client.GameObjects;
using Robust.Shared.Console;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client.Commands
{
    internal sealed class ShowMarkersCommand : IConsoleCommand
    {
        // ReSharper disable once StringLiteralTypo
        public string Command => "showmarkers";
        public string Description => "Toggles visibility of markers such as spawn points.";
        public string Help => "";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            EntitySystem.Get<MarkerSystem>()
                .MarkersVisible ^= true;
        }
    }

    internal sealed class ShowSubFloor : IConsoleCommand
    {
        // ReSharper disable once StringLiteralTypo
        public string Command => "showsubfloor";
        public string Description => "Makes entities below the floor always visible.";
        public string Help => $"Usage: {Command}";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            EntitySystem.Get<SubFloorHideSystem>().ShowAll ^= true;
        }
    }

    internal sealed class ShowSubFloorForever : IConsoleCommand
    {
        // ReSharper disable once StringLiteralTypo
        public string Command => "showsubfloorforever";
        public string Description => "Makes entities below the floor always visible until the client is restarted.";
        public string Help => $"Usage: {Command}";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            EntitySystem.Get<SubFloorHideSystem>().ShowAll = true;

            var entMan = IoCManager.Resolve<IEntityManager>();
            var components = entMan.EntityQuery<SubFloorHideComponent>(true);

            foreach (var component in components)
            {
                if (entMan.TryGetComponent(component.Owner, out ISpriteComponent? sprite))
                {
                    sprite.DrawDepth = (int) DrawDepth.Overlays;
                }
            }
        }
    }

    internal sealed class NotifyCommand : IConsoleCommand
    {
        public string Command => "notify";
        public string Description => "Send a notify client side.";
        public string Help => "notify <message>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var message = args[0];

            EntitySystem.Get<PopupSystem>().PopupCursor(message);
        }
    }
}
