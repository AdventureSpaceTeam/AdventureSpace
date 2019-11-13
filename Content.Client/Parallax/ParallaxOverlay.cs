using Content.Client.Interfaces.Parallax;
using Robust.Client.Graphics;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Graphics.Overlays;
using Robust.Client.Graphics.Shaders;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Client.Parallax
{
    public class ParallaxOverlay : Overlay
    {
#pragma warning disable 649
        [Dependency] private readonly IParallaxManager _parallaxManager;
        [Dependency] private readonly IEyeManager _eyeManager;
        [Dependency] private readonly IPrototypeManager _prototypeManager;
        [Dependency] private readonly IMapManager _mapManager;
#pragma warning restore 649

        public override bool AlwaysDirty => true;
        private const float Slowness = 0.5f;

        private Texture _parallaxTexture;

        public override OverlaySpace Space => OverlaySpace.ScreenSpaceBelowWorld;

        public ParallaxOverlay() : base(nameof(ParallaxOverlay))
        {
            IoCManager.InjectDependencies(this);
            Shader = _prototypeManager.Index<ShaderPrototype>("unshaded").Instance();

            if (_parallaxManager.ParallaxTexture == null)
            {
                _parallaxManager.OnTextureLoaded += texture => _parallaxTexture = texture;
            }
            else
            {
                _parallaxTexture = _parallaxManager.ParallaxTexture;
            }
        }

        protected override void Draw(DrawingHandleBase handle)
        {
            if (_parallaxTexture == null)
            {
                return;
            }

            var screenHandle = (DrawingHandleScreen) handle;

            var (sizeX, sizeY) = _parallaxTexture.Size;
            var (posX, posY) = _eyeManager.ScreenToWorld(Vector2.Zero).ToWorld(_mapManager).Position;
            var (ox, oy) = (Vector2i) new Vector2(-posX / Slowness, posY / Slowness);
            ox = MathHelper.Mod(ox, sizeX);
            oy = MathHelper.Mod(oy, sizeY);

            screenHandle.DrawTexture(_parallaxTexture, new Vector2(ox, oy));
            screenHandle.DrawTexture(_parallaxTexture, new Vector2(ox - sizeX, oy));
            screenHandle.DrawTexture(_parallaxTexture, new Vector2(ox, oy - sizeY));
            screenHandle.DrawTexture(_parallaxTexture, new Vector2(ox - sizeX, oy - sizeY));
        }
    }
}
