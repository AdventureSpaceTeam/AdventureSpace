using System;
using Content.Server.Administration.Logs;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Binary.Components;
using Content.Server.Atmos.Piping.Components;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Piping.Binary.Components;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Timing;

namespace Content.Server.Atmos.Piping.Binary.EntitySystems
{
    [UsedImplicitly]
    public class GasVolumePumpSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
        [Dependency] private UserInterfaceSystem _userInterfaceSystem = default!;
        [Dependency] private AdminLogSystem _adminLogSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GasVolumePumpComponent, AtmosDeviceUpdateEvent>(OnVolumePumpUpdated);
            SubscribeLocalEvent<GasVolumePumpComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<GasVolumePumpComponent, InteractHandEvent>(OnPumpInteractHand);
            // Bound UI subscriptions
            SubscribeLocalEvent<GasVolumePumpComponent, GasVolumePumpChangeTransferRateMessage>(OnTransferRateChangeMessage);
            SubscribeLocalEvent<GasVolumePumpComponent, GasVolumePumpToggleStatusMessage>(OnToggleStatusMessage);
        }

        private void OnExamined(EntityUid uid, GasVolumePumpComponent pump, ExaminedEvent args)
        {
            if (!EntityManager.GetComponent<TransformComponent>(pump.Owner).Anchored || !args.IsInDetailsRange) // Not anchored? Out of range? No status.
                return;

            if (Loc.TryGetString("gas-volume-pump-system-examined", out var str,
                        ("statusColor", "lightblue"), // TODO: change with volume?
                        ("rate", pump.TransferRate)
            ))
                args.PushMarkup(str);
        }

        private void OnVolumePumpUpdated(EntityUid uid, GasVolumePumpComponent pump, AtmosDeviceUpdateEvent args)
        {
            if (!pump.Enabled)
                return;

            if (!EntityManager.TryGetComponent(uid, out NodeContainerComponent? nodeContainer))
                return;

            if (!EntityManager.TryGetComponent(uid, out AtmosDeviceComponent? device))
                return;

            if (!nodeContainer.TryGetNode(pump.InletName, out PipeNode? inlet)
                || !nodeContainer.TryGetNode(pump.OutletName, out PipeNode? outlet))
                return;

            var inputStartingPressure = inlet.Air.Pressure;
            var outputStartingPressure = outlet.Air.Pressure;

            // Pump mechanism won't do anything if the pressure is too high/too low unless you overclock it.
            if ((inputStartingPressure < pump.LowerThreshold) || (outputStartingPressure > pump.HigherThreshold) && !pump.Overclocked)
                return;

            // Overclocked pumps can only force gas a certain amount.
            if ((outputStartingPressure - inputStartingPressure > pump.OverclockThreshold) && pump.Overclocked)
                return;

            // We multiply the transfer rate in L/s by the seconds passed since the last process to get the liters.
            var transferRatio = (float)(pump.TransferRate * (_gameTiming.CurTime - device.LastProcess).TotalSeconds) / inlet.Air.Volume;

            var removed = inlet.Air.RemoveRatio(transferRatio);

            // Some of the gas from the mixture leaks when overclocked.
            if (pump.Overclocked)
            {
                var tile = _atmosphereSystem.GetTileMixture(EntityManager.GetComponent<TransformComponent>(pump.Owner).Coordinates, true);

                if (tile != null)
                {
                    var leaked = removed.RemoveRatio(pump.LeakRatio);
                    _atmosphereSystem.Merge(tile, leaked);
                }
            }

            outlet.AssumeAir(removed);
        }

        private void OnPumpInteractHand(EntityUid uid, GasVolumePumpComponent component, InteractHandEvent args)
        {
            if (!EntityManager.TryGetComponent(args.User, out ActorComponent? actor))
                return;

            if (EntityManager.GetComponent<TransformComponent>(component.Owner).Anchored)
            {
                _userInterfaceSystem.TryOpen(uid, GasVolumePumpUiKey.Key, actor.PlayerSession);
                DirtyUI(uid, component);
            }
            else
            {
                args.User.PopupMessageCursor(Loc.GetString("comp-gas-pump-ui-needs-anchor"));
            }

            args.Handled = true;
        }

        private void OnToggleStatusMessage(EntityUid uid, GasVolumePumpComponent pump, GasVolumePumpToggleStatusMessage args)
        {
            pump.Enabled = args.Enabled;
            _adminLogSystem.Add(LogType.AtmosPowerChanged, LogImpact.Medium,
                $"{ToPrettyString(args.Session.AttachedEntity!.Value):player} set the power on {ToPrettyString(uid):device} to {args.Enabled}");
            DirtyUI(uid, pump);
        }

        private void OnTransferRateChangeMessage(EntityUid uid, GasVolumePumpComponent pump, GasVolumePumpChangeTransferRateMessage args)
        {
            pump.TransferRate = Math.Clamp(args.TransferRate, 0f, Atmospherics.MaxTransferRate);
            _adminLogSystem.Add(LogType.AtmosVolumeChanged, LogImpact.Medium,
                $"{ToPrettyString(args.Session.AttachedEntity!.Value):player} set the transfer rate on {ToPrettyString(uid):device} to {args.TransferRate}");
            DirtyUI(uid, pump);
        }

        private void DirtyUI(EntityUid uid, GasVolumePumpComponent? pump)
        {
            if (!Resolve(uid, ref pump))
                return;

            _userInterfaceSystem.TrySetUiState(uid, GasVolumePumpUiKey.Key,
                new GasVolumePumpBoundUserInterfaceState(EntityManager.GetComponent<MetaDataComponent>(pump.Owner).EntityName, pump.TransferRate, pump.Enabled));
        }
    }
}
