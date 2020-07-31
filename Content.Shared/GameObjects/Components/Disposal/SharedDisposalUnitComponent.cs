﻿using System;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Disposal
{
    public abstract class SharedDisposalUnitComponent : Component
    {
        public override string Name => "DisposalUnit";

        [Serializable, NetSerializable]
        public enum Visuals
        {
            VisualState,
            Handle,
            Light
        }

        [Serializable, NetSerializable]
        public enum VisualState
        {
            UnAnchored,
            Anchored,
            Flushing
        }

        [Serializable, NetSerializable]
        public enum HandleState
        {
            Normal,
            Engaged
        }

        [Serializable, NetSerializable]
        public enum LightState
        {
            Off,
            Charging,
            Full,
            Ready
        }

        [Serializable, NetSerializable]
        public enum UiButton
        {
            Eject,
            Engage,
            Power
        }

        [Serializable, NetSerializable]
        public enum State
        {
            Ready,
            Pressurizing
        }

        [Serializable, NetSerializable]
        public class DisposalUnitBoundUserInterfaceState : BoundUserInterfaceState
        {
            public readonly string UnitName;
            public readonly string UnitState;
            public readonly float Pressure;
            public readonly bool Powered;
            public readonly bool Engaged;

            public DisposalUnitBoundUserInterfaceState(string unitName, string unitState, float pressure, bool powered,
                bool engaged)
            {
                UnitName = unitName;
                UnitState = unitState;
                Pressure = pressure;
                Powered = powered;
                Engaged = engaged;
            }
        }

        /// <summary>
        ///     Message data sent from client to server when a disposal unit ui button is pressed.
        /// </summary>
        [Serializable, NetSerializable]
        public class UiButtonPressedMessage : BoundUserInterfaceMessage
        {
            public readonly UiButton Button;

            public UiButtonPressedMessage(UiButton button)
            {
                Button = button;
            }
        }

        [Serializable, NetSerializable]
        public enum DisposalUnitUiKey
        {
            Key
        }
    }
}
