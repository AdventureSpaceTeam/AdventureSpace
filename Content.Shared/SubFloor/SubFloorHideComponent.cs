#nullable enable
using Robust.Shared.GameObjects;

namespace Content.Shared.SubFloor
{
    /// <summary>
    /// Simple component that automatically hides the sibling
    /// <see cref="ISpriteComponent" /> when the tile it's on is not a sub floor
    /// (plating).
    /// </summary>
    /// <seealso cref="P:Content.Shared.Maps.ContentTileDefinition.IsSubFloor" />
    [RegisterComponent]
    public sealed class SubFloorHideComponent : Component
    {
        /// <inheritdoc />
        public override string Name => "SubFloorHide";
    }
}
