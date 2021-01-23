#nullable enable
using System;
using System.Collections.Generic;
using Content.Client.GameObjects.Components;
using Content.Client.Utility;
using Content.Shared.GameObjects.Components;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Client.Interfaces.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Content.Client.GameObjects.EntitySystems.DoAfter
{
    public sealed class DoAfterGui : VBoxContainer
    {
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        private readonly Dictionary<byte, PanelContainer> _doAfterControls = new();
        private readonly Dictionary<byte, DoAfterBar> _doAfterBars = new();

        // We'll store cancellations for a little bit just so we can flash the graphic to indicate it's cancelled
        private readonly Dictionary<byte, TimeSpan> _cancelledDoAfters = new();

        public IEntity? AttachedEntity { get; set; }
        private ScreenCoordinates _playerPosition;

        // This behavior probably shouldn't be happening; so for whatever reason the control position is set the frame after
        // I got NFI why because I don't know the UI internals
        public bool FirstDraw { get; set; }

        public DoAfterGui()
        {
            IoCManager.InjectDependencies(this);
            IoCManager.Resolve<IUserInterfaceManager>().StateRoot.AddChild(this);
            SeparationOverride = 0;

            LayoutContainer.SetGrowVertical(this, LayoutContainer.GrowDirection.Begin);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (Disposed)
                return;

            foreach (var (_, control) in _doAfterControls)
            {
                control.Dispose();
            }

            _doAfterControls.Clear();
            _doAfterBars.Clear();
            _cancelledDoAfters.Clear();
        }

        /// <summary>
        ///     Add the necessary control for a DoAfter progress bar.
        /// </summary>
        /// <param name="message"></param>
        public void AddDoAfter(ClientDoAfter message)
        {
            if (_doAfterControls.ContainsKey(message.ID))
                return;

            var doAfterBar = new DoAfterBar
            {
                SizeFlagsVertical = SizeFlags.ShrinkCenter
            };

            _doAfterBars[message.ID] = doAfterBar;

            var control = new PanelContainer
            {
                Children =
                {
                    new TextureRect
                    {
                        Texture = StaticIoC.ResC.GetTexture("/Textures/Interface/Misc/progress_bar.rsi/icon.png"),
                        TextureScale = Vector2.One * DoAfterBar.DoAfterBarScale,
                        SizeFlagsVertical = SizeFlags.ShrinkCenter,
                    },

                    doAfterBar
                }
            };

            AddChild(control);
            _doAfterControls.Add(message.ID, control);
        }

        // NOTE THAT THE BELOW ONLY HANDLES THE UI SIDE

        /// <summary>
        ///     Removes a DoAfter without showing a cancel graphic.
        /// </summary>
        /// <param name="id"></param>
        public void RemoveDoAfter(byte id)
        {
            if (!_doAfterControls.ContainsKey(id))
                return;

            var control = _doAfterControls[id];
            RemoveChild(control);
            _doAfterControls.Remove(id);
            _doAfterBars.Remove(id);

            if (_cancelledDoAfters.ContainsKey(id))
                _cancelledDoAfters.Remove(id);

        }

        /// <summary>
        ///     Cancels a DoAfter and shows a graphic indicating it has been cancelled to the player.
        /// </summary>
        ///     Can be called multiple times on the 1 DoAfter because of the client predicting the cancellation.
        /// <param name="id"></param>
        public void CancelDoAfter(byte id)
        {
            if (_cancelledDoAfters.ContainsKey(id))
                return;

            if (!_doAfterBars.TryGetValue(id, out var doAfterBar))
            {
                doAfterBar = new DoAfterBar();
                _doAfterBars[id] = doAfterBar;
            }

            doAfterBar.Cancelled = true;
            _cancelledDoAfters.Add(id, _gameTiming.CurTime);
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);

            if (AttachedEntity?.IsValid() != true ||
                !AttachedEntity.TryGetComponent(out DoAfterComponent? doAfterComponent))
            {
                return;
            }

            var doAfters = doAfterComponent.DoAfters;
            if (doAfters.Count == 0)
                return;

            if (_eyeManager.CurrentMap != AttachedEntity.Transform.MapID)
            {
                Visible = false;
                return;
            }
            else
            {
                Visible = true;
            }

            // Set position ready for 2nd+ frames.
            var screenCoordinates = _eyeManager.CoordinatesToScreen(AttachedEntity.Transform.Coordinates);
            _playerPosition = new ScreenCoordinates(screenCoordinates.X / UIScale, screenCoordinates.Y / UIScale);
            LayoutContainer.SetPosition(this, new Vector2(_playerPosition.X - Width / 2, _playerPosition.Y - Height - 30.0f));

            if (FirstDraw)
            {
                Visible = false;
                FirstDraw = false;
                return;
            }

            Visible = true;
            var currentTime = _gameTiming.CurTime;
            var toCancel = new List<byte>();

            // Cleanup cancelled DoAfters
            foreach (var (id, cancelTime) in _cancelledDoAfters)
            {
                if ((currentTime - cancelTime).TotalSeconds > DoAfterSystem.ExcessTime)
                    toCancel.Add(id);
            }

            foreach (var id in toCancel)
            {
                RemoveDoAfter(id);
            }

            // Update 0 -> 1.0f of the things
            foreach (var (id, message) in doAfters)
            {
                if (_cancelledDoAfters.ContainsKey(id) || !_doAfterControls.ContainsKey(id))
                    continue;

                var doAfterBar = _doAfterBars[id];
                doAfterBar.Ratio = MathF.Min(1.0f,
                    (float) (currentTime - message.StartTime).TotalSeconds / message.Delay);
            }
        }
    }
}
