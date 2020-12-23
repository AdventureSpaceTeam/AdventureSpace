﻿#nullable enable
using System;
using Content.Shared.Alert;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.Utility;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Content.Client.UserInterface.Controls
{
    public class AlertControl : BaseButton
    {
        // shorter than default tooltip delay so user can more easily
        // see what alerts they have
        private const float CustomTooltipDelay = 0.5f;

        public AlertPrototype Alert { get; }

        /// <summary>
        /// Current cooldown displayed in this slot. Set to null to show no cooldown.
        /// </summary>
        public (TimeSpan Start, TimeSpan End)? Cooldown
        {
            get => _cooldown;
            set
            {
                _cooldown = value;
                if (SuppliedTooltip is ActionAlertTooltip actionAlertTooltip)
                {
                    actionAlertTooltip.Cooldown = value;
                }
            }
        }
        private (TimeSpan Start, TimeSpan End)? _cooldown;

        private short? _severity;
        private readonly IGameTiming _gameTiming;
        private readonly TextureRect _icon;
        private readonly CooldownGraphic _cooldownGraphic;

        /// <summary>
        /// Creates an alert control reflecting the indicated alert + state
        /// </summary>
        /// <param name="alert">alert to display</param>
        /// <param name="severity">severity of alert, null if alert doesn't have severity levels</param>
        public AlertControl(AlertPrototype alert, short? severity)
        {
            _gameTiming = IoCManager.Resolve<IGameTiming>();
            TooltipDelay = CustomTooltipDelay;
            TooltipSupplier = SupplyTooltip;
            Alert = alert;
            _severity = severity;
            var texture = alert.GetIcon(_severity).Frame0();
            _icon = new TextureRect
            {
                TextureScale = (2, 2),
                Texture = texture
            };

            Children.Add(_icon);
            _cooldownGraphic = new CooldownGraphic();
            Children.Add(_cooldownGraphic);

        }

        private Control SupplyTooltip(Control? sender)
        {
            return new ActionAlertTooltip(Alert.Name, Alert.Description) {Cooldown = Cooldown};
        }

        /// <summary>
        /// Change the alert severity, changing the displayed icon
        /// </summary>
        public void SetSeverity(short? severity)
        {
            if (_severity != severity)
            {
                _severity = severity;
                _icon.Texture = Alert.GetIcon(_severity).Frame0();
            }
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);
            if (!Cooldown.HasValue)
            {
                _cooldownGraphic.Visible = false;
                _cooldownGraphic.Progress = 0;
                return;
            }

            var duration = Cooldown.Value.End - Cooldown.Value.Start;
            var curTime = _gameTiming.CurTime;
            var length = duration.TotalSeconds;
            var progress = (curTime - Cooldown.Value.Start).TotalSeconds / length;
            var ratio = (progress <= 1 ? (1 - progress) : (curTime - Cooldown.Value.End).TotalSeconds * -5);

            _cooldownGraphic.Progress = MathHelper.Clamp((float)ratio, -1, 1);
            _cooldownGraphic.Visible = ratio > -1f;
        }
    }
}
