using Content.Client.Items.Components;
using Content.Shared.Light.Component;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Light.Components
{
    [RegisterComponent]
    [Access(typeof(HandheldLightSystem))]
    public sealed class HandheldLightComponent : SharedHandheldLightComponent, IItemStatus
    {
        public byte? Level;
        public bool Activated;

        /// <summary>
        ///     Whether to automatically set item-prefixes when toggling the flashlight.
        /// </summary>
        /// <remarks>
        ///     Flashlights should probably be using explicit unshaded sprite, in-hand and clothing layers, this is
        ///     mostly here for backwards compatibility.
        /// </remarks>
        [DataField("addPrefix")]
        public bool AddPrefix = false;

        public Control MakeControl()
        {
            return new StatusControl(this);
        }

        private sealed class StatusControl : Control
        {
            private const float TimerCycle = 1;

            private readonly HandheldLightComponent _parent;
            private readonly PanelContainer[] _sections = new PanelContainer[StatusLevels - 1];

            private float _timer;

            private static readonly StyleBoxFlat StyleBoxLit = new()
            {
                BackgroundColor = Color.LimeGreen
            };

            private static readonly StyleBoxFlat StyleBoxUnlit = new()
            {
                BackgroundColor = Color.Black
            };

            public StatusControl(HandheldLightComponent parent)
            {
                _parent = parent;

                var wrapper = new BoxContainer
                {
                    Orientation = LayoutOrientation.Horizontal,
                    SeparationOverride = 4,
                    HorizontalAlignment = HAlignment.Center
                };

                AddChild(wrapper);

                for (var i = 0; i < _sections.Length; i++)
                {
                    var panel = new PanelContainer {MinSize = (20, 20)};
                    wrapper.AddChild(panel);
                    _sections[i] = panel;
                }
            }

            protected override void FrameUpdate(FrameEventArgs args)
            {
                base.FrameUpdate(args);

                _timer += args.DeltaSeconds;
                _timer %= TimerCycle;

                var level = _parent.Level;

                for (var i = 0; i < _sections.Length; i++)
                {
                    if (i == 0)
                    {
                        if (level == 0 || level == null)
                        {
                            _sections[0].PanelOverride = StyleBoxUnlit;
                        }
                        else if (level == 1)
                        {
                            // Flash the last light.
                            _sections[0].PanelOverride = _timer > TimerCycle / 2 ? StyleBoxLit : StyleBoxUnlit;
                        }
                        else
                        {
                            _sections[0].PanelOverride = StyleBoxLit;
                        }

                        continue;
                    }

                    _sections[i].PanelOverride = level >= i + 2 ? StyleBoxLit : StyleBoxUnlit;
                }
            }
        }
    }
}
