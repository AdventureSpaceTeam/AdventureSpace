﻿using Robust.Shared.Input;

namespace Content.Shared.Input
{
    [KeyFunctions]
    public static class ContentKeyFunctions
    {
        public static readonly BoundKeyFunction UseOrAttack = "UseOrAttack";
        public static readonly BoundKeyFunction Attack = "Attack";
        public static readonly BoundKeyFunction ActivateItemInHand = "ActivateItemInHand";
        public static readonly BoundKeyFunction ActivateItemInWorld = "ActivateItemInWorld"; // default action on world entity
        public static readonly BoundKeyFunction Drop = "Drop";
        public static readonly BoundKeyFunction ExamineEntity = "ExamineEntity";
        public static readonly BoundKeyFunction FocusChat = "FocusChatWindow";
        public static readonly BoundKeyFunction OpenCharacterMenu = "OpenCharacterMenu";
        public static readonly BoundKeyFunction OpenContextMenu = "OpenContextMenu";
        public static readonly BoundKeyFunction OpenCraftingMenu = "OpenCraftingMenu";
        public static readonly BoundKeyFunction OpenInventoryMenu = "OpenInventoryMenu";
        public static readonly BoundKeyFunction OpenTutorial = "OpenTutorial";
        public static readonly BoundKeyFunction SwapHands = "SwapHands";
        public static readonly BoundKeyFunction ThrowItemInHand = "ThrowItemInHand";
        public static readonly BoundKeyFunction ToggleCombatMode = "ToggleCombatMode";
        public static readonly BoundKeyFunction MouseMiddle = "MouseMiddle";
        public static readonly BoundKeyFunction OpenEntitySpawnWindow = "OpenEntitySpawnWindow";
        public static readonly BoundKeyFunction OpenSandboxWindow = "OpenSandboxWindow";
        public static readonly BoundKeyFunction OpenTileSpawnWindow = "OpenTileSpawnWindow";
    }
}
