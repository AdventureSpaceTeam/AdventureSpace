using Content.Client.Pinpointer.UI;

namespace Content.Client.SurveillanceCamera.UI;

public sealed partial class SurveillanceCameraNavMapControl : NavMapControl
{
    public SurveillanceCameraNavMapControl() : base()
    {
        WallColor = new Color(100, 100, 100);
        TileColor = new(71, 42, 72, 0);
        BackgroundColor = Color.FromSrgb(TileColor.WithAlpha(BackgroundOpacity));
    }
}
