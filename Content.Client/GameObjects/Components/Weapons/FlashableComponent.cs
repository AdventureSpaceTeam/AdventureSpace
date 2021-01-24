﻿using System;
using System.Threading;
using Content.Client.Graphics.Overlays;
using Content.Shared.GameObjects.Components.Weapons;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Graphics.Overlays;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.Interfaces.Graphics.Overlays;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Timers;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using Timer = Robust.Shared.Timers.Timer;

namespace Content.Client.GameObjects.Components.Weapons
{
    [RegisterComponent]
    public sealed class FlashableComponent : SharedFlashableComponent
    {
        private TimeSpan _startTime;
        private double _duration;

        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
        {
            if (curState == null)
            {
                return;
            }

            var playerManager = IoCManager.Resolve<IPlayerManager>();
            if (playerManager?.LocalPlayer != null && playerManager.LocalPlayer.ControlledEntity != Owner)
            {
                return;
            }

            var newState = (FlashComponentState) curState;
            if (newState.Time == default)
            {
                return;
            }

            // Few things here:
            // 1. If a shorter duration flash is applied then don't do anything
            // 2. If the client-side time is later than when the flash should've ended don't do anything
            var currentTime = IoCManager.Resolve<IGameTiming>().CurTime.TotalSeconds;
            var newEndTime = newState.Time.TotalSeconds + newState.Duration;
            var currentEndTime = _startTime.TotalSeconds + _duration;

            if (currentEndTime > newEndTime)
            {
                return;
            }

            if (currentTime > newEndTime)
            {
                return;
            }

            _startTime = newState.Time;
            _duration = newState.Duration;

            var overlayManager = IoCManager.Resolve<IOverlayManager>();
            var overlay = overlayManager.GetOverlay<FlashOverlay>(nameof(FlashOverlay));
            overlay.ReceiveFlash(_duration);
        }
    }
}
