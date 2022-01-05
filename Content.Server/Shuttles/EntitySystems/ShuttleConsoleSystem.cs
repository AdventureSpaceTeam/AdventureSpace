using System;
using Content.Server.Power.Components;
using Content.Server.Shuttles.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Alert;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Shuttles;
using Content.Shared.Shuttles.Components;
using Content.Shared.Tag;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Utility;

namespace Content.Server.Shuttles.EntitySystems
{
    internal sealed class ShuttleConsoleSystem : SharedShuttleConsoleSystem
    {
        [Dependency] private readonly ActionBlockerSystem _blocker = default!;
        [Dependency] private readonly AlertsSystem _alertsSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ShuttleConsoleComponent, ComponentShutdown>(HandleConsoleShutdown);
            SubscribeLocalEvent<PilotComponent, ComponentShutdown>(HandlePilotShutdown);
            SubscribeLocalEvent<ShuttleConsoleComponent, ActivateInWorldEvent>(HandleConsoleInteract);
            SubscribeLocalEvent<PilotComponent, MoveEvent>(HandlePilotMove);
            SubscribeLocalEvent<ShuttleConsoleComponent, PowerChangedEvent>(HandlePowerChange);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var toRemove = new RemQueue<PilotComponent>();

            foreach (var comp in EntityManager.EntityQuery<PilotComponent>())
            {
                if (comp.Console == null) continue;

                if (!_blocker.CanInteract((comp).Owner))
                {
                    toRemove.Add(comp);
                }
            }

            foreach (var comp in toRemove)
            {
                RemovePilot(comp);
            }
        }

        /// <summary>
        /// Console requires power to operate.
        /// </summary>
        private void HandlePowerChange(EntityUid uid, ShuttleConsoleComponent component, PowerChangedEvent args)
        {
            if (!args.Powered)
            {
                component.Enabled = false;

                ClearPilots(component);
            }
            else
            {
                component.Enabled = true;
            }
        }

        /// <summary>
        /// If pilot is moved then we'll stop them from piloting.
        /// </summary>
        private void HandlePilotMove(EntityUid uid, PilotComponent component, ref MoveEvent args)
        {
            if (component.Console == null || component.Position == null)
            {
                DebugTools.Assert(component.Position == null && component.Console == null);
                EntityManager.RemoveComponent<PilotComponent>(uid);
                return;
            }

            if (args.NewPosition.TryDistance(EntityManager, component.Position.Value, out var distance) &&
                distance < PilotComponent.BreakDistance) return;

            RemovePilot(component);
        }

        /// <summary>
        /// For now pilots just interact with the console and can start piloting with wasd.
        /// </summary>
        private void HandleConsoleInteract(EntityUid uid, ShuttleConsoleComponent component, ActivateInWorldEvent args)
        {
            if (!args.User.HasTag("CanPilot"))
            {
                return;
            }

            var pilotComponent = EntityManager.EnsureComponent<PilotComponent>(args.User);

            if (!component.Enabled)
            {
                args.User.PopupMessage($"Console is not powered.");
                return;
            }

            args.Handled = true;
            var console = pilotComponent.Console;

            if (console != null)
            {
                RemovePilot(pilotComponent);

                if (console == component)
                {
                    return;
                }
            }

            AddPilot(args.User, component);
        }

        private void HandlePilotShutdown(EntityUid uid, PilotComponent component, ComponentShutdown args)
        {
            RemovePilot(component);
        }

        private void HandleConsoleShutdown(EntityUid uid, ShuttleConsoleComponent component, ComponentShutdown args)
        {
            ClearPilots(component);
        }

        public void AddPilot(EntityUid entity, ShuttleConsoleComponent component)
        {
            if (!_blocker.CanInteract(entity) ||
                !EntityManager.TryGetComponent(entity, out PilotComponent? pilotComponent) ||
                component.SubscribedPilots.Contains(pilotComponent))
            {
                return;
            }

            component.SubscribedPilots.Add(pilotComponent);

            _alertsSystem.ShowAlert(entity, AlertType.PilotingShuttle);

            entity.PopupMessage(Loc.GetString("shuttle-pilot-start"));
            pilotComponent.Console = component;
            pilotComponent.Position = EntityManager.GetComponent<TransformComponent>(entity).Coordinates;
            pilotComponent.Dirty();
        }

        public void RemovePilot(PilotComponent pilotComponent)
        {
            var console = pilotComponent.Console;

            if (console is not ShuttleConsoleComponent helmsman) return;

            pilotComponent.Console = null;
            pilotComponent.Position = null;

            if (!helmsman.SubscribedPilots.Remove(pilotComponent)) return;

            _alertsSystem.ClearAlert(pilotComponent.Owner, AlertType.PilotingShuttle);

            pilotComponent.Owner.PopupMessage(Loc.GetString("shuttle-pilot-end"));

            if (pilotComponent.LifeStage < ComponentLifeStage.Stopping)
                EntityManager.RemoveComponent<PilotComponent>(pilotComponent.Owner);
        }

        public void RemovePilot(EntityUid entity)
        {
            if (!EntityManager.TryGetComponent(entity, out PilotComponent? pilotComponent)) return;

            RemovePilot(pilotComponent);
        }

        public void ClearPilots(ShuttleConsoleComponent component)
        {
            while (component.SubscribedPilots.TryGetValue(0, out var pilot))
            {
                RemovePilot(pilot);
            }
        }
    }
}
