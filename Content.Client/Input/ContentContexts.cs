﻿using Content.Shared.Input;
using Robust.Shared.Input;

namespace Content.Client.Input
{
    /// <summary>
    ///     Contains a helper function for setting up all content
    ///     contexts, and modifying existing engine ones.
    /// </summary>
    public static class ContentContexts
    {
        public static void SetupContexts(IInputContextContainer contexts)
        {
            var common = contexts.GetContext("common");
            common.AddFunction(ContentKeyFunctions.FocusChat);
            common.AddFunction(ContentKeyFunctions.FocusOOC);
            common.AddFunction(ContentKeyFunctions.FocusAdminChat);
            common.AddFunction(ContentKeyFunctions.ExamineEntity);
            common.AddFunction(ContentKeyFunctions.OpenTutorial);
            common.AddFunction(ContentKeyFunctions.TakeScreenshot);
            common.AddFunction(ContentKeyFunctions.TakeScreenshotNoUI);
            common.AddFunction(ContentKeyFunctions.Point);

            var human = contexts.GetContext("human");
            human.AddFunction(ContentKeyFunctions.SwapHands);
            human.AddFunction(ContentKeyFunctions.Drop);
            human.AddFunction(ContentKeyFunctions.ActivateItemInHand);
            human.AddFunction(ContentKeyFunctions.OpenCharacterMenu);
            human.AddFunction(ContentKeyFunctions.ActivateItemInWorld);
            human.AddFunction(ContentKeyFunctions.ThrowItemInHand);
            human.AddFunction(ContentKeyFunctions.TryPullObject);
            human.AddFunction(ContentKeyFunctions.MovePulledObject);
            human.AddFunction(ContentKeyFunctions.ReleasePulledObject);
            human.AddFunction(ContentKeyFunctions.OpenContextMenu);
            human.AddFunction(ContentKeyFunctions.OpenCraftingMenu);
            human.AddFunction(ContentKeyFunctions.OpenInventoryMenu);
            human.AddFunction(ContentKeyFunctions.SmartEquipBackpack);
            human.AddFunction(ContentKeyFunctions.SmartEquipBelt);
            human.AddFunction(ContentKeyFunctions.MouseMiddle);
            human.AddFunction(ContentKeyFunctions.ToggleCombatMode);
            human.AddFunction(ContentKeyFunctions.WideAttack);
            human.AddFunction(ContentKeyFunctions.ArcadeUp);
            human.AddFunction(ContentKeyFunctions.ArcadeDown);
            human.AddFunction(ContentKeyFunctions.ArcadeLeft);
            human.AddFunction(ContentKeyFunctions.ArcadeRight);
            human.AddFunction(ContentKeyFunctions.Arcade1);
            human.AddFunction(ContentKeyFunctions.Arcade2);
            human.AddFunction(ContentKeyFunctions.Arcade3);

            var ghost = contexts.New("ghost", "common");
            ghost.AddFunction(EngineKeyFunctions.MoveUp);
            ghost.AddFunction(EngineKeyFunctions.MoveDown);
            ghost.AddFunction(EngineKeyFunctions.MoveLeft);
            ghost.AddFunction(EngineKeyFunctions.MoveRight);
            ghost.AddFunction(EngineKeyFunctions.Walk);
            ghost.AddFunction(ContentKeyFunctions.OpenContextMenu);

            common.AddFunction(ContentKeyFunctions.OpenEntitySpawnWindow);
            common.AddFunction(ContentKeyFunctions.OpenSandboxWindow);
            common.AddFunction(ContentKeyFunctions.OpenTileSpawnWindow);
            common.AddFunction(ContentKeyFunctions.OpenAdminMenu);
        }
    }
}
