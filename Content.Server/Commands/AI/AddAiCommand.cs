﻿#nullable enable
using Content.Server.Administration;
using Content.Server.AI.Utility;
using Content.Server.AI.Utility.AiLogic;
using Content.Server.GameObjects.Components.Movement;
using Content.Server.GameObjects.EntitySystems.AI;
using Content.Shared.Administration;
using Content.Shared.GameObjects.Components.Movement;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Commands.AI
{
    [AdminCommand(AdminFlags.Fun)]
    public class AddAiCommand : IConsoleCommand
    {
        public string Command => "addai";
        public string Description => "Add an ai component with a given processor to an entity.";
        public string Help => "Usage: addai <entityId> <behaviorSet1> <behaviorSet2>..."
                              + "\n    entityID: Uid of entity to add the AiControllerComponent to. Open its VV menu to find this."
                              + "\n    behaviorSet: Name of a behaviorset to add to the component on initialize.";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if(args.Length < 1)
            {
                shell.WriteLine("Wrong number of args.");
                return;
            }

            var entId = new EntityUid(int.Parse(args[0]));

            if (!IoCManager.Resolve<IEntityManager>().TryGetEntity(entId, out var ent))
            {
                shell.WriteLine($"Unable to find entity with uid {entId}");
                return;
            }

            if (ent.HasComponent<AiControllerComponent>())
            {
                shell.WriteLine("Entity already has an AI component.");
                return;
            }

            // TODO: IMover refffaaccctttooorrr
            if (ent.HasComponent<IMoverComponent>())
            {
                ent.RemoveComponent<IMoverComponent>();
            }

            var comp = ent.AddComponent<UtilityAi>();
            var behaviorManager = IoCManager.Resolve<INpcBehaviorManager>();

            for (var i = 1; i < args.Length; i++)
            {
                var bSet = args[i];
                behaviorManager.AddBehaviorSet(comp, bSet, false);
            }

            behaviorManager.RebuildActions(comp);
            shell.WriteLine("AI component added.");
        }
    }
}
