﻿using Robust.Client.Graphics.Drawing;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Client.Interfaces.Graphics;
using Robust.Shared.Maths;
using System;
using Robust.Client.Graphics.Shaders;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using CannyFastMath;
using Math = CannyFastMath.Math;
using MathF = CannyFastMath.MathF;

namespace Robust.Client.UserInterface.Controls
{

    public class CooldownGraphic : Control
    {

        [Dependency] private readonly IPrototypeManager _protoMan = default!;

        private ShaderInstance _shader;

        public CooldownGraphic()
        {
            IoCManager.InjectDependencies(this);
            _shader = _protoMan.Index<ShaderPrototype>("CooldownAnimation").InstanceUnique();
        }

        /// <summary>
        ///     Progress of the cooldown animation.
        ///     Possible values range from 1 to -1, where 1 to 0 is a depleting circle animation and 0 to -1 is a blink animation.
        /// </summary>
        public float Progress { get; set; }

        protected override void Draw(DrawingHandleScreen handle)
        {
            Color color;

            var lerp = 1f - MathF.Abs(Progress); // for future bikeshedding purposes

            if (Progress >= 0f)
            {
                var hue = (5f / 18f) * lerp;
                color = Color.FromHsv((hue, 0.75f, 0.75f, 0.50f));
            }
            else
            {
                var alpha = MathF.Clamp(0.5f * lerp, 0f, 0.5f);
                color = new Color(1f, 1f, 1f, alpha);
            }

            _shader.SetParameter("progress", Progress);
            handle.UseShader(_shader);
            handle.DrawRect(PixelSizeBox, color);
        }

    }

}
