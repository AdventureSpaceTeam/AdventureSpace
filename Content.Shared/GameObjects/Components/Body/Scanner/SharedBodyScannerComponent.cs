﻿#nullable enable
using System;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Body.Scanner
{
    public abstract class SharedBodyScannerComponent : Component
    {
        public override string Name => "BodyScanner";
    }

    [Serializable, NetSerializable]
    public enum BodyScannerUiKey
    {
        Key
    }

    [Serializable, NetSerializable]
    public class BodyScannerUIState : BoundUserInterfaceState
    {
        public readonly EntityUid Uid;

        public BodyScannerUIState(EntityUid uid)
        {
            Uid = uid;
        }
    }
}
