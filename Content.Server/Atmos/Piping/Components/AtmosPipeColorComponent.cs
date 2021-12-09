using Content.Server.Atmos.Piping.EntitySystems;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Atmos.Piping.Components
{
    [RegisterComponent]
    public class AtmosPipeColorComponent : Component
    {
        public override string Name => "AtmosPipeColor";

        [DataField("color")]
        public Color Color { get; set; } = Color.White;

        [ViewVariables(VVAccess.ReadWrite), UsedImplicitly]
        public Color ColorVV
        {
            get => Color;
            set => EntitySystem.Get<AtmosPipeColorSystem>().SetColor(Owner, this, value);
        }
    }
}
