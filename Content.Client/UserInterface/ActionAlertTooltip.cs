﻿#nullable enable

using System;
using Content.Client.UserInterface.Stylesheets;
using Content.Shared.Actions;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface
{
    /// <summary>
    /// Tooltip for actions or alerts because they are very similar.
    /// </summary>
    public class ActionAlertTooltip : PanelContainer
    {
        private const float TooltipTextMaxWidth = 350;

        private readonly RichTextLabel _cooldownLabel;
        private readonly IGameTiming _gameTiming;

        /// <summary>
        /// Current cooldown displayed in this tooltip. Set to null to show no cooldown.
        /// </summary>
        public (TimeSpan Start, TimeSpan End)? Cooldown { get; set; }

        public ActionAlertTooltip(FormattedMessage name, FormattedMessage? desc, string? requires = null)
        {
            _gameTiming = IoCManager.Resolve<IGameTiming>();

            SetOnlyStyleClass(StyleNano.StyleClassTooltipPanel);

            VBoxContainer vbox;
            AddChild(vbox = new VBoxContainer {RectClipContent = true});
            var nameLabel = new RichTextLabel
            {
                MaxWidth = TooltipTextMaxWidth,
                StyleClasses = {StyleNano.StyleClassTooltipActionTitle}
            };
            nameLabel.SetMessage(name);
            vbox.AddChild(nameLabel);

            if (desc != null && !string.IsNullOrWhiteSpace(desc.ToString()))
            {
                var description = new RichTextLabel
                {
                    MaxWidth = TooltipTextMaxWidth,
                    StyleClasses = {StyleNano.StyleClassTooltipActionDescription}
                };
                description.SetMessage(desc);
                vbox.AddChild(description);
            }

            vbox.AddChild(_cooldownLabel = new RichTextLabel
            {
                MaxWidth = TooltipTextMaxWidth,
                StyleClasses = {StyleNano.StyleClassTooltipActionCooldown},
                Visible = false
            });

            if (!string.IsNullOrWhiteSpace(requires))
            {
                var requiresLabel = new RichTextLabel
                {
                    MaxWidth = TooltipTextMaxWidth,
                    StyleClasses = {StyleNano.StyleClassTooltipActionRequirements}
                };
                requiresLabel.SetMessage(FormattedMessage.FromMarkup("[color=#635c5c]" +
                                                                     requires +
                                                                     "[/color]"));
                vbox.AddChild(requiresLabel);
            }
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);
            if (!Cooldown.HasValue)
            {
                _cooldownLabel.Visible = false;
                return;
            }

            var timeLeft = Cooldown.Value.End - _gameTiming.CurTime;
            if (timeLeft > TimeSpan.Zero)
            {
                var duration = Cooldown.Value.End - Cooldown.Value.Start;
                _cooldownLabel.SetMessage(FormattedMessage.FromMarkup(
                    $"[color=#a10505]{duration.Seconds} sec cooldown ({timeLeft.Seconds + 1} sec remaining)[/color]"));
                _cooldownLabel.Visible = true;
            }
            else
            {
                _cooldownLabel.Visible = false;
            }
        }
    }
}
