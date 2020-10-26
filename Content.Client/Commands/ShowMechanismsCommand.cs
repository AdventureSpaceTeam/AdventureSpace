﻿using Content.Shared.GameObjects.Components.Body.Mechanism;
using Robust.Client.Console;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.Console;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.Commands
{
    public class ShowMechanismsCommand : IConsoleCommand
    {
        public const string CommandName = "showmechanisms";

        // ReSharper disable once StringLiteralTypo
        public string Command => CommandName;
        public string Description => "Makes mechanisms visible, even when they shouldn't be.";
        public string Help => $"{Command}";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            var componentManager = IoCManager.Resolve<IComponentManager>();
            var mechanisms = componentManager.EntityQuery<IMechanism>();

            foreach (var mechanism in mechanisms)
            {
                if (mechanism.Owner.TryGetComponent(out SpriteComponent sprite))
                {
                    sprite.ContainerOccluded = false;
                }
            }

            IoCManager.Resolve<IClientConsole>().ProcessCommand("showcontainedcontext");

            return false;
        }
    }
}
