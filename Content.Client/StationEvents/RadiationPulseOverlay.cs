#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Content.Client.GameObjects.Components.StationEvents;
using Content.Shared.GameObjects.Components.Mobs;
using JetBrains.Annotations;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Graphics.Overlays;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Client.Player;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Content.Client.StationEvents
{
    [UsedImplicitly]
    public sealed class RadiationPulseOverlay : Overlay
    {
        [Dependency] private readonly IComponentManager _componentManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;

        /// <summary>
        /// Current color of a pulse
        /// </summary>
        private readonly Dictionary<IEntity, Color> _colors = new Dictionary<IEntity, Color>();

        /// <summary>
        /// Whether our alpha is increasing or decreasing and at what time does it flip (or stop)
        /// </summary>
        private readonly Dictionary<IEntity, (bool EasingIn, TimeSpan TransitionTime)> _transitions =
                     new Dictionary<IEntity, (bool EasingIn, TimeSpan TransitionTime)>();

        /// <summary>
        /// How much the alpha changes per second for each pulse
        /// </summary>
        private readonly Dictionary<IEntity, float> _alphaRateOfChange = new Dictionary<IEntity, float>();

        private TimeSpan _lastTick;

        // TODO: When worldHandle can do DrawCircle change this.
        public override OverlaySpace Space => OverlaySpace.ScreenSpace;

        public RadiationPulseOverlay() : base(nameof(SharedOverlayID.RadiationPulseOverlay))
        {
            IoCManager.InjectDependencies(this);
            _lastTick = _gameTiming.CurTime;
        }

        /// <summary>
        /// Get the current color for the entity,
        /// accounting for what its alpha should be and whether it should be transitioning in or out
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="elapsedTime">frametime</param>
        /// <param name="endTime"></param>
        /// <returns></returns>
        private Color GetColor(IEntity entity, float elapsedTime, TimeSpan endTime)
        {
            var currentTime = _gameTiming.CurTime;

            // New pulse
            if (!_colors.ContainsKey(entity))
            {
                UpdateTransition(entity, currentTime, endTime);
            }

            var currentColor = _colors[entity];
            var alphaChange = _alphaRateOfChange[entity] * elapsedTime;

            if (!_transitions[entity].EasingIn)
            {
                alphaChange *= -1;
            }

            if (currentTime > _transitions[entity].TransitionTime)
            {
                UpdateTransition(entity, currentTime, endTime);
            }

            _colors[entity] = _colors[entity].WithAlpha(currentColor.A + alphaChange);
            return _colors[entity];
        }

        private void UpdateTransition(IEntity entity, TimeSpan currentTime, TimeSpan endTime)
        {
            bool easingIn;
            TimeSpan transitionTime;

            if (!_transitions.TryGetValue(entity, out var transition))
            {
                // Start as false because it will immediately be flipped
                easingIn = false;
                transitionTime = (endTime - currentTime) / 2 + currentTime;
            }
            else
            {
                easingIn = transition.EasingIn;
                transitionTime = endTime;
            }

            _transitions[entity] = (!easingIn, transitionTime);
            _colors[entity] = Color.Green.WithAlpha(0.0f);
            _alphaRateOfChange[entity] = 1.0f / (float) (transitionTime - currentTime).TotalSeconds;
        }

        protected override void Draw(DrawingHandleBase handle, OverlaySpace currentSpace)
        {
            // PVS should control the overlay pretty well so the overlay doesn't get instantiated unless we're near one...
            var playerEntity = _playerManager.LocalPlayer?.ControlledEntity;

            if (playerEntity == null)
            {
                return;
            }

            var elapsedTime = (float) (_gameTiming.CurTime - _lastTick).TotalSeconds;
            _lastTick = _gameTiming.CurTime;

            var radiationPulses = _componentManager
                .EntityQuery<RadiationPulseComponent>()
                .ToList();

            var screenHandle = (DrawingHandleScreen) handle;
            var viewport = _eyeManager.GetWorldViewport();

            foreach (var grid in _mapManager.FindGridsIntersecting(playerEntity.Transform.MapID, viewport))
            {
                foreach (var pulse in radiationPulses)
                {
                    if (!pulse.Draw || grid.Index != pulse.Owner.Transform.GridID) continue;

                    // TODO: Check if viewport intersects circle
                    var circlePosition = _eyeManager.WorldToScreen(pulse.Owner.Transform.WorldPosition);

                    // change to worldhandle when implemented
                    screenHandle.DrawCircle(
                        circlePosition,
                        pulse.Range * 64,
                        GetColor(pulse.Owner, pulse.Decay ? elapsedTime : 0, pulse.EndTime));
                }
            }
        }
    }
}
