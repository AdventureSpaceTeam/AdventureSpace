﻿using Content.Shared.GameObjects.Components.MachineLinking;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.GameObjects.Verbs;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.GameObjects.Components.MachineLinking
{
    [RegisterComponent]
    public class SignalSwitchComponent : Component, IInteractHand, IActivate
    {
        public override string Name => "SignalSwitch";

        [DataField("on")]
        private bool _on;

        public override void Initialize()
        {
            base.Initialize();

            UpdateSprite();
        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            TransmitSignal(eventArgs.User);
        }

        bool IInteractHand.InteractHand(InteractHandEventArgs eventArgs)
        {
            TransmitSignal(eventArgs.User);
            return true;
        }

        public void TransmitSignal(IEntity user)
        {
            _on = !_on;

            UpdateSprite();

            if (!Owner.TryGetComponent<SignalTransmitterComponent>(out var transmitter))
            {
                return;
            }

            if (!transmitter.TransmitSignal(_on))
            {
                Owner.PopupMessage(user, Loc.GetString("No receivers connected."));
            }
        }

        private void UpdateSprite()
        {
            if (Owner.TryGetComponent(out AppearanceComponent? appearance))
            {
                appearance.SetData(SignalSwitchVisuals.On, _on);
            }
        }

        [Verb]
        private sealed class ToggleSwitchVerb : Verb<SignalSwitchComponent>
        {
            protected override void Activate(IEntity user, SignalSwitchComponent component)
            {
                component.TransmitSignal(user);
            }

            protected override void GetData(IEntity user, SignalSwitchComponent component, VerbData data)
            {
                if (!ActionBlockerSystem.CanInteract(user))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.Text = Loc.GetString("Toggle Switch");
                data.Visibility = VerbVisibility.Visible;
            }
        }
    }
}
