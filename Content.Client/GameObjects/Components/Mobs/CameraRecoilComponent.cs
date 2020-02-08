﻿using System;
using Content.Shared.GameObjects.Components.Mobs;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Log;
using Robust.Shared.Maths;

namespace Content.Client.GameObjects.Components.Mobs
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedCameraRecoilComponent))]
    public sealed class CameraRecoilComponent : SharedCameraRecoilComponent
    {
        // Maximum rate of magnitude restore towards 0 kick.
        private const float RestoreRateMax = 2f;

        // Minimum rate of magnitude restore towards 0 kick.
        private const float RestoreRateMin = 1f;

        // Time in seconds since the last kick that lerps RestoreRateMin and RestoreRateMax
        private const float RestoreRateRamp = 0.05f;

        // The maximum magnitude of the kick applied to the camera at any point.
        private const float KickMagnitudeMax = 0.25f;

        private Vector2 _currentKick;
        private float _lastKickTime;

        private EyeComponent _eye;

        // Basically I needed a way to chain this effect for the attack lunge animation.
        // Sorry!
        public Vector2 BaseOffset { get; set; }

        public override void Initialize()
        {
            base.Initialize();

            _eye = Owner.GetComponent<EyeComponent>();
        }

        public override void Kick(Vector2 recoil)
        {
            if (float.IsNaN(recoil.X) || float.IsNaN(recoil.Y))
            {
                Logger.Error($"CameraRecoilComponent on entity {Owner.Uid} passed a NaN recoil value. Ignoring.");
                return;
            }

            // Use really bad math to "dampen" kicks when we're already kicked.
            var existing = _currentKick.Length;
            var dampen = existing/KickMagnitudeMax;
            _currentKick += recoil * (1-dampen);
            if (_currentKick.Length > KickMagnitudeMax)
            {
                _currentKick = _currentKick.Normalized * KickMagnitudeMax;
            }

            _lastKickTime = 0;
            _updateEye();
        }

        public override void HandleMessage(ComponentMessage message, INetChannel netChannel = null, IComponent component = null)
        {
            base.HandleMessage(message, netChannel, component);

            switch (message)
            {
                case RecoilKickMessage msg:
                    Kick(msg.Recoil);
                    break;
            }
        }

        public void FrameUpdate(float frameTime)
        {
            var magnitude = _currentKick.Length;
            if (magnitude <= 0.005f)
            {
                _currentKick = Vector2.Zero;
                _updateEye();
                return;
            }

            // Continually restore camera to 0.
            var normalized = _currentKick.Normalized;
            var restoreRate = FloatMath.Lerp(RestoreRateMin, RestoreRateMax, Math.Min(1, _lastKickTime/RestoreRateRamp));
            var restore = normalized * restoreRate * frameTime;
            var (x, y) = _currentKick - restore;
            if (Math.Sign(x) != Math.Sign(_currentKick.X))
            {
                x = 0;
            }

            if (Math.Sign(y) != Math.Sign(_currentKick.Y))
            {
                y = 0;
            }

            _currentKick = (x, y);

            _updateEye();
        }

        private void _updateEye()
        {
            _eye.Offset = BaseOffset + _currentKick;
        }
    }
}
