using Content.Client.Interfaces.Chat;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Timers;
using Robust.Shared.Timing;

namespace Content.Client.Chat
{
    public class SpeechBubble : Control
    {
        /// <summary>
        ///     The total time a speech bubble stays on screen.
        /// </summary>
        private const float TotalTime = 4;

        /// <summary>
        ///     The amount of time at the end of the bubble's life at which it starts fading.
        /// </summary>
        private const float FadeTime = 0.25f;

        /// <summary>
        ///     The distance in world space to offset the speech bubble from the center of the entity.
        ///     i.e. greater -> higher above the mob's head.
        /// </summary>
        private const float EntityVerticalOffset = 0.5f;

        private readonly IEyeManager _eyeManager;
        private readonly IEntity _senderEntity;
        private readonly IChatManager _chatManager;

        private float _timeLeft = TotalTime;

        public float VerticalOffset { get; set; }
        private float _verticalOffsetAchieved;

        public float ContentHeight { get; private set; }

        public SpeechBubble(string text, IEntity senderEntity, IEyeManager eyeManager, IChatManager chatManager)
        {
            _chatManager = chatManager;
            _senderEntity = senderEntity;
            _eyeManager = eyeManager;

            MouseFilter = MouseFilterMode.Ignore;
            // Use text clipping so new messages don't overlap old ones being pushed up.
            RectClipContent = true;

            var label = new RichTextLabel
            {
                MaxWidth = 256,
                MouseFilter = MouseFilterMode.Ignore
            };
            label.SetMessage(text);

            var panel = new PanelContainer
            {
                StyleClasses = { "tooltipBox" },
                Children = { label },
                MouseFilter = MouseFilterMode.Ignore,
                ModulateSelfOverride = Color.White.WithAlpha(0.75f)
            };

            AddChild(panel);

            ForceRunStyleUpdate();

            ContentHeight = panel.CombinedMinimumSize.Y;
            _verticalOffsetAchieved = -ContentHeight;
        }

        protected override Vector2 CalculateMinimumSize()
        {
            return (base.CalculateMinimumSize().X, 0);
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);

            _timeLeft -= args.DeltaSeconds;

            if (_timeLeft <= FadeTime)
            {
                // Update alpha if we're fading.
                Modulate = Color.White.WithAlpha(_timeLeft / FadeTime);
            }

            if (_senderEntity.Deleted || _timeLeft <= 0)
            {
                // Timer spawn to prevent concurrent modification exception.
                Timer.Spawn(0, Die);
                return;
            }

            // Lerp to our new vertical offset if it's been modified.
            if (FloatMath.CloseTo(_verticalOffsetAchieved - VerticalOffset, 0, 0.1))
            {
                _verticalOffsetAchieved = VerticalOffset;
            }
            else
            {
                _verticalOffsetAchieved = FloatMath.Lerp(_verticalOffsetAchieved, VerticalOffset, 10 * args.DeltaSeconds);
            }

            var worldPos = _senderEntity.Transform.WorldPosition;
            worldPos += (0, EntityVerticalOffset);

            var lowerCenter = _eyeManager.WorldToScreen(worldPos) / UIScale;
            var screenPos = lowerCenter - (Width / 2, ContentHeight + _verticalOffsetAchieved);
            LayoutContainer.SetPosition(this, screenPos);

            var height = (lowerCenter.Y - screenPos.Y).Clamp(0, ContentHeight);
            LayoutContainer.SetSize(this, (Size.X, height));
        }

        private void Die()
        {
            if (Disposed)
            {
                return;
            }

            _chatManager.RemoveSpeechBubble(_senderEntity.Uid, this);
        }

        /// <summary>
        ///     Causes the speech bubble to start fading IMMEDIATELY.
        /// </summary>
        public void FadeNow()
        {
            if (_timeLeft > FadeTime)
            {
                _timeLeft = FadeTime;
            }
        }
    }
}
