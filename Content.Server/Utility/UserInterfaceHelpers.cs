﻿#nullable enable
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.Utility
{
    public static class UserInterfaceHelpers
    {
        public static BoundUserInterface? GetUIOrNull(this IEntity entity, object uiKey)
        {
            return entity.GetComponentOrNull<ServerUserInterfaceComponent>()?.GetBoundUserInterfaceOrNull(uiKey);
        }
    }
}
