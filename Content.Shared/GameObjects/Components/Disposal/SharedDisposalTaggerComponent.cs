﻿#nullable enable
using System;
using System.Text.RegularExpressions;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Disposal
{
    public class SharedDisposalTaggerComponent : Component
    {
        public override string Name => "DisposalTagger";

        public static readonly Regex TagRegex = new Regex("^[a-zA-Z0-9 ]*$", RegexOptions.Compiled);

        [Serializable, NetSerializable]
        public class DisposalTaggerUserInterfaceState : BoundUserInterfaceState
        {
            public readonly string Tag;

            public DisposalTaggerUserInterfaceState(string tag)
            {
                Tag = tag;
            }
        }

        [Serializable, NetSerializable]
        public class UiActionMessage : BoundUserInterfaceMessage
        {
            public readonly UiAction Action;
            public readonly string Tag = "";

            public UiActionMessage(UiAction action, string tag)
            {
                Action = action;

                if (Action == UiAction.Ok)
                {
                    Tag = tag.Substring(0, Math.Min(tag.Length, 30));
                }
            }
        }

        [Serializable, NetSerializable]
        public enum UiAction
        {
            Ok
        }

        [Serializable, NetSerializable]
        public enum DisposalTaggerUiKey
        {
            Key
        }
    }
}
