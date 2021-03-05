using System.Collections.Generic;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.GameObjects.Components
{
    [RegisterComponent]
    public class RandomSpriteColorComponent : Component, IMapInit
    {
        public override string Name => "RandomSpriteColor";

        [DataField("selected")]
        private string _selectedColor;
        [DataField("state")]
        private string _baseState = "error";

        [DataField("colors")] private Dictionary<string, Color> _colors = new();

        void IMapInit.MapInit()
        {
            if (_colors == null)
            {
                return;
            }

            var random = IoCManager.Resolve<IRobustRandom>();
            _selectedColor = random.Pick(_colors.Keys);
            UpdateColor();
        }

        protected override void Startup()
        {
            base.Startup();

            UpdateColor();
        }

        private void UpdateColor()
        {
            if (Owner.TryGetComponent(out SpriteComponent spriteComponent) && _colors != null && _selectedColor != null)
            {
                spriteComponent.LayerSetState(0, _baseState);
                spriteComponent.LayerSetColor(0, _colors[_selectedColor]);
            }
        }
    }
}
