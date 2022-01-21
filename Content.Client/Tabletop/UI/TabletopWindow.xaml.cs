﻿using Robust.Client.AutoGenerated;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Maths;

namespace Content.Client.Tabletop.UI
{
    [GenerateTypedNameReferences]
    public partial class TabletopWindow : DefaultWindow
    {
        public TabletopWindow(IEye? eye, Vector2i size)
        {
            RobustXamlLoader.Load(this);

            ScalingVp.Eye = eye;
            ScalingVp.ViewportSize = size;

            FlipButton.OnButtonUp += Flip;
            OpenCentered();
        }

        private void Flip(BaseButton.ButtonEventArgs args)
        {
            // Flip the view 180 degrees
            if (ScalingVp.Eye is { } eye)
            {
                eye.Rotation = eye.Rotation.Opposite();

                // Flip alignmento of the button
                FlipButton.HorizontalAlignment = FlipButton.HorizontalAlignment == HAlignment.Right
                    ? HAlignment.Left
                    : HAlignment.Right;
            }
        }
    }
}
