﻿#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Players;
using Content.Server.Utility;
using Content.Shared.Damage;
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.Components.Medical;
using Content.Shared.GameObjects.Components.Mobs.State;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.GameObjects.Verbs;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.Interfaces.GameObjects;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Medical
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(SharedMedicalScannerComponent))]
    public class MedicalScannerComponent : SharedMedicalScannerComponent, IActivate, IDestroyAct
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        private static readonly TimeSpan InternalOpenAttemptDelay = TimeSpan.FromSeconds(0.5);
        private TimeSpan _lastInternalOpenAttempt;

        private ContainerSlot _bodyContainer = default!;
        private readonly Vector2 _ejectOffset = new(0f, 0f);

        [ViewVariables]
        private bool Powered => !Owner.TryGetComponent(out PowerReceiverComponent? receiver) || receiver.Powered;
        [ViewVariables]
        private BoundUserInterface? UserInterface => Owner.GetUIOrNull(MedicalScannerUiKey.Key);

        public bool IsOccupied => _bodyContainer.ContainedEntity != null;

        public override void Initialize()
        {
            base.Initialize();

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += OnUiReceiveMessage;
            }

            _bodyContainer = ContainerManagerComponent.Ensure<ContainerSlot>($"{Name}-bodyContainer", Owner);

            // TODO: write this so that it checks for a change in power events and acts accordingly.
            var newState = GetUserInterfaceState();
            UserInterface?.SetState(newState);

            UpdateUserInterface();
        }

        /// <inheritdoc />
        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
            base.HandleMessage(message, component);

            switch (message)
            {
                case RelayMovementEntityMessage msg:
                {
                    if (ActionBlockerSystem.CanInteract(msg.Entity))
                    {
                        if (_gameTiming.CurTime <
                            _lastInternalOpenAttempt + InternalOpenAttemptDelay)
                        {
                            break;
                        }

                        _lastInternalOpenAttempt = _gameTiming.CurTime;
                        EjectBody();
                    }
                    break;
                }
            }
        }

        private static readonly MedicalScannerBoundUserInterfaceState EmptyUIState =
            new(
                null,
                new Dictionary<DamageClass, int>(),
                new Dictionary<DamageType, int>(),
                false);

        private MedicalScannerBoundUserInterfaceState GetUserInterfaceState()
        {
            var body = _bodyContainer.ContainedEntity;
            if (body == null)
            {
                if (Owner.TryGetComponent(out AppearanceComponent? appearance))
                {
                    appearance?.SetData(MedicalScannerVisuals.Status, MedicalScannerStatus.Open);
                }

                return EmptyUIState;
            }

            if (!body.TryGetComponent(out IDamageableComponent? damageable))
            {
                return EmptyUIState;
            }

            var classes = new Dictionary<DamageClass, int>(damageable.DamageClasses);
            var types = new Dictionary<DamageType, int>(damageable.DamageTypes);

            if (_bodyContainer.ContainedEntity?.Uid == null)
            {
                return new MedicalScannerBoundUserInterfaceState(body.Uid, classes, types, true);
            }

            var cloningSystem = EntitySystem.Get<CloningSystem>();
            var scanned = _bodyContainer.ContainedEntity.TryGetComponent(out MindComponent? mindComponent) &&
                         mindComponent.Mind != null &&
                         cloningSystem.HasDnaScan(mindComponent.Mind);

            return new MedicalScannerBoundUserInterfaceState(body.Uid, classes, types, scanned);
        }

        private void UpdateUserInterface()
        {
            if (!Powered)
            {
                return;
            }

            var newState = GetUserInterfaceState();
            UserInterface?.SetState(newState);
        }

        private MedicalScannerStatus GetStatusFromDamageState(IMobStateComponent state)
        {
            if (state.IsAlive())
            {
                return MedicalScannerStatus.Green;
            }
            else if (state.IsCritical())
            {
                return MedicalScannerStatus.Red;
            }
            else if (state.IsDead())
            {
                return MedicalScannerStatus.Death;
            }
            else
            {
                return MedicalScannerStatus.Yellow;
            }
        }

        private MedicalScannerStatus GetStatus()
        {
            if (Powered)
            {
                var body = _bodyContainer.ContainedEntity;
                var state = body?.GetComponentOrNull<IMobStateComponent>();

                return state == null
                    ? MedicalScannerStatus.Open
                    : GetStatusFromDamageState(state);
            }

            return MedicalScannerStatus.Off;
        }

        private void UpdateAppearance()
        {
            if (Owner.TryGetComponent(out AppearanceComponent? appearance))
            {
                appearance.SetData(MedicalScannerVisuals.Status, GetStatus());
            }
        }

        void IActivate.Activate(ActivateEventArgs args)
        {
            if (!args.User.TryGetComponent(out IActorComponent? actor))
            {
                return;
            }

            if (!Powered)
                return;

            UserInterface?.Open(actor.playerSession);
        }

        [Verb]
        public sealed class EnterVerb : Verb<MedicalScannerComponent>
        {
            protected override void GetData(IEntity user, MedicalScannerComponent component, VerbData data)
            {
                if (!ActionBlockerSystem.CanInteract(user))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.Text = Loc.GetString("Enter");
                data.Visibility = component.IsOccupied ? VerbVisibility.Invisible : VerbVisibility.Visible;
            }

            protected override void Activate(IEntity user, MedicalScannerComponent component)
            {
                component.InsertBody(user);
            }
        }

        [Verb]
        public sealed class EjectVerb : Verb<MedicalScannerComponent>
        {
            protected override void GetData(IEntity user, MedicalScannerComponent component, VerbData data)
            {
                if (!ActionBlockerSystem.CanInteract(user))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.Text = Loc.GetString("Eject");
                data.Visibility = component.IsOccupied ? VerbVisibility.Visible : VerbVisibility.Invisible;
            }

            protected override void Activate(IEntity user, MedicalScannerComponent component)
            {
                component.EjectBody();
            }
        }

        public void InsertBody(IEntity user)
        {
            _bodyContainer.Insert(user);
            UpdateUserInterface();
            UpdateAppearance();
        }

        public void EjectBody()
        {
            var containedEntity = _bodyContainer.ContainedEntity;
            if (containedEntity == null) return;
            _bodyContainer.Remove(containedEntity);
            containedEntity.Transform.WorldPosition += _ejectOffset;
            UpdateUserInterface();
            UpdateAppearance();
        }

        public void Update(float frameTime)
        {
            UpdateUserInterface();
            UpdateAppearance();
        }

        private void OnUiReceiveMessage(ServerBoundUserInterfaceMessage obj)
        {
            if (obj.Message is not UiButtonPressedMessage message) return;

            switch (message.Button)
            {
                case UiButton.ScanDNA:
                    if (_bodyContainer.ContainedEntity != null)
                    {
                        //TODO: Show a 'ERROR: Body is completely devoid of soul' if no Mind owns the entity.
                        var cloningSystem = EntitySystem.Get<CloningSystem>();

                        if (!_bodyContainer.ContainedEntity.TryGetComponent(out MindComponent? mind) || !mind.HasMind)
                            break;

                        cloningSystem.AddToDnaScans(mind.Mind);
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override bool DragDropOn(DragDropEventArgs eventArgs)
        {
            _bodyContainer.Insert(eventArgs.Dragged);
            return true;
        }

        void IDestroyAct.OnDestroy(DestructionEventArgs eventArgs)
        {
            EjectBody();
        }
    }
}
